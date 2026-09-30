using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.API.Models.Mappings;

public static class LessonModelMapper
{
    public static CreateLessonRequest ToServiceDto(this CreateLessonRequestModel model) => new()
    {
        CourseId = model.CourseId,
        Title = model.Title,
        Description = model.Description,
        Content = model.Content,
        DurationMinutes = model.DurationMinutes,
        IsFree = model.IsFree
    };

    public static UpdateLessonRequest ToServiceDto(this UpdateLessonRequestModel model) => new()
    {
        Title = model.Title,
        Description = model.Description,
        Content = model.Content,
        DurationMinutes = model.DurationMinutes,
        OrderIndex = model.OrderIndex,
        IsFree = model.IsFree
    };

    public static LessonQueryParams ToServiceDto(this LessonQueryRequestModel model) => new LessonQueryParams
    {
        CourseId = model.CourseId,
        Search = model.Search,
        IsFree = model.IsFree
    }.WithPaging(model);

    public static LessonResponseModel ToResponseModel(this LessonDto dto) => new()
    {
        Id = dto.Id,
        CourseId = dto.CourseId,
        CourseTitle = dto.CourseTitle,
        Title = dto.Title,
        Description = dto.Description,
        Content = dto.Content,
        DurationMinutes = dto.DurationMinutes,
        OrderIndex = dto.OrderIndex,
        IsFree = dto.IsFree,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };

    public static LessonProgressResponseModel ToResponseModel(this LessonProgressDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        DurationMinutes = dto.DurationMinutes,
        OrderIndex = dto.OrderIndex,
        IsFree = dto.IsFree,
        Completed = dto.Completed,
        WatchPercentage = dto.WatchPercentage,
        LastPositionSeconds = dto.LastPositionSeconds,
        IsLocked = dto.IsLocked
    };
}
