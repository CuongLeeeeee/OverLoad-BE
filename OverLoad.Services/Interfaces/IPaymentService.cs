using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IPaymentService
{
    /// <summary>Creates a pending transaction and a PayOS checkout link for it.</summary>
    Task<CheckoutResponse> CheckoutAsync(int userId, CheckoutRequest request);

    /// <summary>
    /// Applies a payment the gateway reported as paid (the caller has already verified the webhook
    /// signature). Idempotent: repeated or concurrent calls fulfil the order once.
    /// Returns false when the order code is unknown.
    /// </summary>
    Task<bool> ConfirmPaidAsync(long orderCode, long amountPaid);

    /// <summary>One of the caller's orders, refreshed from the gateway while still pending.</summary>
    Task<TransactionResponse> GetMyOrderAsync(int userId, long orderCode);

    /// <summary>
    /// Asks the gateway about the user's pending orders and applies the result. A fallback for
    /// webhooks that never arrive (local development, gateway outages).
    /// </summary>
    Task SyncPendingAsync(int userId);

    Task<PagedResult<TransactionResponse>> GetMyTransactionsAsync(int userId, TransactionQueryParams query);
    Task<PagedResult<TransactionResponse>> GetAllTransactionsAsync(TransactionQueryParams query);
    Task<RevenueStatsResponse> GetRevenueStatsAsync();
}
