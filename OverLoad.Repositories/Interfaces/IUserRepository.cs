using OverLoad.Domain.Entities;

namespace OverLoad.Repositories.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetWithEnrollmentsAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task<(IEnumerable<User> Items, int TotalCount)> SearchAsync(
        string? searchTerm, string? role, int page, int pageSize, string? sortBy, bool sortDesc);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
}
