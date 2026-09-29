using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ICourseService
{
    /// <param name="includeUnpublished">True for staff; everyone else only ever sees published courses.</param>
    Task<PagedResult<CourseResponse>> GetAllAsync(CourseQueryParams query, bool includeUnpublished);
    Task<CourseDetailResponse> GetByIdAsync(int id, bool includeUnpublished);
    Task<CourseDetailResponse> GetBySlugAsync(string slug, bool includeUnpublished);
    Task<CourseResponse> CreateAsync(CreateCourseRequest request);
    Task<CourseResponse> UpdateAsync(int id, UpdateCourseRequest request);
    Task DeleteAsync(int id);
}
