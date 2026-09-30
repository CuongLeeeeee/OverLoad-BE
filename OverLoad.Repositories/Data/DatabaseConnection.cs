using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OverLoad.Repositories.Data;

/// <summary>
/// Resolves the PostgreSQL connection string for the app and the EF Core CLI.
/// <para>
/// <c>DATABASE_URL</c> (the URL form Neon, Render and Railway hand out, e.g.
/// <c>postgresql://user:pass@host/db?sslmode=require</c>) wins when set; otherwise
/// <c>ConnectionStrings:DefaultConnection</c> (Npgsql key=value form) is used.
/// </para>
/// </summary>
public static class DatabaseConnection
{
    public const string UrlVariable = "DATABASE_URL";

    /// <summary>
    /// Loads the nearest <c>.env</c> file (searching up from the working directory) into
    /// environment variables for local runs. Variables that are already set are not overwritten,
    /// and nothing happens when there is no <c>.env</c> file (as in the Docker image).
    /// </summary>
    public static void LoadDotEnv() => Env.NoClobber().TraversePath().Load();

    public static string Resolve(IConfiguration configuration)
    {
        var url = configuration[UrlVariable];
        if (!string.IsNullOrWhiteSpace(url))
            return FromUrl(url);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"No database configured. Set {UrlVariable} or ConnectionStrings:DefaultConnection.");

        return connectionString;
    }

    /// <summary>Converts a <c>postgres://</c> / <c>postgresql://</c> URL to an Npgsql connection string.</summary>
    public static string FromUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim().Trim('\'', '"'), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql"))
            throw new InvalidOperationException($"{UrlVariable} must look like postgresql://user:password@host/database.");

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null
        };

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            switch (parts[0].ToLowerInvariant())
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(value.Replace("-", string.Empty), ignoreCase: true);
                    break;
                case "channel_binding":
                    builder.ChannelBinding = Enum.Parse<ChannelBinding>(value, ignoreCase: true);
                    break;
                // Other libpq options (e.g. Neon's "options=endpoint%3D...") are not needed by Npgsql.
            }
        }

        return builder.ConnectionString;
    }
}
