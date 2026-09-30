using OverLoad.Domain.Enums;
using OverLoad.Services.Common;

namespace OverLoad.Services.DTOs.Request;

public enum CheckoutType
{
    Course,
    Pro,
    Deposit
}

/// <summary>Starts a PayOS payment. Which optional field is required depends on <see cref="Type"/>.</summary>
public class CheckoutRequest
{
    public CheckoutType Type { get; set; }

    /// <summary>Required when Type = Course.</summary>
    public int? CourseId { get; set; }

    /// <summary>Required when Type = Pro.</summary>
    public ProPackage? Package { get; set; }

    /// <summary>Required when Type = Deposit (VND).</summary>
    public decimal? Amount { get; set; }

    /// <summary>Optional; must be on the configured frontend origin.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Optional; must be on the configured frontend origin.</summary>
    public string? CancelUrl { get; set; }
}

public class PurchaseProRequest
{
    public ProPackage Package { get; set; } = ProPackage.Month;
}

public class TransactionQueryParams : QueryParameters
{
    /// <summary>Only used by the admin listing; ignored on /me/transactions.</summary>
    public int? UserId { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }

    /// <summary>createdAt, amount. Prefix with '-' for descending.</summary>
    public override string? SortBy { get; set; } = "-createdAt";
}
