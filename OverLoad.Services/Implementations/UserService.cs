using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;
using BCrypt.Net;
namespace OverLoad.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApiResponse<UserDetailResponse>> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetWithEnrollmentsAsync(id);
        if (user == null)
            return ApiResponse<UserDetailResponse>.FailResult("User not found.");

        return ApiResponse<UserDetailResponse>.SuccessResult(MapToDetailResponse(user));
    }

    public async Task<PagedResponse<UserResponse>> GetAllAsync(UserQueryParams query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await _userRepository.SearchAsync(
            query.Search, query.Role, query.Page, query.PageSize, query.SortBy, query.SortDesc);

        return PagedResponse<UserResponse>.SuccessResult(
            items.Select(MapToResponse), total, query.Page, query.PageSize);
    }

    public async Task<ApiResponse<UserResponse>> CreateAsync(CreateUserRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            return ApiResponse<UserResponse>.FailResult("Email already in use.", "Duplicate email address.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return ApiResponse<UserResponse>.FailResult("Invalid role specified.", $"Role '{request.Role}' is not valid.");

        var user = new User
        {
            Email = request.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            AvatarUrl = request.AvatarUrl,
            Bio = request.Bio,
            Role = role,
            IsVerified = false
        };

        var created = await _userRepository.AddAsync(user);
        return ApiResponse<UserResponse>.SuccessResult(MapToResponse(created), "User created successfully.");
    }

    public async Task<ApiResponse<UserResponse>> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return ApiResponse<UserResponse>.FailResult("User not found.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return ApiResponse<UserResponse>.FailResult("Invalid role specified.", $"Role '{request.Role}' is not valid.");

        user.FullName = request.FullName.Trim();
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;
        user.IsVerified = request.IsVerified;
        user.Role = role;

        await _userRepository.UpdateAsync(user);
        return ApiResponse<UserResponse>.SuccessResult(MapToResponse(user), "User updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (!await _userRepository.ExistsAsync(id))
            return ApiResponse<bool>.FailResult("User not found.");

        await _userRepository.DeleteAsync(id);
        return ApiResponse<bool>.SuccessResult(true, "User deleted successfully.");
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    private static UserResponse MapToResponse(User user) => new()
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
    };

    private static UserDetailResponse MapToDetailResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        Bio = user.Bio,
        Role = user.Role.ToString(),
        IsVerified = user.IsVerified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Enrollments = user.Enrollments.Select(e => new EnrollmentSummaryResponse
        {
            CourseId = e.CourseId,
            CourseTitle = e.Course?.Title ?? string.Empty,
            CourseSlug = e.Course?.Slug ?? string.Empty,
            ProgressPercentage = e.ProgressPercentage,
            EnrolledAt = e.EnrolledAt,
            CompletedAt = e.CompletedAt,
            LastAccessedAt = e.LastAccessedAt
        }).ToList()
    };

    
}
