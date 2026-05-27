namespace OverLoad.Services.DTOs.Response;

public class LessonResponse
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
