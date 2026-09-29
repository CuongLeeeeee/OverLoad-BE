using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class ProgressService : IProgressService
{
    private readonly IUserLessonProgressRepository _progressRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public ProgressService(
        IUserLessonProgressRepository progressRepository,
        ILessonRepository lessonRepository,
        IEnrollmentRepository enrollmentRepository)
    {
        _progressRepository = progressRepository;
        _lessonRepository = lessonRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task<PagedResult<ProgressResponse>> GetAllAsync(ProgressQueryParams query)
    {
        var (sortBy, sortDesc) = query.ParseSort();
        var (items, total) = await _progressRepository.SearchAsync(
            query.UserId, query.LessonId, query.Completed,
            query.PageNumber, query.PageSize, sortBy, sortDesc);

        return PagedResult<ProgressResponse>.Create(items.Select(MapToResponse), total, query);
    }

    public async Task<ProgressResponse> GetMyLessonProgressAsync(int userId, int lessonId)
    {
        var progress = await _progressRepository.GetByUserAndLessonAsync(userId, lessonId)
            ?? throw new NotFoundException("No progress recorded for this lesson yet.");

        return MapToResponse(progress);
    }

    public async Task<ProgressResponse> SaveMyLessonProgressAsync(int userId, int lessonId, SaveProgressRequest request)
    {
        var lesson = await GetAccessibleLessonAsync(userId, lessonId);
        var progress = await _progressRepository.GetByUserAndLessonAsync(userId, lessonId);
        var isNew = progress == null;
        progress ??= new UserLessonProgress { UserId = userId, LessonId = lessonId };

        progress.LastScrollPercentage = request.LastScrollPercentage;
        progress.UnlockedCheckpointIndex = request.UnlockedCheckpointIndex;
        progress.LastPositionSeconds = request.LastPositionSeconds;
        progress.WatchTimeSeconds = request.WatchTimeSeconds;

        // Completion is one-way: once completed, a later report cannot un-complete the lesson.
        var becameCompleted = request.Completed && !progress.Completed;
        if (becameCompleted)
        {
            progress.Completed = true;
            progress.CompletedAt = DateTime.UtcNow;
        }

        if (isNew) await _progressRepository.AddAsync(progress);
        else await _progressRepository.UpdateAsync(progress);

        await TouchEnrollmentAsync(userId, lesson.CourseId, recalculate: becameCompleted);

        return MapToResponse((await _progressRepository.GetByUserAndLessonAsync(userId, lessonId))!);
    }

    public async Task<ProgressResponse> CompleteLessonAsync(int userId, int lessonId)
    {
        var lesson = await GetAccessibleLessonAsync(userId, lessonId);
        var progress = await _progressRepository.GetByUserAndLessonAsync(userId, lessonId);

        if (progress == null)
        {
            await _progressRepository.AddAsync(new UserLessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                Completed = true,
                CompletedAt = DateTime.UtcNow,
                LastScrollPercentage = 100
            });
        }
        else if (!progress.Completed)
        {
            progress.Completed = true;
            progress.CompletedAt = DateTime.UtcNow;
            await _progressRepository.UpdateAsync(progress);
        }

        await TouchEnrollmentAsync(userId, lesson.CourseId, recalculate: true);

        return MapToResponse((await _progressRepository.GetByUserAndLessonAsync(userId, lessonId))!);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Free lessons are open to any signed-in user; paid lessons need an enrollment.</summary>
    private async Task<Lesson> GetAccessibleLessonAsync(int userId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId)
            ?? throw new NotFoundException("Lesson", lessonId);

        if (!lesson.IsFree && !await _enrollmentRepository.IsEnrolledAsync(userId, lesson.CourseId))
            throw new ForbiddenException("Enroll in this course to track progress on this lesson.");

        return lesson;
    }

    /// <summary>
    /// Updates LastAccessedAt and, when asked, recomputes the course percentage from completed
    /// lessons on the server (the client no longer sends it).
    /// </summary>
    private async Task TouchEnrollmentAsync(int userId, int courseId, bool recalculate)
    {
        var enrollment = await _enrollmentRepository.GetByUserAndCourseAsync(userId, courseId);
        if (enrollment == null) return;

        enrollment.LastAccessedAt = DateTime.UtcNow;

        if (recalculate)
        {
            var lessonIds = (await _lessonRepository.GetByCourseIdAsync(courseId)).Select(l => l.Id).ToHashSet();
            var completed = (await _progressRepository.GetByUserIdAsync(userId))
                .Count(p => lessonIds.Contains(p.LessonId) && p.Completed);

            enrollment.ProgressPercentage = lessonIds.Count == 0
                ? 0
                : Math.Round(completed * 100m / lessonIds.Count, 2);

            if (enrollment.ProgressPercentage >= 100 && enrollment.CompletedAt == null)
                enrollment.CompletedAt = DateTime.UtcNow;
        }

        await _enrollmentRepository.UpdateAsync(enrollment);
    }

    private static ProgressResponse MapToResponse(UserLessonProgress p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        UserFullName = p.User?.FullName ?? string.Empty,
        UserEmail = p.User?.Email ?? string.Empty,
        LessonId = p.LessonId,
        LessonTitle = p.Lesson?.Title ?? string.Empty,
        CourseId = p.Lesson?.CourseId ?? 0,
        CourseTitle = p.Lesson?.Course?.Title ?? string.Empty,
        LastScrollPercentage = p.LastScrollPercentage,
        UnlockedCheckpointIndex = p.UnlockedCheckpointIndex,
        Completed = p.Completed,
        CompletedAt = p.CompletedAt,
        LastPositionSeconds = p.LastPositionSeconds,
        WatchTimeSeconds = p.WatchTimeSeconds,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
