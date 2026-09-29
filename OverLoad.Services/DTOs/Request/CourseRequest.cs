using OverLoad.Domain.Enums;
using OverLoad.Services.Common;

namespace OverLoad.Services.DTOs.Request;

public class CreateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public bool IsPublished { get; set; }
}

public class UpdateCourseRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public bool IsPublished { get; set; }
}

public class CourseQueryParams : QueryParameters
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public CourseLevel? Level { get; set; }

    /// <summary>Only honoured for Admin/Instructor; everyone else always sees published courses.</summary>
    public bool? IsPublished { get; set; }

    /// <summary>title, category, totalLessons, createdAt. Prefix with '-' for descending.</summary>
    public override string? SortBy { get; set; } = "-createdAt";
}
