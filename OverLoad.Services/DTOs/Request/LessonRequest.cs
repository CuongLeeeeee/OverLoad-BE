using OverLoad.Services.Common;

namespace OverLoad.Services.DTOs.Request;

public class CreateLessonRequest
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFree { get; set; }
}

public class UpdateLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
}

public class LessonQueryParams : QueryParameters
{
    public int? CourseId { get; set; }
    public string? Search { get; set; }
    public bool? IsFree { get; set; }

    /// <summary>title, orderIndex. Prefix with '-' for descending.</summary>
    public override string? SortBy { get; set; } = "orderIndex";
}
