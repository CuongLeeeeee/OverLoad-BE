namespace OverLoad.Services.Interfaces;

/// <summary>Single place that decides who may use the paid content of a course.</summary>
public interface ICourseAccessService
{
    /// <summary>True when the user is enrolled in the course or has an active PRO subscription.</summary>
    Task<bool> HasPaidContentAccessAsync(int userId, int courseId);

    Task<bool> HasActiveProAsync(int userId);
}
