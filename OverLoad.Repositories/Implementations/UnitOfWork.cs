using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public void ClearChanges() => _context.ChangeTracker.Clear();
}
