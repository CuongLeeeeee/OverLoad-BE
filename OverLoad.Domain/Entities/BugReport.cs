using OverLoad.Domain.Common;

namespace OverLoad.Domain.Entities;

/// <summary>
/// A problem a learner reported on a course or lesson. The table was created by migrations whose
/// source code was lost; this entity mirrors the production schema so the model and database agree.
/// The bug-report endpoints themselves still need to be rebuilt.
/// </summary>
public class BugReport : BaseEntity
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public int? LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Stored as an int. The meaning of each value was defined in the lost code; confirm it
    /// (e.g. against the frontend) before turning this into an enum.
    /// </summary>
    public int Status { get; set; }

    public string? InstructorNote { get; set; }
    public string? AdminNote { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Lesson? Lesson { get; set; }
}
