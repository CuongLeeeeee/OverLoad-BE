using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.API.Models.Mappings;

/// <summary>Enrollments, progress and chat.</summary>
public static class LearningModelMapper
{
    public static CreateEnrollmentRequest ToServiceDto(this GrantEnrollmentRequestModel model) => new()
    {
        UserId = model.UserId,
        CourseId = model.CourseId
    };

    public static EnrollmentQueryParams ToServiceDto(this EnrollmentQueryRequestModel model) =>
        new EnrollmentQueryParams { UserId = model.UserId, CourseId = model.CourseId }.WithPaging(model);

    public static SaveProgressRequest ToServiceDto(this SaveProgressRequestModel model) => new()
    {
        LastScrollPercentage = model.LastScrollPercentage,
        UnlockedCheckpointIndex = model.UnlockedCheckpointIndex,
        LastPositionSeconds = model.LastPositionSeconds,
        WatchTimeSeconds = model.WatchTimeSeconds,
        Completed = model.Completed
    };

    public static ProgressQueryParams ToServiceDto(this ProgressQueryRequestModel model) => new ProgressQueryParams
    {
        UserId = model.UserId,
        LessonId = model.LessonId,
        Completed = model.Completed
    }.WithPaging(model);

    public static ChatRequest ToServiceDto(this ChatRequestModel model) => new()
    {
        Message = model.Message,
        RecentHistory = model.RecentHistory
            .Select(h => new ChatHistoryItem { Role = h.Role, Content = h.Content })
            .ToList()
    };

    public static EnrollmentResponseModel ToResponseModel(this EnrollmentDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        UserFullName = dto.UserFullName,
        UserEmail = dto.UserEmail,
        CourseId = dto.CourseId,
        CourseTitle = dto.CourseTitle,
        CourseSlug = dto.CourseSlug,
        EnrolledAt = dto.EnrolledAt,
        CompletedAt = dto.CompletedAt,
        ProgressPercentage = dto.ProgressPercentage,
        LastAccessedAt = dto.LastAccessedAt
    };

    public static MyCourseResponseModel ToResponseModel(this MyCourseDto dto) => new()
    {
        CourseId = dto.CourseId,
        Title = dto.Title,
        ThumbnailUrl = dto.ThumbnailUrl,
        Category = dto.Category,
        Level = dto.Level,
        Price = dto.Price,
        ProgressPercentage = dto.ProgressPercentage,
        CompletedLessons = dto.CompletedLessons,
        TotalLessons = dto.TotalLessons,
        EnrolledAt = dto.EnrolledAt,
        LastAccessedAt = dto.LastAccessedAt,
        CompletedAt = dto.CompletedAt,
        IsCompleted = dto.IsCompleted
    };

    public static CourseProgressResponseModel ToResponseModel(this CourseProgressDto dto) => new()
    {
        CourseId = dto.CourseId,
        CourseTitle = dto.CourseTitle,
        ProgressPercentage = dto.ProgressPercentage,
        CompletedLessons = dto.CompletedLessons,
        TotalLessons = dto.TotalLessons,
        LastAccessedAt = dto.LastAccessedAt,
        IsCompleted = dto.IsCompleted
    };

    public static ProgressResponseModel ToResponseModel(this ProgressDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        UserFullName = dto.UserFullName,
        UserEmail = dto.UserEmail,
        LessonId = dto.LessonId,
        LessonTitle = dto.LessonTitle,
        CourseId = dto.CourseId,
        CourseTitle = dto.CourseTitle,
        LastScrollPercentage = dto.LastScrollPercentage,
        UnlockedCheckpointIndex = dto.UnlockedCheckpointIndex,
        Completed = dto.Completed,
        CompletedAt = dto.CompletedAt,
        LastPositionSeconds = dto.LastPositionSeconds,
        WatchTimeSeconds = dto.WatchTimeSeconds,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    public static ChatResponseModel ToResponseModel(this ChatReplyDto dto) => new()
    {
        Reply = dto.Reply,
        IsBlocked = dto.IsBlocked,
        BlockReason = dto.BlockReason,
        InputTokens = dto.InputTokens,
        OutputTokens = dto.OutputTokens
    };
}
