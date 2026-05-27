namespace OverLoad.Services.DTOs.Response;

public class CourseResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Category { get; set; }
    public string Level { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalLessons { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CourseDetailResponse : CourseResponse
{
    public List<LessonSummaryResponse> Lessons { get; set; } = new();
    public int EnrollmentCount { get; set; }
}

public class LessonSummaryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public bool IsFree { get; set; }
}
