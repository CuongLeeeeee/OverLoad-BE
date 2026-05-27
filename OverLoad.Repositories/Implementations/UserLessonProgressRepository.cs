// OverLoad.Repositories/Implementations/UserLessonProgressRepository.cs
using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class UserLessonProgressRepository : BaseRepository<UserLessonProgress>, IUserLessonProgressRepository
{
    public UserLessonProgressRepository(AppDbContext context) : base(context) { }

    public async Task<UserLessonProgress?> GetByIdDetailAsync(int id)
        => await _dbSet
            .Include(p => p.User)
            .Include(p => p.Lesson).ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<UserLessonProgress?> GetByUserAndLessonAsync(int userId, int lessonId)
        => await _dbSet
            .Include(p => p.User)
            .Include(p => p.Lesson).ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);

    public async Task<IEnumerable<UserLessonProgress>> GetByUserIdAsync(int userId)
        => await _dbSet
            .Include(p => p.User)
            .Include(p => p.Lesson).ThenInclude(l => l.Course)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

    public async Task<IEnumerable<UserLessonProgress>> GetByLessonIdAsync(int lessonId)
        => await _dbSet
            .Include(p => p.User)
            .Include(p => p.Lesson).ThenInclude(l => l.Course)
            .Where(p => p.LessonId == lessonId)
            .ToListAsync();

    public async Task<bool> ExistsByUserAndLessonAsync(int userId, int lessonId)
        => await _dbSet.AnyAsync(p => p.UserId == userId && p.LessonId == lessonId);

    public async Task<(IEnumerable<UserLessonProgress> Items, int TotalCount)> SearchAsync(
        int? userId, int? lessonId, bool? completed,
        int page, int pageSize, string? sortBy, bool sortDesc)
    {
        var query = _dbSet
            .Include(p => p.User)
            .Include(p => p.Lesson).ThenInclude(l => l.Course)
            .AsQueryable();

        if (userId.HasValue) query = query.Where(p => p.UserId == userId.Value);
        if (lessonId.HasValue) query = query.Where(p => p.LessonId == lessonId.Value);
        if (completed.HasValue) query = query.Where(p => p.Completed == completed.Value);

        var total = await query.CountAsync();

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("watchtime", false) => query.OrderBy(p => p.WatchTimeSeconds),
            ("watchtime", true) => query.OrderByDescending(p => p.WatchTimeSeconds),
            ("scroll", false) => query.OrderBy(p => p.LastScrollPercentage),
            ("scroll", true) => query.OrderByDescending(p => p.LastScrollPercentage),
            ("completedat", false) => query.OrderBy(p => p.CompletedAt),
            ("completedat", true) => query.OrderByDescending(p => p.CompletedAt),
            ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.CreatedAt)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}