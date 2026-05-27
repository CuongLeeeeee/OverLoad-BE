using System.ComponentModel.DataAnnotations;

namespace OverLoad.Services.DTOs.Request;

public class CreateCourseRequest
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public string Level { get; set; } = "Beginner";

    public bool IsPublished { get; set; } = false;
}

public class UpdateCourseRequest
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public string Level { get; set; } = "Beginner";

    public bool IsPublished { get; set; } = false;
}

public class CourseQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Level { get; set; }
    public bool? IsPublished { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public bool SortDesc { get; set; } = false;
}
