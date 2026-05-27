using System.ComponentModel.DataAnnotations;

namespace OverLoad.Services.DTOs.Request;

public class CreateUserRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public string Role { get; set; } = "Student";
}

public class UpdateUserRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public bool IsVerified { get; set; }

    public string Role { get; set; } = "Student";
}

public class UserQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public bool SortDesc { get; set; } = false;
}
