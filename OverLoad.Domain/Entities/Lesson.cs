using OverLoad.Domain.Common;

namespace OverLoad.Domain.Entities;

public class Lesson : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; } = false;

    // Navigation
    public Course Course { get; set; } = null!;
    public ICollection<UserLessonProgress> UserProgresses { get; set; } = new List<UserLessonProgress>();
}
