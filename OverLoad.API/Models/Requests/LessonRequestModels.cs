namespace OverLoad.API.Models.Requests;

public class CreateLessonRequestModel
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFree { get; set; }
}

public class UpdateLessonRequestModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
}

public class LessonQueryRequestModel : PagedQueryRequestModel
{
    public int? CourseId { get; set; }

    /// <summary>Matches the lesson title.</summary>
    public string? Search { get; set; }
    public bool? IsFree { get; set; }

    // SortBy: title, orderIndex (default "orderIndex")
}
