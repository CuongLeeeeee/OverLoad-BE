using OverLoad.Domain.Enums;
using OverLoad.Services.Common;

namespace OverLoad.Services.DTOs.Request;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
}

public class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsVerified { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
}

/// <summary>Fields a user may change on their own profile.</summary>
public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}

public class UserQueryParams : QueryParameters
{
    public string? Search { get; set; }
    public UserRole? Role { get; set; }

    /// <summary>email, fullName, createdAt. Prefix with '-' for descending.</summary>
    public override string? SortBy { get; set; } = "-createdAt";
}
