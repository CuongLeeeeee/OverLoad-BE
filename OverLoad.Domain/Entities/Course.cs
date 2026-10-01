using OverLoad.Domain.Common;
using OverLoad.Domain.Enums;

namespace OverLoad.Domain.Entities;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public decimal Price { get; set; } = 0;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public string? RejectionReason { get; set; }
    public int InstructorId { get; set; } = 3;
    public int TotalDurationMinutes { get; set; }
    public int TotalLessons { get; set; }

    // Derived — not stored in DB
    public bool IsPublished => Status == CourseStatus.Published;

    // Navigation
    public User? Instructor { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
