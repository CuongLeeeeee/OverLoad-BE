namespace OverLoad.Domain.Enums;

public enum TransactionStatus
{
    Pending = 0,
    Success = 1,

    /// <summary>Cancelled by the buyer, expired or failed at the gateway.</summary>
    Cancelled = 2
}
