// OverLoad.Services/Implementations/ProgressService.cs
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Interfaces;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;

namespace OverLoad.Services.Implementations;

public class ProgressService : IProgressService
{
    private readonly IUserLessonProgressRepository _progressRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILessonRepository _lessonRepository;

    public ProgressService(
        IUserLessonProgressRepository progressRepository,
        IUserRepository userRepository,
        ILessonRepository lessonRepository)
    {
        _progressRepository = progressRepository;
        _userRepository = userRepository;
        _lessonRepository = lessonRepository;
    }

    public async Task<ApiResponse<ProgressResponse>> GetByIdAsync(int id)
    {
        var progress = await _progressRepository.GetByIdDetailAsync(id);
        if (progress == null)
            return ApiResponse<ProgressResponse>.FailResult("Progress record not found.");

        return ApiResponse<ProgressResponse>.SuccessResult(MapToResponse(progress));
    }

    public async Task<PagedResponse<ProgressResponse>> GetAllAsync(ProgressQueryParams query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await _progressRepository.SearchAsync(
            query.UserId, query.LessonId, query.Completed,
            query.Page, query.PageSize, query.SortBy, query.SortDesc);

        return PagedResponse<ProgressResponse>.SuccessResult(
            items.Select(MapToResponse), total, query.Page, query.PageSize);
    }

    public async Task<ApiResponse<ProgressResponse>> GetByUserAndLessonAsync(int userId, int lessonId)
    {
        var progress = await _progressRepository.GetByUserAndLessonAsync(userId, lessonId);
        if (progress == null)
            return ApiResponse<ProgressResponse>.FailResult("Progress record not found.");

        return ApiResponse<ProgressResponse>.SuccessResult(MapToResponse(progress));
    }

    public async Task<ApiResponse<List<ProgressResponse>>> GetByUserIdAsync(int userId)
    {
        if (!await _userRepository.ExistsAsync(userId))
            return ApiResponse<List<ProgressResponse>>.FailResult("User not found.");

        var records = await _progressRepository.GetByUserIdAsync(userId);
        return ApiResponse<List<ProgressResponse>>.SuccessResult(
            records.Select(MapToResponse).ToList());
    }

    public async Task<ApiResponse<List<ProgressResponse>>> GetByLessonIdAsync(int lessonId)
    {
        if (!await _lessonRepository.ExistsAsync(lessonId))
            return ApiResponse<List<ProgressResponse>>.FailResult("Lesson not found.");

        var records = await _progressRepository.GetByLessonIdAsync(lessonId);
        return ApiResponse<List<ProgressResponse>>.SuccessResult(
            records.Select(MapToResponse).ToList());
    }

    public async Task<ApiResponse<ProgressResponse>> CreateAsync(CreateProgressRequest request)
    {
        if (!await _userRepository.ExistsAsync(request.UserId))
            return ApiResponse<ProgressResponse>.FailResult("User not found.");

        if (!await _lessonRepository.ExistsAsync(request.LessonId))
            return ApiResponse<ProgressResponse>.FailResult("Lesson not found.");

        if (await _progressRepository.ExistsByUserAndLessonAsync(request.UserId, request.LessonId))
            return ApiResponse<ProgressResponse>.FailResult(
                "Progress record already exists. Use PUT to update or POST /upsert.");

        var progress = new UserLessonProgress
        {
            UserId = request.UserId,
            LessonId = request.LessonId,
            LastScrollPercentage = request.LastScrollPercentage,
            UnlockedCheckpointIndex = request.UnlockedCheckpointIndex,
            Completed = request.Completed,
            CompletedAt = request.Completed ? DateTime.UtcNow : null,
            LastPositionSeconds = request.LastPositionSeconds,
            WatchTimeSeconds = request.WatchTimeSeconds
        };

        await _progressRepository.AddAsync(progress);

        var created = await _progressRepository.GetByUserAndLessonAsync(request.UserId, request.LessonId);
        return ApiResponse<ProgressResponse>.SuccessResult(
            MapToResponse(created!), "Progress created successfully.");
    }

    public async Task<ApiResponse<ProgressResponse>> UpdateAsync(int id, UpdateProgressRequest request)
    {
        var progress = await _progressRepository.GetByIdDetailAsync(id);
        if (progress == null)
            return ApiResponse<ProgressResponse>.FailResult("Progress record not found.");

        progress.LastScrollPercentage = request.LastScrollPercentage;
        progress.UnlockedCheckpointIndex = request.UnlockedCheckpointIndex;
        progress.LastPositionSeconds = request.LastPositionSeconds;
        progress.WatchTimeSeconds = request.WatchTimeSeconds;

        // Chỉ set CompletedAt một lần khi chuyển sang completed
        if (!progress.Completed && request.Completed)
        {
            progress.Completed = true;
            progress.CompletedAt = DateTime.UtcNow;
        }
        else if (progress.Completed && !request.Completed)
        {
            // Cho phép reset completed
            progress.Completed = false;
            progress.CompletedAt = null;
        }

        await _progressRepository.UpdateAsync(progress);

        var updated = await _progressRepository.GetByIdDetailAsync(id);
        return ApiResponse<ProgressResponse>.SuccessResult(
            MapToResponse(updated!), "Progress updated successfully.");
    }

    public async Task<ApiResponse<ProgressResponse>> UpsertAsync(CreateProgressRequest request)
    {
        if (!await _userRepository.ExistsAsync(request.UserId))
            return ApiResponse<ProgressResponse>.FailResult("User not found.");

        if (!await _lessonRepository.ExistsAsync(request.LessonId))
            return ApiResponse<ProgressResponse>.FailResult("Lesson not found.");

        var existing = await _progressRepository.GetByUserAndLessonAsync(request.UserId, request.LessonId);

        if (existing != null)
        {
            existing.LastScrollPercentage = request.LastScrollPercentage;
            existing.UnlockedCheckpointIndex = request.UnlockedCheckpointIndex;
            existing.LastPositionSeconds = request.LastPositionSeconds;
            existing.WatchTimeSeconds = request.WatchTimeSeconds;

            if (!existing.Completed && request.Completed)
            {
                existing.Completed = true;
                existing.CompletedAt = DateTime.UtcNow;
            }

            await _progressRepository.UpdateAsync(existing);
            var updated = await _progressRepository.GetByUserAndLessonAsync(request.UserId, request.LessonId);
            return ApiResponse<ProgressResponse>.SuccessResult(
                MapToResponse(updated!), "Progress updated successfully.");
        }

        var progress = new UserLessonProgress
        {
            UserId = request.UserId,
            LessonId = request.LessonId,
            LastScrollPercentage = request.LastScrollPercentage,
            UnlockedCheckpointIndex = request.UnlockedCheckpointIndex,
            Completed = request.Completed,
            CompletedAt = request.Completed ? DateTime.UtcNow : null,
            LastPositionSeconds = request.LastPositionSeconds,
            WatchTimeSeconds = request.WatchTimeSeconds
        };

        await _progressRepository.AddAsync(progress);
        var created = await _progressRepository.GetByUserAndLessonAsync(request.UserId, request.LessonId);
        return ApiResponse<ProgressResponse>.SuccessResult(
            MapToResponse(created!), "Progress created successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (!await _progressRepository.ExistsAsync(id))
            return ApiResponse<bool>.FailResult("Progress record not found.");

        await _progressRepository.DeleteAsync(id);
        return ApiResponse<bool>.SuccessResult(true, "Progress deleted successfully.");
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

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