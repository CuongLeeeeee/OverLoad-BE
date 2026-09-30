using OverLoad.Domain.Enums;

namespace OverLoad.Domain.Entities;

public class Transaction
{
    /// <summary>PayOS payment link id, or "WALLET-{orderCode}" for wallet payments.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Unique order number shared with PayOS.</summary>
    public long OrderCode { get; set; }

    public int UserId { get; set; }
    public TransactionType Type { get; set; }
    public PaymentMethod Method { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>Set for <see cref="TransactionType.CoursePurchase"/> only.</summary>
    public int? CourseId { get; set; }

    /// <summary>Set for <see cref="TransactionType.ProUpgrade"/> only.</summary>
    public ProPackage? ProPackage { get; set; }

    /// <summary>Always positive; the direction follows from Type and Method.</summary>
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Course? Course { get; set; }
}
