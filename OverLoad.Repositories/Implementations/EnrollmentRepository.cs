using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class EnrollmentRepository : BaseRepository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(AppDbContext context) : base(context) { }

    public async Task<Enrollment?> GetByUserAndCourseAsync(int userId, int courseId)
        => await _dbSet.Include(e => e.User).Include(e => e.Course)
                       .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

    public async Task<IEnumerable<Enrollment>> GetByUserIdAsync(int userId)
        => await _dbSet.Include(e => e.Course)
                       .Where(e => e.UserId == userId)
                       .OrderByDescending(e => e.EnrolledAt)
                       .ToListAsync();

    public async Task<IEnumerable<Enrollment>> GetByCourseIdAsync(int courseId)
        => await _dbSet.Include(e => e.User)
                       .Where(e => e.CourseId == courseId)
                       .OrderByDescending(e => e.EnrolledAt)
                       .ToListAsync();

    public async Task<bool> IsEnrolledAsync(int userId, int courseId)
        => await _dbSet.AnyAsync(e => e.UserId == userId && e.CourseId == courseId);

    public async Task<(IEnumerable<Enrollment> Items, int TotalCount)> SearchAsync(
        int? userId, int? courseId, int page, int pageSize, string? sortBy, bool sortDesc)
    {
        var query = _dbSet.Include(e => e.User).Include(e => e.Course).AsQueryable();

        if (userId.HasValue)   query = query.Where(e => e.UserId == userId.Value);
        if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId.Value);

        var total = await query.CountAsync();

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("enrolledat", true)  => query.OrderByDescending(e => e.EnrolledAt),
            ("progress", false)   => query.OrderBy(e => e.ProgressPercentage),
            ("progress", true)    => query.OrderByDescending(e => e.ProgressPercentage),
            _                     => query.OrderByDescending(e => e.EnrolledAt)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}
