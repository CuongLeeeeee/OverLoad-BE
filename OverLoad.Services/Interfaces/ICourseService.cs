using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ICourseService
{
    Task<ApiResponse<CourseDetailResponse>> GetByIdAsync(int id);
    Task<ApiResponse<CourseDetailResponse>> GetBySlugAsync(string slug);
    Task<PagedResponse<CourseResponse>> GetAllAsync(CourseQueryParams query);
    Task<ApiResponse<CourseResponse>> CreateAsync(CreateCourseRequest request);
    Task<ApiResponse<CourseResponse>> UpdateAsync(int id, UpdateCourseRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
    Task<PagedResponse<CourseResponse>> GetByCategoryAsync(
    string category, int page = 1, int pageSize = 10);
}
