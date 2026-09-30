using Microsoft.Extensions.Options;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Implementations;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Implementations;
using OverLoad.Services.Settings;
using Xunit;

namespace OverLoad.Tests.Services;

public class AuthServiceTests
{
    private static readonly IOptions<JwtSettings> JwtOptions = Options.Create(new JwtSettings
    {
        SecretKey = "unit-test-secret-key-that-is-at-least-32-bytes",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7
    });

    private static AuthService CreateService(AppDbContext context) =>
        new(new UserRepository(context), new JwtService(JwtOptions), JwtOptions);

    [Fact]
    public async Task RegisterAsync_Should_Create_Student_And_Return_Tokens()
    {
        await using var context = TestData.CreateContext();

        var result = await CreateService(context).RegisterAsync(new RegisterRequest
        {
            Email = "  New@Test.io ",
            Password = "secret123",
            FullName = "New User"
        });

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal("new@test.io", result.User.Email);
        Assert.Equal(UserRole.Student.ToString(), result.User.Role);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_Conflict_For_Existing_Email()
    {
        await using var context = TestData.CreateContext();
        await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<ConflictException>(() => CreateService(context).RegisterAsync(new RegisterRequest
        {
            Email = "STUDENT@test.io",
            Password = "secret123",
            FullName = "Dup"
        }));
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_Unauthorized_For_Wrong_Password()
    {
        await using var context = TestData.CreateContext();
        await TestData.SeedAsync(context);

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService(context).LoginAsync(
            new LoginRequest { Email = "student@test.io", Password = "wrong" }));
    }

    [Fact]
    public async Task LoginAsync_Should_Refuse_Locked_Account()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        data.Student.IsLocked = true;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateService(context).LoginAsync(
            new LoginRequest { Email = "student@test.io", Password = TestData.Password }));
    }

    [Fact]
    public async Task Locking_Should_Revoke_Refresh_Token_And_Unlocking_Allows_Login_Again()
    {
        await using var context = TestData.CreateContext();
        var data = await TestData.SeedAsync(context);
        var auth = CreateService(context);
        var users = new UserService(new UserRepository(context));
        var login = await auth.LoginAsync(new LoginRequest { Email = "student@test.io", Password = TestData.Password });

        var locked = await users.SetLockedAsync(data.Student.Id, locked: true);

        Assert.True(locked.IsLocked);
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => auth.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken }));

        await users.SetLockedAsync(data.Student.Id, locked: false);
        var again = await auth.LoginAsync(new LoginRequest { Email = "student@test.io", Password = TestData.Password });
        Assert.False(again.User.IsLocked);
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Rotate_Token_And_Reject_The_Old_One()
    {
        await using var context = TestData.CreateContext();
        await TestData.SeedAsync(context);
        var service = CreateService(context);

        var login = await service.LoginAsync(new LoginRequest { Email = "student@test.io", Password = TestData.Password });
        var refreshed = await service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken });

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => service.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = login.RefreshToken }));
    }
}
