using OverLoad.Domain.Enums;
using OverLoad.Services.DTOs.Request;

namespace OverLoad.API.Models.Requests;

/// <summary>Starts a PayOS payment. Which optional field is required depends on <see cref="Type"/>.</summary>
public class CheckoutRequestModel
{
    public CheckoutType Type { get; set; }

    /// <summary>Required when type = Course.</summary>
    public int? CourseId { get; set; }

    /// <summary>Required when type = Pro.</summary>
    public ProPackage? Package { get; set; }

    /// <summary>Required when type = Deposit (VND, whole amount).</summary>
    public decimal? Amount { get; set; }

    /// <summary>Optional; must be on the configured frontend origin.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Optional; must be on the configured frontend origin.</summary>
    public string? CancelUrl { get; set; }
}

public class PurchaseProRequestModel
{
    public ProPackage Package { get; set; } = ProPackage.Month;
}

public class TransactionQueryRequestModel : PagedQueryRequestModel
{
    /// <summary>Admin listing only; ignored on /me/transactions.</summary>
    public int? UserId { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }

    // SortBy: createdAt, amount (default "-createdAt")
}
