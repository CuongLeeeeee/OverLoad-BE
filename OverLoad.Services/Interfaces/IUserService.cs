using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetAllAsync(UserQueryParams query);
    Task<UserDetailResponse> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request);
    Task<UserResponse> UpdateProfileAsync(int id, UpdateProfileRequest request);
    Task DeleteAsync(int id);
}
