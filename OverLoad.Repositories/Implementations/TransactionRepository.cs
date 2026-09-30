using Microsoft.EntityFrameworkCore;
using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;
using OverLoad.Repositories.Data;
using OverLoad.Repositories.Interfaces;

namespace OverLoad.Repositories.Implementations;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context) => _context = context;

    public async Task<Transaction?> GetByOrderCodeAsync(long orderCode)
        => await _context.Transactions.FirstOrDefaultAsync(t => t.OrderCode == orderCode);

    public async Task<bool> OrderCodeExistsAsync(long orderCode)
        => await _context.Transactions.AnyAsync(t => t.OrderCode == orderCode);

    public async Task<List<Transaction>> GetPendingGatewayTransactionsAsync(int userId)
        => await _context.Transactions
            .Where(t => t.UserId == userId && t.Status == TransactionStatus.Pending && t.Method == PaymentMethod.PayOS)
            .ToListAsync();

    public void Add(Transaction transaction) => _context.Transactions.Add(transaction);

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> SearchAsync(
        int? userId, TransactionType? type, TransactionStatus? status,
        int page, int pageSize, string? sortBy, bool sortDesc)
    {
        var query = _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Course)
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var total = await query.CountAsync();

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("amount", false)    => query.OrderBy(t => t.Amount),
            ("amount", true)     => query.OrderByDescending(t => t.Amount),
            ("createdat", false) => query.OrderBy(t => t.CreatedAt),
            _                    => query.OrderByDescending(t => t.CreatedAt)
        };

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<List<TransactionTotal>> GetSuccessTotalsAsync()
        => await _context.Transactions
            .Where(t => t.Status == TransactionStatus.Success)
            .GroupBy(t => new { t.Type, t.Method })
            .Select(g => new TransactionTotal(g.Key.Type, g.Key.Method, g.Count(), g.Sum(t => t.Amount)))
            .ToListAsync();
}
