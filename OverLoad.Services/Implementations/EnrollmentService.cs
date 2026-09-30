using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserLessonProgressRepository _progressRepository;
    private readonly ICourseAccessService _courseAccess;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IUserRepository userRepository,
        ICourseRepository courseRepository,
        IUserLessonProgressRepository progressRepository,
        ICourseAccessService courseAccess)
    {
        _enrollmentRepository = enrollmentRepository;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _progressRepository = progressRepository;
        _courseAccess = courseAccess;
    }

    public async Task<PagedResult<EnrollmentResponse>> GetAllAsync(EnrollmentQueryParams query)
    {
        var (sortBy, sortDesc) = query.ParseSort();
        var (items, total) = await _enrollmentRepository.SearchAsync(
            query.UserId, query.CourseId, query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<EnrollmentResponse>.Create(items.Select(MapToResponse), total, query);
    }

    public async Task<EnrollmentResponse> GetByIdAsync(int id)
    {
        var enrollment = await _enrollmentRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Enrollment", id);

        // Hydrate navigation props for the response
        enrollment.User ??= (await _userRepository.GetByIdAsync(enrollment.UserId))!;
        enrollment.Course ??= (await _courseRepository.GetByIdAsync(enrollment.CourseId))!;

        return MapToResponse(enrollment);
    }

    public async Task<EnrollmentResponse> SelfEnrollAsync(int userId, int courseId)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || !course.IsPublished)
            throw new NotFoundException("Course", courseId);

        if (course.Price > 0 && !await _courseAccess.HasActiveProAsync(userId))
            throw new BusinessRuleException("This is a paid course. Purchase it or upgrade to PRO to enroll.");

        return await CreateEnrollmentAsync(userId, course);
    }

    public async Task<EnrollmentResponse> GrantAsync(CreateEnrollmentRequest request)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId)
            ?? throw new NotFoundException("Course", request.CourseId);

        return await CreateEnrollmentAsync(request.UserId, course);
    }

    public async Task UnenrollAsync(int id)
    {
        if (!await _enrollmentRepository.ExistsAsync(id))
            throw new NotFoundException("Enrollment", id);

        await _enrollmentRepository.DeleteAsync(id);
    }

    public async Task<List<MyCourseResponse>> GetMyCoursesAsync(int userId)
    {
        var enrollments = await _enrollmentRepository.GetByUserIdWithDetailsAsync(userId);

        return enrollments.Select(e =>
        {
            var totalLessons = e.Course?.Lessons?.Count ?? 0;
            var completedLessons = totalLessons > 0
                ? (int)Math.Round(totalLessons * (double)e.ProgressPercentage / 100)
                : 0;

            return new MyCourseResponse
            {
                CourseId = e.CourseId,
                Title = e.Course?.Title ?? string.Empty,
                ThumbnailUrl = e.Course?.ThumbnailUrl,
                Category = e.Course?.Category,
                Level = e.Course?.Level.ToString() ?? string.Empty,
                Price = e.Course?.Price ?? 0,
                ProgressPercentage = e.ProgressPercentage,
                CompletedLessons = completedLessons,
                TotalLessons = totalLessons,
                EnrolledAt = e.EnrolledAt,
                LastAccessedAt = e.LastAccessedAt,
                CompletedAt = e.CompletedAt,
                IsCompleted = e.CompletedAt.HasValue
            };
        }).ToList();
    }

    public async Task<CourseProgressResponse> GetCourseProgressAsync(int userId, int courseId)
    {
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, courseId)
            ?? throw new NotFoundException("You are not enrolled in this course.");

        var course = await _courseRepository.GetWithLessonsAsync(courseId)
            ?? throw new NotFoundException("Course", courseId);

        var lessonIds = course.Lessons.Select(l => l.Id).ToHashSet();
        var completedLessons = (await _progressRepository.GetByUserIdAsync(userId))
            .Count(p => lessonIds.Contains(p.LessonId) && p.Completed);

        return new CourseProgressResponse
        {
            CourseId = courseId,
            CourseTitle = course.Title,
            ProgressPercentage = enrollment.ProgressPercentage,
            CompletedLessons = completedLessons,
            TotalLessons = lessonIds.Count,
            LastAccessedAt = enrollment.LastAccessedAt,
            IsCompleted = enrollment.CompletedAt.HasValue
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EnrollmentResponse> CreateEnrollmentAsync(int userId, Course course)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (await _enrollmentRepository.IsEnrolledAsync(userId, course.Id))
            throw new ConflictException("User is already enrolled in this course.");

        var created = await _enrollmentRepository.AddAsync(new Enrollment
        {
            UserId = userId,
            CourseId = course.Id,
            EnrolledAt = DateTime.UtcNow
        });

        created.User = user;
        created.Course = course;
        return MapToResponse(created);
    }

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
