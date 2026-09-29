using OverLoad.Domain.Entities;

namespace OverLoad.Services.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
