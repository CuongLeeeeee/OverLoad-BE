namespace OverLoad.Services.DTOs.Response;

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserDetailResponse : UserResponse
{
    public List<EnrollmentSummaryResponse> Enrollments { get; set; } = new();
}

public class EnrollmentSummaryResponse
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string CourseSlug { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}
