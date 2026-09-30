using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetAllAsync(UserQueryParams query);
    Task<UserDetailDto> GetByIdAsync(int id);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);
    Task<UserDto> UpdateProfileAsync(int id, UpdateProfileRequest request);
    Task DeleteAsync(int id);

    /// <summary>Locks or unlocks an account. Locking also revokes the refresh token.</summary>
    Task<UserDto> SetLockedAsync(int id, bool locked);
}
