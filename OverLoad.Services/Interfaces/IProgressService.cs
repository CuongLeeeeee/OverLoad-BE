using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IProgressService
{
    /// <summary>Reporting view for staff.</summary>
    Task<PagedResult<ProgressDto>> GetAllAsync(ProgressQueryParams query);

    Task<ProgressDto> GetMyLessonProgressAsync(int userId, int lessonId);

    /// <summary>Creates or updates the caller's progress on a lesson.</summary>
    Task<ProgressDto> SaveMyLessonProgressAsync(int userId, int lessonId, SaveProgressRequest request);

    Task<ProgressDto> CompleteLessonAsync(int userId, int lessonId);
}
