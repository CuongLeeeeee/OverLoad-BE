using Microsoft.Extensions.Options;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;
using OverLoad.Services.Settings;

namespace OverLoad.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(email))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            AvatarUrl = request.AvatarUrl,
            Bio = request.Bio,
            Role = UserRole.Student,
            IsVerified = false
        };

        SetRefreshToken(user);
        var created = await _userRepository.AddAsync(user);

        return BuildAuthResponse(created);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email or password is incorrect.");

        EnsureNotLocked(user);

        SetRefreshToken(user);
        await _userRepository.UpdateAsync(user);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);

        if (user == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token. Please log in again.");

        EnsureNotLocked(user);

        // Rotate refresh token (one-time use)
        SetRefreshToken(user);
        await _userRepository.UpdateAsync(user);

        return BuildAuthResponse(user);
    }

    public async Task LogoutAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        await _userRepository.UpdateAsync(user);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void EnsureNotLocked(User user)
    {
        if (user.IsLocked)
            throw new ForbiddenException("This account has been locked. Please contact an administrator.");
    }

    private void SetRefreshToken(User user)
    {
        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
    }

    private AuthResultDto BuildAuthResponse(User user) => new()
    {
        AccessToken = _jwtService.GenerateAccessToken(user),
        RefreshToken = user.RefreshToken!,
        AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
        User = UserService.MapToResponse(user)
    };
}
