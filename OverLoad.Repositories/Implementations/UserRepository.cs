using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<User?> GetWithEnrollmentsAsync(int id)
        => await _dbSet.Include(u => u.Enrollments).ThenInclude(e => e.Course)
                       .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> EmailExistsAsync(string email)
        => await _dbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<(IEnumerable<User> Items, int TotalCount)> SearchAsync(
        string? searchTerm, string? role, int page, int pageSize, string? sortBy, bool sortDesc)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(u =>
                u.FullName.Contains(searchTerm) ||
                u.Email.Contains(searchTerm));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role.ToString() == role);

        var total = await query.CountAsync();

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("email", false)    => query.OrderBy(u => u.Email),
            ("email", true)     => query.OrderByDescending(u => u.Email),
            ("fullname", false) => query.OrderBy(u => u.FullName),
            ("fullname", true)  => query.OrderByDescending(u => u.FullName),
            ("createdat", true) => query.OrderByDescending(u => u.CreatedAt),
            _                   => query.OrderBy(u => u.CreatedAt)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}
