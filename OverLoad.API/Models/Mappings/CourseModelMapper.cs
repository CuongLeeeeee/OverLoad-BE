using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.API.Models.Mappings;

public static class CourseModelMapper
{
    public static CreateCourseRequest ToServiceDto(this CreateCourseRequestModel model) => new()
    {
        Title = model.Title,
        Description = model.Description,
        ThumbnailUrl = model.ThumbnailUrl,
        Category = model.Category,
        Level = model.Level,
        IsPublished = model.IsPublished
    };

    public static UpdateCourseRequest ToServiceDto(this UpdateCourseRequestModel model) => new()
    {
        Title = model.Title,
        Description = model.Description,
        ThumbnailUrl = model.ThumbnailUrl,
        Category = model.Category,
        Level = model.Level,
        IsPublished = model.IsPublished
    };

    public static CourseQueryParams ToServiceDto(this CourseQueryRequestModel model) => new CourseQueryParams
    {
        Search = model.Search,
        Category = model.Category,
        Level = model.Level,
        IsPublished = model.IsPublished
    }.WithPaging(model);

    public static CourseResponseModel ToResponseModel(this CourseDto dto) => Fill(new CourseResponseModel(), dto);

    public static CourseDetailResponseModel ToResponseModel(this CourseDetailDto dto)
    {
        var model = Fill(new CourseDetailResponseModel(), dto);
        model.EnrollmentCount = dto.EnrollmentCount;
        model.Lessons = dto.Lessons.Select(l => new LessonSummaryResponseModel
        {
            Id = l.Id,
            Title = l.Title,
            DurationMinutes = l.DurationMinutes,
            OrderIndex = l.OrderIndex,
            IsFree = l.IsFree
        }).ToList();
        return model;
    }

    private static TModel Fill<TModel>(TModel model, CourseDto dto) where TModel : CourseResponseModel
    {
        model.Id = dto.Id;
        model.Title = dto.Title;
        model.Slug = dto.Slug;
        model.Description = dto.Description;
        model.ThumbnailUrl = dto.ThumbnailUrl;
        model.Category = dto.Category;
        model.Level = dto.Level;
        model.IsPublished = dto.IsPublished;
        model.TotalDurationMinutes = dto.TotalDurationMinutes;
        model.TotalLessons = dto.TotalLessons;
        model.CreatedAt = dto.CreatedAt;
        model.UpdatedAt = dto.UpdatedAt;
        return model;
    }
}
