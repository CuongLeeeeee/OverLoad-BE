// OverLoad.Services/Interfaces/IProgressService.cs
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IProgressService
{
    Task<ApiResponse<ProgressResponse>> GetByIdAsync(int id);
    Task<PagedResponse<ProgressResponse>> GetAllAsync(ProgressQueryParams query);
    Task<ApiResponse<ProgressResponse>> GetByUserAndLessonAsync(int userId, int lessonId);
    Task<ApiResponse<List<ProgressResponse>>> GetByUserIdAsync(int userId);
    Task<ApiResponse<List<ProgressResponse>>> GetByLessonIdAsync(int lessonId);
    Task<ApiResponse<ProgressResponse>> CreateAsync(CreateProgressRequest request);
    Task<ApiResponse<ProgressResponse>> UpdateAsync(int id, UpdateProgressRequest request);
    Task<ApiResponse<ProgressResponse>> UpsertAsync(CreateProgressRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}