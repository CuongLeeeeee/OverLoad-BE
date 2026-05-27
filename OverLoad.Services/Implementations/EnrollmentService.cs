using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICourseRepository _courseRepository;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IUserRepository userRepository,
        ICourseRepository courseRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
    }

    public async Task<ApiResponse<EnrollmentResponse>> GetByIdAsync(int id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (enrollment == null)
            return ApiResponse<EnrollmentResponse>.FailResult("Enrollment not found.");

        // Hydrate navigation props if needed
        if (enrollment.User == null)
            enrollment.User = (await _userRepository.GetByIdAsync(enrollment.UserId))!;
        if (enrollment.Course == null)
            enrollment.Course = (await _courseRepository.GetByIdAsync(enrollment.CourseId))!;

        return ApiResponse<EnrollmentResponse>.SuccessResult(MapToResponse(enrollment));
    }

    public async Task<PagedResponse<EnrollmentResponse>> GetAllAsync(EnrollmentQueryParams query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await _enrollmentRepository.SearchAsync(
            query.UserId, query.CourseId, query.Page, query.PageSize, query.SortBy, query.SortDesc);

        return PagedResponse<EnrollmentResponse>.SuccessResult(
            items.Select(MapToResponse), total, query.Page, query.PageSize);
    }

    public async Task<ApiResponse<List<EnrollmentResponse>>> GetByUserIdAsync(int userId)
    {
        if (!await _userRepository.ExistsAsync(userId))
            return ApiResponse<List<EnrollmentResponse>>.FailResult("User not found.");

        var enrollments = await _enrollmentRepository.GetByUserIdAsync(userId);
        return ApiResponse<List<EnrollmentResponse>>.SuccessResult(
            enrollments.Select(MapToResponse).ToList());
    }

    public async Task<ApiResponse<List<EnrollmentResponse>>> GetByCourseIdAsync(int courseId)
    {
        if (!await _courseRepository.ExistsAsync(courseId))
            return ApiResponse<List<EnrollmentResponse>>.FailResult("Course not found.");

        var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId);
        return ApiResponse<List<EnrollmentResponse>>.SuccessResult(
            enrollments.Select(MapToResponse).ToList());
    }

    public async Task<ApiResponse<EnrollmentResponse>> EnrollAsync(CreateEnrollmentRequest request)
    {
        if (!await _userRepository.ExistsAsync(request.UserId))
            return ApiResponse<EnrollmentResponse>.FailResult("User not found.");

        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        if (course == null)
            return ApiResponse<EnrollmentResponse>.FailResult("Course not found.");

        if (await _enrollmentRepository.IsEnrolledAsync(request.UserId, request.CourseId))
            return ApiResponse<EnrollmentResponse>.FailResult("User is already enrolled in this course.");

        var enrollment = new Enrollment
        {
            UserId = request.UserId,
            CourseId = request.CourseId,
            EnrolledAt = DateTime.UtcNow
        };

        var created = await _enrollmentRepository.AddAsync(enrollment);

        var user = await _userRepository.GetByIdAsync(request.UserId);
        created.User = user!;
        created.Course = course;

        return ApiResponse<EnrollmentResponse>.SuccessResult(
            MapToResponse(created), "Enrolled successfully.");
    }

    public async Task<ApiResponse<EnrollmentResponse>> UpdateProgressAsync(int id, UpdateEnrollmentRequest request)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id);
        if (enrollment == null)
            return ApiResponse<EnrollmentResponse>.FailResult("Enrollment not found.");

        enrollment.ProgressPercentage = request.ProgressPercentage;
        enrollment.CompletedAt = request.CompletedAt;
        enrollment.LastAccessedAt = request.LastAccessedAt ?? DateTime.UtcNow;

        await _enrollmentRepository.UpdateAsync(enrollment);

        // Hydrate for response
        if (enrollment.User == null)
            enrollment.User = (await _userRepository.GetByIdAsync(enrollment.UserId))!;
        if (enrollment.Course == null)
            enrollment.Course = (await _courseRepository.GetByIdAsync(enrollment.CourseId))!;

        return ApiResponse<EnrollmentResponse>.SuccessResult(MapToResponse(enrollment), "Progress updated.");
    }

    public async Task<ApiResponse<bool>> UnenrollAsync(int id)
    {
        if (!await _enrollmentRepository.ExistsAsync(id))
            return ApiResponse<bool>.FailResult("Enrollment not found.");

        await _enrollmentRepository.DeleteAsync(id);
        return ApiResponse<bool>.SuccessResult(true, "Unenrolled successfully.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EnrollmentResponse MapToResponse(Enrollment e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        UserFullName = e.User?.FullName ?? string.Empty,
        UserEmail = e.User?.Email ?? string.Empty,
        CourseId = e.CourseId,
        CourseTitle = e.Course?.Title ?? string.Empty,
        CourseSlug = e.Course?.Slug ?? string.Empty,
        EnrolledAt = e.EnrolledAt,
        CompletedAt = e.CompletedAt,
        ProgressPercentage = e.ProgressPercentage,
        LastAccessedAt = e.LastAccessedAt
    };
}
