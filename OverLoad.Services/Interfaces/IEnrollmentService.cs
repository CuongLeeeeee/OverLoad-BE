using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IEnrollmentService
{
    Task<ApiResponse<EnrollmentResponse>> GetByIdAsync(int id);
    Task<PagedResponse<EnrollmentResponse>> GetAllAsync(EnrollmentQueryParams query);
    Task<ApiResponse<List<EnrollmentResponse>>> GetByUserIdAsync(int userId);
    Task<ApiResponse<List<EnrollmentResponse>>> GetByCourseIdAsync(int courseId);
    Task<ApiResponse<EnrollmentResponse>> EnrollAsync(CreateEnrollmentRequest request);
    Task<ApiResponse<EnrollmentResponse>> UpdateProgressAsync(int id, UpdateEnrollmentRequest request);
    Task<ApiResponse<bool>> UnenrollAsync(int id);
}
