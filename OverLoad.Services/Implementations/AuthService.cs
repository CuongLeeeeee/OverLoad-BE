using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverLoad.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private const int RefreshTokenExpiryDays = 7;

        public AuthService(IUserRepository userRepository, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            if (await _userRepository.EmailExistsAsync(request.Email))
                return ApiResponse<AuthResponse>.FailResult("Email already in use.",
                    "An account with this email already exists.");

            var user = new User
            {
                Email = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                AvatarUrl = request.AvatarUrl,
                Bio = request.Bio,
                Role = UserRole.Student,
                IsVerified = false
            };

            SetRefreshToken(user);
            var created = await _userRepository.AddAsync(user);

            return ApiResponse<AuthResponse>.SuccessResult(BuildAuthResponse(created), "Registration successful.");
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return ApiResponse<AuthResponse>.FailResult("Invalid credentials.",
                    "Email or password is incorrect.");

            SetRefreshToken(user);
            await _userRepository.UpdateAsync(user);

            return ApiResponse<AuthResponse>.SuccessResult(BuildAuthResponse(user), "Login successful.");
        }

        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);

            if (user == null)
                return ApiResponse<AuthResponse>.FailResult("Invalid refresh token.");

            if (user.RefreshTokenExpiresAt < DateTime.UtcNow)
                return ApiResponse<AuthResponse>.FailResult("Refresh token has expired. Please log in again.");

            // Rotate refresh token (one-time use)
            SetRefreshToken(user);
            await _userRepository.UpdateAsync(user);

            return ApiResponse<AuthResponse>.SuccessResult(BuildAuthResponse(user), "Token refreshed successfully.");
        }

        public async Task<ApiResponse<bool>> LogoutAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<bool>.FailResult("User not found.");

            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await _userRepository.UpdateAsync(user);

            return ApiResponse<bool>.SuccessResult(true, "Logged out successfully.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void SetRefreshToken(User user)
        {
            user.RefreshToken = _jwtService.GenerateRefreshToken();
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);
        }

        private AuthResponse BuildAuthResponse(User user) => new()
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            RefreshToken = user.RefreshToken!,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Role = user.Role.ToString(),
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        };
    }
}
