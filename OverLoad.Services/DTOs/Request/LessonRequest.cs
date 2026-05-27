using System.ComponentModel.DataAnnotations;

namespace OverLoad.Services.DTOs.Request;

public class CreateLessonRequest
{
    [Required]
    public int CourseId { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? Content { get; set; }

    [Range(0, int.MaxValue)]
    public int DurationMinutes { get; set; }

    public bool IsFree { get; set; } = false;
}

public class UpdateLessonRequest
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? Content { get; set; }

    [Range(0, int.MaxValue)]
    public int DurationMinutes { get; set; }

    [Range(1, int.MaxValue)]
    public int OrderIndex { get; set; }

    public bool IsFree { get; set; } = false;
}

public class LessonQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? CourseId { get; set; }
    public string? Search { get; set; }
    public bool? IsFree { get; set; }
    public string? SortBy { get; set; } = "orderIndex";
    public bool SortDesc { get; set; } = false;
}
