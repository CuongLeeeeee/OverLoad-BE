using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "")
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
            query = query.Where(filter);

        foreach (var prop in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
            query = query.Include(prop.Trim());

        var total = await query.CountAsync();

        if (orderBy != null)
            query = orderBy(query);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public virtual async Task<bool> ExistsAsync(int id)
        => await _dbSet.FindAsync(id) != null;

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        if (filter == null) return await _dbSet.CountAsync();
        return await _dbSet.CountAsync(filter);
    }
}
