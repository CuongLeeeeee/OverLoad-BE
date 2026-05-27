using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IProgressService
{
    Task<ApiResponse<ProgressResponse>> GetByIdAsync(int id);
    Task<ApiResponse<ProgressResponse>> GetByUserAndLessonAsync(int userId, int lessonId);
    Task<ApiResponse<List<ProgressResponse>>> GetByUserIdAsync(int userId);
    Task<ApiResponse<ProgressResponse>> UpsertAsync(UpsertProgressRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
