using System.ComponentModel.DataAnnotations;

namespace OverLoad.Services.DTOs.Request;

public class UpsertProgressRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int LessonId { get; set; }

    [Range(0, 100)]
    public decimal LastScrollPercentage { get; set; }

    public int UnlockedCheckpointIndex { get; set; }
    public bool Completed { get; set; }
    public int LastPositionSeconds { get; set; }
    public int WatchTimeSeconds { get; set; }
}
