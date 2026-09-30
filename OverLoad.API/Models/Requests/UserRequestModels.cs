using OverLoad.Domain.Enums;

namespace OverLoad.API.Models.Requests;

public class CreateUserRequestModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
}

public class UpdateUserRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsVerified { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;
}

/// <summary>Fields a user may change on their own profile.</summary>
public class UpdateProfileRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}

public class UserQueryRequestModel : PagedQueryRequestModel
{
    /// <summary>Matches email or full name.</summary>
    public string? Search { get; set; }
    public UserRole? Role { get; set; }

    // SortBy: email, fullName, createdAt (default "-createdAt")
}
