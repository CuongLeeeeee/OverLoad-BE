using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ILessonService
{
    Task<PagedResult<LessonResponse>> GetAllAsync(LessonQueryParams query);

    /// <summary>
    /// Returns the full lesson content. Paid lessons require the caller to be enrolled in the course
    /// (or be staff); draft courses are hidden from non-staff.
    /// </summary>
    Task<LessonResponse> GetByIdAsync(int id, int? userId, bool isStaff);

    Task<List<LessonProgressResponse>> GetByCourseIdWithProgressAsync(int courseId, int? userId, bool isStaff);
    Task<LessonResponse> CreateAsync(CreateLessonRequest request);
    Task<LessonResponse> UpdateAsync(int id, UpdateLessonRequest request);
    Task DeleteAsync(int id);
}
