using Microsoft.Extensions.Configuration;
using Npgsql;
using OverLoad.Repositories.Data;
using Xunit;

namespace OverLoad.Tests.Services;

public class DatabaseConnectionTests
{
    [Fact]
    public void FromUrl_Should_Convert_A_Neon_Style_Url()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "postgresql://app_user:s3cret@ep-cool-name-123-pooler.ap-southeast-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require"));

        Assert.Equal("ep-cool-name-123-pooler.ap-southeast-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("app_user", result.Username);
        Assert.Equal("s3cret", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
        Assert.Equal(ChannelBinding.Require, result.ChannelBinding);
    }

    [Fact]
    public void FromUrl_Should_Decode_Special_Characters_And_Keep_Explicit_Port()
    {
        var result = new NpgsqlConnectionStringBuilder(DatabaseConnection.FromUrl(
            "'postgres://user:p%40ss%3Aw%2Frd@db.example.com:6543/my%20db?sslmode=verify-full'"));

        Assert.Equal("p@ss:w/rd", result.Password);
        Assert.Equal(6543, result.Port);
        Assert.Equal("my db", result.Database);
        Assert.Equal(SslMode.VerifyFull, result.SslMode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Host=localhost;Database=x")]
    [InlineData("mysql://u:p@host/db")]
    public void FromUrl_Should_Reject_Anything_But_A_Postgres_Url(string url)
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.FromUrl(url));
    }

    [Fact]
    public void Resolve_Should_Prefer_DATABASE_URL_Over_DefaultConnection()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgresql://u:p@from-url/db",
            ["ConnectionStrings:DefaultConnection"] = "Host=from-appsettings;Database=db"
        }).Build();

        Assert.Equal("from-url", new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration)).Host);
    }

    [Fact]
    public void Resolve_Should_Fall_Back_To_DefaultConnection_And_Fail_Clearly_When_Nothing_Is_Set()
    {
        var withDefault = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "",
            ["ConnectionStrings:DefaultConnection"] = "Host=from-appsettings;Database=db"
        }).Build();
        var empty = new ConfigurationBuilder().Build();

        Assert.Equal("Host=from-appsettings;Database=db", DatabaseConnection.Resolve(withDefault));
        Assert.Throws<InvalidOperationException>(() => DatabaseConnection.Resolve(empty));
    }
}
