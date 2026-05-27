
using System.ComponentModel.DataAnnotations;

namespace OverLoad.Services.DTOs.Request;

public class CreateProgressRequest
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int LessonId { get; set; }

    [Range(0, 100)]
    public decimal LastScrollPercentage { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int UnlockedCheckpointIndex { get; set; } = 0;

    public bool Completed { get; set; } = false;

    [Range(0, int.MaxValue)]
    public int LastPositionSeconds { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int WatchTimeSeconds { get; set; } = 0;
}

public class UpdateProgressRequest
{
    [Range(0, 100)]
    public decimal LastScrollPercentage { get; set; }

    [Range(0, int.MaxValue)]
    public int UnlockedCheckpointIndex { get; set; }

    public bool Completed { get; set; }

    [Range(0, int.MaxValue)]
    public int LastPositionSeconds { get; set; }

    [Range(0, int.MaxValue)]
    public int WatchTimeSeconds { get; set; }
}

public class ProgressQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? UserId { get; set; }
    public int? LessonId { get; set; }
    public bool? Completed { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public bool SortDesc { get; set; } = false;
}