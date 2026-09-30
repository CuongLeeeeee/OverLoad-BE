using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Services.Implementations;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;

namespace OverLoad.Tests.Integration;

/// <summary>
/// Boots the real API pipeline (routing, versioning, auth, validation, middleware) on an
/// in-memory database seeded with <see cref="TestData"/>.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "integration-test-secret-key-at-least-32-bytes";
    public const string AllowedOrigin = "https://app.overload.test";
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public TestData.Seeded Data { get; private set; } = null!;

    public FakePaymentGateway Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting (rather than ConfigureAppConfiguration) is visible to Program.cs while the
        // builder is still being set up, which is when the JWT options are read.
        builder.UseSetting("Jwt:SecretKey", JwtSecret);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.ConfigureServices(services =>
        {
            // Swap SQL Server for the in-memory provider.
            var sqlServerRegistrations = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();
            foreach (var descriptor in sqlServerRegistrations)
                services.Remove(descriptor);

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
}
