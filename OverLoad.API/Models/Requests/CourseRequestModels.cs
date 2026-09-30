using OverLoad.Domain.Enums;

namespace OverLoad.API.Models.Requests;

public class CreateCourseRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public bool IsPublished { get; set; }
}

public class UpdateCourseRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public bool IsPublished { get; set; }
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
