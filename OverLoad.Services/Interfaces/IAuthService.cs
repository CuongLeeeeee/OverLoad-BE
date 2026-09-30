using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest request);
    Task<AuthResultDto> LoginAsync(LoginRequest request);
    Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(int userId);
}
