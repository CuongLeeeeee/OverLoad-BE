using OverLoad.Services.Common;

namespace OverLoad.Services.DTOs.Request;

/// <summary>Progress the signed-in user reports for one lesson (video position, scroll, checkpoints).</summary>
public class SaveProgressRequest
{
    public decimal LastScrollPercentage { get; set; }
    public int UnlockedCheckpointIndex { get; set; }
    public int LastPositionSeconds { get; set; }
    public int WatchTimeSeconds { get; set; }
    public bool Completed { get; set; }
}

public class ProgressQueryParams : QueryParameters
{
    public int? UserId { get; set; }
    public int? LessonId { get; set; }
    public bool? Completed { get; set; }

    /// <summary>watchTime, scroll, completedAt, createdAt. Prefix with '-' for descending.</summary>
    public override string? SortBy { get; set; } = "-createdAt";
}
