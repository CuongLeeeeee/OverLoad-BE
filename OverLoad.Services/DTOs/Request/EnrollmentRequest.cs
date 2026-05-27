using System.ComponentModel.DataAnnotations;

namespace OverLoad.Services.DTOs.Request;

public class CreateEnrollmentRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int CourseId { get; set; }
}

public class UpdateEnrollmentRequest
{
    [Range(0, 100)]
    public decimal ProgressPercentage { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
}

public class EnrollmentQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public int? CourseId { get; set; }
    public string? SortBy { get; set; } = "enrolledAt";
    public bool SortDesc { get; set; } = true;
}
