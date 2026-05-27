namespace OverLoad.Services.DTOs.Response;

public class ProgressResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public decimal LastScrollPercentage { get; set; }
    public int UnlockedCheckpointIndex { get; set; }
    public bool Completed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LastPositionSeconds { get; set; }
    public int WatchTimeSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
