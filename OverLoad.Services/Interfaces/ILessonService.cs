using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ILessonService
{
    Task<PagedResult<LessonDto>> GetAllAsync(LessonQueryParams query);

    /// <summary>
    /// Returns the full lesson content. Paid lessons require the caller to be enrolled in the course
    /// (or be staff); draft courses are hidden from non-staff.
    /// </summary>
    Task<LessonDto> GetByIdAsync(int id, int? userId, bool isStaff);

    Task<List<LessonProgressDto>> GetByCourseIdWithProgressAsync(int courseId, int? userId, bool isStaff);
    Task<LessonDto> CreateAsync(CreateLessonRequest request);
    Task<LessonDto> UpdateAsync(int id, UpdateLessonRequest request);
    Task DeleteAsync(int id);
}
