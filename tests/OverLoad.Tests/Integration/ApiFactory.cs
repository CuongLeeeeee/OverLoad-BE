using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Services.Implementations;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;

namespace OverLoad.Tests.Integration;

/// <summary>
/// Boots the real API pipeline (routing, versioning, auth, validation, middleware) on a database
/// seeded with <see cref="TestData"/>.
/// <para>
/// By default the database is EF Core InMemory. When the TEST_POSTGRES_CONNECTION environment
/// variable is set (CI does this), each factory instead gets its own throw-away PostgreSQL
/// database, created by the real migrations — so provider-specific behaviour is covered too.
/// </para>
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "integration-test-secret-key-at-least-32-bytes";
    public const string AllowedOrigin = "https://app.overload.test";

    private static readonly string? PostgresConnection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION");
    private readonly string _databaseName = $"overload_test_{Guid.NewGuid():N}";

    public TestData.Seeded Data { get; private set; } = null!;

    public FakePaymentGateway Gateway { get; } = new();

    public static bool UsesPostgres => !string.IsNullOrWhiteSpace(PostgresConnection);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting (rather than ConfigureAppConfiguration) is visible to Program.cs while the
        // builder is still being set up, which is when the JWT options are read.
        builder.UseSetting("Jwt:SecretKey", JwtSecret);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused");
        // Program loads the developer's .env; make sure a DATABASE_URL in it can never reach the tests.
        builder.UseSetting("DATABASE_URL", string.Empty);
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.ConfigureServices(services =>
        {
            // Replace the application's database registration with the test database.
            var dbRegistrations = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();
            foreach (var descriptor in dbRegistrations)
                services.Remove(descriptor);

            if (UsesPostgres)
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(TestDatabaseConnection()));
            else
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            // Never call the real PayOS from tests.
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(Gateway);
        });
    }

    public async Task InitializeDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (UsesPostgres)
            await context.Database.MigrateAsync();
        Data = await TestData.SeedAsync(context);
    }

    /// <summary>An HttpClient whose requests carry a valid access token for <paramref name="user"/>.</summary>
    public HttpClient CreateClientFor(User user)
    {
        var jwt = new JwtService(Services.GetRequiredService<IOptions<JwtSettings>>());
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(user));
        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        if (UsesPostgres)
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private string TestDatabaseConnection()
        => new NpgsqlConnectionStringBuilder(PostgresConnection) { Database = _databaseName }.ConnectionString;
}
