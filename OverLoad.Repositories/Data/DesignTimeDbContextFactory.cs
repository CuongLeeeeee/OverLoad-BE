using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OverLoad.Repositories.Data;

/// <summary>
/// Used by the EF Core CLI (dotnet ef migrations add / database update).
/// The database comes from DATABASE_URL (also read from the repo's .env) or from
/// ConnectionStrings:DefaultConnection in OverLoad.API/appsettings*.json.
/// Run from the solution root:
///   dotnet ef database update --project OverLoad.Repositories --startup-project OverLoad.API
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DatabaseConnection.LoadDotEnv();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../OverLoad.API"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            DatabaseConnection.Resolve(configuration),
            npgsql => npgsql.MigrationsAssembly("OverLoad.Repositories"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
