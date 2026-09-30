namespace OverLoad.API.Models.Requests;

/// <summary>Admin-only: grant a user access to a course without going through payment.</summary>
public class GrantEnrollmentRequestModel
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
}

public class EnrollmentQueryRequestModel : PagedQueryRequestModel
{
    public int? UserId { get; set; }
    public int? CourseId { get; set; }

    // SortBy: enrolledAt, progress (default "-enrolledAt")
}

/// <summary>Progress the signed-in user reports for one lesson (video position, scroll, checkpoints).</summary>
public class SaveProgressRequestModel
{
    public decimal LastScrollPercentage { get; set; }
    public int UnlockedCheckpointIndex { get; set; }
    public int LastPositionSeconds { get; set; }
    public int WatchTimeSeconds { get; set; }

    /// <summary>Completion is one-way: sending false for a completed lesson does not un-complete it.</summary>
    public bool Completed { get; set; }
}

public class ProgressQueryRequestModel : PagedQueryRequestModel
{
    public int? UserId { get; set; }
    public int? LessonId { get; set; }
    public bool? Completed { get; set; }

    // SortBy: watchTime, scroll, completedAt, createdAt (default "-createdAt")
}
