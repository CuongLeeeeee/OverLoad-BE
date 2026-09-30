using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserLessonProgressRepository _progressRepository;
    private readonly ICourseAccessService _courseAccess;

    public LessonService(
        ILessonRepository lessonRepository,
        ICourseRepository courseRepository,
        IUserLessonProgressRepository progressRepository,
        ICourseAccessService courseAccess)
    {
        _lessonRepository = lessonRepository;
        _courseRepository = courseRepository;
        _progressRepository = progressRepository;
        _courseAccess = courseAccess;
    }

    public async Task<PagedResult<LessonDto>> GetAllAsync(LessonQueryParams query)
    {
        var (sortBy, sortDesc) = query.ParseSort();
        var (items, total) = await _lessonRepository.SearchAsync(
            query.CourseId, query.Search, query.IsFree,
            query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<LessonDto>.Create(items.Select(MapToResponse), total, query);
    }

    public async Task<LessonDto> GetByIdAsync(int id, int? userId, bool isStaff)
    {
        var lesson = await _lessonRepository.GetWithCourseAsync(id);
        if (lesson == null || (!lesson.Course.IsPublished && !isStaff))
            throw new NotFoundException("Lesson", id);

        if (!lesson.IsFree && !isStaff)
        {
            if (userId == null)
                throw new UnauthorizedException("Sign in to access this lesson.");

            if (!await _courseAccess.HasPaidContentAccessAsync(userId.Value, lesson.CourseId))
                throw new ForbiddenException("Enroll in this course or upgrade to PRO to access this lesson.");
        }

        return MapToResponse(lesson);
    }

    public async Task<List<LessonProgressDto>> GetByCourseIdWithProgressAsync(int courseId, int? userId, bool isStaff)
    {
        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course == null || (!course.IsPublished && !isStaff))
            throw new NotFoundException("Course", courseId);

        var lessonList = (await _lessonRepository.GetByCourseIdAsync(courseId))
            .OrderBy(l => l.OrderIndex)
            .ToList();

        var hasAccess = false;
        List<UserLessonProgress> progressList = new();
        if (userId.HasValue)
        {
            hasAccess = await _courseAccess.HasPaidContentAccessAsync(userId.Value, courseId);

            var lessonIds = lessonList.Select(l => l.Id).ToHashSet();
            progressList = (await _progressRepository.GetByUserIdAsync(userId.Value))
                .Where(p => lessonIds.Contains(p.LessonId))
                .ToList();
        }

        return lessonList.Select(lesson =>
        {
            var progress = progressList.FirstOrDefault(p => p.LessonId == lesson.Id);

            // A paid lesson unlocks once the user has access (enrolled or PRO) and has completed the previous lesson.
            var previousLesson = lessonList.FirstOrDefault(l => l.OrderIndex == lesson.OrderIndex - 1);
            var previousCompleted = previousLesson == null
                || progressList.Any(p => p.LessonId == previousLesson.Id && p.Completed);

            var isLocked = !isStaff && !lesson.IsFree && (!hasAccess || !previousCompleted);

            var watchPercentage = progress != null && lesson.DurationMinutes > 0
                ? Math.Clamp(
                    Math.Round((decimal)progress.LastPositionSeconds / (lesson.DurationMinutes * 60) * 100, 1),
                    0, 100)
                : 0;

            return new LessonProgressDto
            {
                Id = lesson.Id,
                Title = lesson.Title,
                DurationMinutes = lesson.DurationMinutes,
                OrderIndex = lesson.OrderIndex,
                IsFree = lesson.IsFree,
                Completed = progress?.Completed ?? false,
                WatchPercentage = watchPercentage,
                LastPositionSeconds = progress?.LastPositionSeconds ?? 0,
                IsLocked = isLocked
            };
        }).ToList();
    }

    public async Task<LessonDto> CreateAsync(CreateLessonRequest request)
    {
        var course = await _courseRepository.GetByIdAsync(request.CourseId)
            ?? throw new NotFoundException("Course", request.CourseId);

        var lesson = new Lesson
        {
            CourseId = request.CourseId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Content = request.Content,
            DurationMinutes = request.DurationMinutes,
            OrderIndex = await _lessonRepository.GetNextOrderIndexAsync(request.CourseId),
            IsFree = request.IsFree
        };

        var created = await _lessonRepository.AddAsync(lesson);
        await SyncCourseTotalsAsync(course);

        created.Course = course;
        return MapToResponse(created);
    }

    public async Task<LessonDto> UpdateAsync(int id, UpdateLessonRequest request)
    {
        var lesson = await _lessonRepository.GetWithCourseAsync(id)
            ?? throw new NotFoundException("Lesson", id);

        lesson.Title = request.Title.Trim();
        lesson.Description = request.Description;
        lesson.Content = request.Content;
        lesson.DurationMinutes = request.DurationMinutes;
        lesson.OrderIndex = request.OrderIndex;
        lesson.IsFree = request.IsFree;

        await _lessonRepository.UpdateAsync(lesson);
        await SyncCourseTotalsAsync(lesson.Course);

        return MapToResponse(lesson);
    }

    public async Task DeleteAsync(int id)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Lesson", id);

        var courseId = lesson.CourseId;
        await _lessonRepository.DeleteAsync(id);

        var course = await _courseRepository.GetByIdAsync(courseId);
        if (course != null) await SyncCourseTotalsAsync(course);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task SyncCourseTotalsAsync(Course course)
    {
        var lessons = (await _lessonRepository.GetByCourseIdAsync(course.Id)).ToList();
        course.TotalLessons = lessons.Count;
        course.TotalDurationMinutes = lessons.Sum(l => l.DurationMinutes);
        await _courseRepository.UpdateAsync(course);
    }

    private static LessonDto MapToResponse(Lesson l) => new()
    {
        Id = l.Id,
        CourseId = l.CourseId,
        CourseTitle = l.Course?.Title ?? string.Empty,
        Title = l.Title,
        Description = l.Description,
        Content = l.Content,
        DurationMinutes = l.DurationMinutes,
        OrderIndex = l.OrderIndex,
        IsFree = l.IsFree,
        CreatedAt = l.CreatedAt,
        UpdatedAt = l.UpdatedAt
    };
}
