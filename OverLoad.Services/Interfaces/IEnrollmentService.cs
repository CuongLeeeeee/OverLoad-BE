using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IEnrollmentService
{
    Task<PagedResult<EnrollmentDto>> GetAllAsync(EnrollmentQueryParams query);
    Task<EnrollmentDto> GetByIdAsync(int id);

    /// <summary>A user enrolling themselves in a published course: free courses, or any course with active PRO.</summary>
    Task<EnrollmentDto> SelfEnrollAsync(int userId, int courseId);

    /// <summary>An admin granting access to any course, bypassing payment.</summary>
    Task<EnrollmentDto> GrantAsync(CreateEnrollmentRequest request);

    Task UnenrollAsync(int id);
    Task<List<MyCourseDto>> GetMyCoursesAsync(int userId);
    Task<CourseProgressDto> GetCourseProgressAsync(int userId, int courseId);
}
