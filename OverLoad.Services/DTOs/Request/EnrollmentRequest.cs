using OverLoad.Services.Common;

namespace OverLoad.Services.DTOs.Request;

/// <summary>Admin-only: grant a user access to a course without going through payment.</summary>
public class CreateEnrollmentRequest
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
}

public class EnrollmentQueryParams : QueryParameters
{
    public int? UserId { get; set; }
    public int? CourseId { get; set; }

    /// <summary>enrolledAt, progress. Prefix with '-' for descending.</summary>
    public override string? SortBy { get; set; } = "-enrolledAt";
}
