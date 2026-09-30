using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ICourseService
{
    /// <param name="includeUnpublished">True for staff; everyone else only ever sees published courses.</param>
    Task<PagedResult<CourseDto>> GetAllAsync(CourseQueryParams query, bool includeUnpublished);
    Task<CourseDetailDto> GetByIdAsync(int id, bool includeUnpublished);
    Task<CourseDetailDto> GetBySlugAsync(string slug, bool includeUnpublished);
    Task<CourseDto> CreateAsync(CreateCourseRequest request);
    Task<CourseDto> UpdateAsync(int id, UpdateCourseRequest request);
    Task DeleteAsync(int id);
}
