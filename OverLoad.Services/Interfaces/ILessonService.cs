using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface ILessonService
{
    Task<ApiResponse<LessonResponse>> GetByIdAsync(int id);
    Task<PagedResponse<LessonResponse>> GetAllAsync(LessonQueryParams query);
    Task<ApiResponse<List<LessonResponse>>> GetByCourseIdAsync(int courseId);
    Task<ApiResponse<LessonResponse>> CreateAsync(CreateLessonRequest request);
    Task<ApiResponse<LessonResponse>> UpdateAsync(int id, UpdateLessonRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
