namespace OverLoad.Services.DTOs.Response;

public class CourseProgressDto
{
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool IsCompleted { get; set; }
}