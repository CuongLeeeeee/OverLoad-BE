using OverLoad.Domain.Enums;

namespace OverLoad.API.Models.Requests;

public class CreateCourseRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    // Courses always start as Draft; no IsPublished flag
}

public class UpdateCourseRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    // Status changes go through /submit, /approve, /reject
}

public class RejectCourseRequestModel
{
    public string Reason { get; set; } = string.Empty;
}

public class SetCoursePriceRequestModel
{
    public decimal Price { get; set; }
}

public class CourseQueryRequestModel : PagedQueryRequestModel
{
    /// <summary>Matches title or description.</summary>
    public string? Search { get; set; }
    public string? Category { get; set; }
    public CourseLevel? Level { get; set; }

    /// <summary>Only honoured for Admin/Instructor; everyone else always sees published courses.</summary>
    public bool? IsPublished { get; set; }

    // SortBy: title, category, totalLessons, createdAt (default "-createdAt")
}
