using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OverLoad.Repositories.Data;

/// <summary>
/// Required by EF Core CLI tooling to create migrations from the Repositories project.
/// Run: dotnet ef migrations add InitialCreate --project OverLoad.Repositories --startup-project OverLoad.API
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../OverLoad.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly("OverLoad.Repositories"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
