using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDetailResponse>> GetByIdAsync(int id);
    Task<PagedResponse<UserResponse>> GetAllAsync(UserQueryParams query);
    Task<ApiResponse<UserResponse>> CreateAsync(CreateUserRequest request);
    Task<ApiResponse<UserResponse>> UpdateAsync(int id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
