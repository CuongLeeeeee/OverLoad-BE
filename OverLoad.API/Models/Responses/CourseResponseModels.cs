namespace OverLoad.API.Models.Responses;

public class CourseResponseModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public string? RejectionReason { get; set; }
    public int InstructorId { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalLessons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CourseDetailResponseModel : CourseResponseModel
{
    public List<LessonSummaryResponseModel> Lessons { get; set; } = new();
    public int EnrollmentCount { get; set; }
}

public class LessonSummaryResponseModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
}

public class LessonResponseModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; }
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LessonProgressResponseModel
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
