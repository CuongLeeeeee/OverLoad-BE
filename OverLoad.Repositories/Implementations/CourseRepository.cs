using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class CourseRepository : BaseRepository<Course>, ICourseRepository
{
    public CourseRepository(AppDbContext context) : base(context) { }

    public async Task<Course?> GetBySlugAsync(string slug)
        => await _dbSet.Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                       .FirstOrDefaultAsync(c => c.Slug == slug);

    public async Task<Course?> GetWithLessonsAsync(int id)
        => await _dbSet.Include(c => c.Lessons.OrderBy(l => l.OrderIndex))
                       .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.Slug == slug);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<(IEnumerable<Course> Items, int TotalCount)> SearchAsync(
        string? searchTerm, string? category, string? level, bool? isPublished,
        int page, int pageSize, string? sortBy, bool sortDesc)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(c =>
                c.Title.Contains(searchTerm) ||
                (c.Description != null && c.Description.Contains(searchTerm)));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(c => c.Level.ToString() == level);

        if (isPublished.HasValue)
            query = query.Where(c => c.IsPublished == isPublished.Value);

        var total = await query.CountAsync();

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("title", false)        => query.OrderBy(c => c.Title),
            ("title", true)         => query.OrderByDescending(c => c.Title),
            ("category", false)     => query.OrderBy(c => c.Category),
            ("category", true)      => query.OrderByDescending(c => c.Category),
            ("totallessons", false) => query.OrderBy(c => c.TotalLessons),
            ("totallessons", true)  => query.OrderByDescending(c => c.TotalLessons),
            ("createdat", true)     => query.OrderByDescending(c => c.CreatedAt),
            _                       => query.OrderBy(c => c.CreatedAt)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }
}
