
namespace OverLoad.Services.DTOs.Response;

public class LessonProgressDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
    public bool Completed { get; set; }
    public decimal WatchPercentage { get; set; }
    public int LastPositionSeconds { get; set; }
    public bool IsLocked { get; set; }
}