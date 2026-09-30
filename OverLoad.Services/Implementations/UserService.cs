using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(UserQueryParams query)
    {
        var (sortBy, sortDesc) = query.ParseSort();
        var (items, total) = await _userRepository.SearchAsync(
            query.Search, query.Role?.ToString(), query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<UserDto>.Create(items.Select(MapToResponse), total, query);
    }

    public async Task<UserDetailDto> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetWithEnrollmentsAsync(id)
            ?? throw new NotFoundException("User", id);

        return MapToDetailResponse(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email.Trim().ToLowerInvariant()))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            AvatarUrl = request.AvatarUrl,
            Bio = request.Bio,
            Role = request.Role,
            IsVerified = false
        };

        var created = await _userRepository.AddAsync(user);
        return MapToResponse(created);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        user.FullName = request.FullName.Trim();
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;
        user.IsVerified = request.IsVerified;
        user.Role = request.Role;

        await _userRepository.UpdateAsync(user);
        return MapToResponse(user);
    }

    public async Task<UserDto> UpdateProfileAsync(int id, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        user.FullName = request.FullName.Trim();
        user.AvatarUrl = request.AvatarUrl;
        user.Bio = request.Bio;

        await _userRepository.UpdateAsync(user);
        return MapToResponse(user);
    }

    public async Task<UserDto> SetLockedAsync(int id, bool locked)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("User", id);

        user.IsLocked = locked;
        if (locked)
        {
            // Sign-in and refresh are refused from now on; an access token already issued stays
            // valid until it expires (Jwt:AccessTokenExpiryMinutes).
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
        }

        await _userRepository.UpdateAsync(user);
        return MapToResponse(user);
    }

    public async Task DeleteAsync(int id)
    {
        if (!await _userRepository.ExistsAsync(id))
            throw new NotFoundException("User", id);

        await _userRepository.DeleteAsync(id);
    }

    // ── Mapping helpers ──────────────────────────────────────────────────────

    internal static UserDto MapToResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        Bio = user.Bio,
        Role = user.Role.ToString(),
        IsVerified = user.IsVerified,
        IsLocked = user.IsLocked,
        IsPro = user.HasActivePro(DateTime.UtcNow),
        ProExpiresAt = user.ProExpiresAt,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    private static UserDetailDto MapToDetailResponse(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        AvatarUrl = user.AvatarUrl,
        Bio = user.Bio,
        Role = user.Role.ToString(),
        IsVerified = user.IsVerified,
        IsLocked = user.IsLocked,
        IsPro = user.HasActivePro(DateTime.UtcNow),
        ProExpiresAt = user.ProExpiresAt,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Enrollments = user.Enrollments.Select(e => new EnrollmentSummaryDto
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
