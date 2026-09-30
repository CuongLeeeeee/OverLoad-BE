using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IEnrollmentService
{
    Task<PagedResult<EnrollmentResponse>> GetAllAsync(EnrollmentQueryParams query);
    Task<EnrollmentResponse> GetByIdAsync(int id);

    /// <summary>A user enrolling themselves in a published course: free courses, or any course with active PRO.</summary>
    Task<EnrollmentResponse> SelfEnrollAsync(int userId, int courseId);

    /// <summary>An admin granting access to any course, bypassing payment.</summary>
    Task<EnrollmentResponse> GrantAsync(CreateEnrollmentRequest request);

    Task UnenrollAsync(int id);
    Task<List<MyCourseResponse>> GetMyCoursesAsync(int userId);
    Task<CourseProgressResponse> GetCourseProgressAsync(int userId, int courseId);
}
