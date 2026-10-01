using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ICourseService
{
    /// <param name="includeUnpublished">True for staff; everyone else only sees Published courses.</param>
    Task<PagedResult<CourseDto>> GetAllAsync(CourseQueryParams query, bool includeUnpublished);
    Task<CourseDetailDto> GetByIdAsync(int id, bool includeUnpublished);
    Task<CourseDetailDto> GetBySlugAsync(string slug, bool includeUnpublished);

    /// <summary>Returns all courses the given instructor owns, in all statuses.</summary>
    Task<List<CourseDto>> GetInstructorCoursesAsync(int instructorId);

    /// <summary>Courses in PendingReview status (admin approval queue).</summary>
    Task<PagedResult<CourseDto>> GetPendingReviewAsync(CourseQueryParams query);

    // ── Write (Instructor) ───────────────────────────────────────────────────
    Task<CourseDto> CreateAsync(CreateCourseRequest request, int instructorId);

    /// <summary>Ownership check: Instructors may only edit their own Draft/Rejected courses.</summary>
    Task<CourseDto> UpdateAsync(int id, UpdateCourseRequest request, int callerId, bool isAdmin);

    Task DeleteAsync(int id, int callerId, bool isAdmin);

    Task<CourseDto> SubmitForReviewAsync(int id, int instructorId);

    // ── Approval (Admin) ─────────────────────────────────────────────────────
    Task<CourseDto> ApproveCourseAsync(int id);
    Task<CourseDto> RejectCourseAsync(int id, string reason);
    Task<CourseDto> SetPriceAsync(int id, decimal price);
}
