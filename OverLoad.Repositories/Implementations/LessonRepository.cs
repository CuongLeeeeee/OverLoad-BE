using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class LessonRepository : BaseRepository<Lesson>, ILessonRepository
{
    public LessonRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Lesson>> GetByCourseIdAsync(int courseId)
        => await _dbSet.Where(l => l.CourseId == courseId)
                       .OrderBy(l => l.OrderIndex)
                       .ToListAsync();

    public async Task<Lesson?> GetWithCourseAsync(int id)
        => await _dbSet.Include(l => l.Course)
                       .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<int> GetNextOrderIndexAsync(int courseId)
    {
        var max = await _dbSet.Where(l => l.CourseId == courseId)
                              .MaxAsync(l => (int?)l.OrderIndex);
        return (max ?? 0) + 1;
    }

    public async Task<(IEnumerable<Lesson> Items, int TotalCount)> SearchAsync(
        int? courseId, string? searchTerm, bool? isFree, int page, int pageSize, string? sortBy, bool sortDesc)
    {
        var query = _dbSet.AsQueryable();

        if (courseId.HasValue)
            query = query.Where(l => l.CourseId == courseId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(l => l.Title.Contains(searchTerm));

        if (isFree.HasValue)
            query = query.Where(l => l.IsFree == isFree.Value);

        var total = await query.CountAsync();

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("title", false)      => query.OrderBy(l => l.Title),
            ("title", true)       => query.OrderByDescending(l => l.Title),
            ("orderindex", false) => query.OrderBy(l => l.OrderIndex),
            ("orderindex", true)  => query.OrderByDescending(l => l.OrderIndex),
            _                     => query.OrderBy(l => l.OrderIndex)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}
