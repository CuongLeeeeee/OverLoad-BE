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
        var progress = await _progressRepository.GetByIdAsync(id);
        if (progress == null)
            return ApiResponse<ProgressResponse>.FailResult("Progress record not found.");

        var lesson = await _lessonRepository.GetByIdAsync(progress.LessonId);
        progress.Lesson = lesson!;

        return ApiResponse<ProgressResponse>.SuccessResult(MapToResponse(progress));
    }

    public async Task<ApiResponse<ProgressResponse>> GetByUserAndLessonAsync(int userId, int lessonId)
    {
        var progress = await _progressRepository.GetByUserAndLessonAsync(userId, lessonId);
        if (progress == null)
            return ApiResponse<ProgressResponse>.FailResult("Progress record not found.");

        var lesson = await _lessonRepository.GetByIdAsync(progress.LessonId);
        progress.Lesson = lesson!;

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

    public async Task<ApiResponse<ProgressResponse>> UpsertAsync(UpsertProgressRequest request)
    {
        if (!await _userRepository.ExistsAsync(request.UserId))
            return ApiResponse<ProgressResponse>.FailResult("User not found.");

        var lesson = await _lessonRepository.GetByIdAsync(request.LessonId);
        if (lesson == null)
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
            existing.Lesson = lesson;
            return ApiResponse<ProgressResponse>.SuccessResult(MapToResponse(existing), "Progress updated.");
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

        var created = await _progressRepository.AddAsync(progress);
        created.Lesson = lesson;

        return ApiResponse<ProgressResponse>.SuccessResult(MapToResponse(created), "Progress created.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        if (!await _progressRepository.ExistsAsync(id))
            return ApiResponse<bool>.FailResult("Progress record not found.");

        await _progressRepository.DeleteAsync(id);
        return ApiResponse<bool>.SuccessResult(true, "Progress record deleted.");
    }

    private static ProgressResponse MapToResponse(UserLessonProgress p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        LessonId = p.LessonId,
        LessonTitle = p.Lesson?.Title ?? string.Empty,
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
