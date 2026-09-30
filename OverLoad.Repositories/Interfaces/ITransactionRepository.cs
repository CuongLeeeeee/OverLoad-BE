using OverLoad.Domain.Entities;
using OverLoad.Domain.Enums;

namespace OverLoad.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByOrderCodeAsync(long orderCode);
    Task<bool> OrderCodeExistsAsync(long orderCode);
    Task<List<Transaction>> GetPendingGatewayTransactionsAsync(int userId);

    /// <summary>Tracks a new transaction; it is written by <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(Transaction transaction);

    Task<(IEnumerable<Transaction> Items, int TotalCount)> SearchAsync(
        int? userId, TransactionType? type, TransactionStatus? status,
        int page, int pageSize, string? sortBy, bool sortDesc);

    /// <summary>Successful transactions grouped by type and payment method.</summary>
    Task<List<TransactionTotal>> GetSuccessTotalsAsync();
}

public record TransactionTotal(TransactionType Type, PaymentMethod Method, int Count, decimal Amount);
