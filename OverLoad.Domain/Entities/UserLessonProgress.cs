using OverLoad.Domain.Common;

namespace OverLoad.Domain.Entities;

public class UserLessonProgress : BaseEntity
{
    public int UserId { get; set; }
    public int LessonId { get; set; }
    public decimal LastScrollPercentage { get; set; } = 0;
    public int UnlockedCheckpointIndex { get; set; } = 0;
    public bool Completed { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public int LastPositionSeconds { get; set; } = 0;
    public int WatchTimeSeconds { get; set; } = 0;

    // Navigation
    public User User { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
