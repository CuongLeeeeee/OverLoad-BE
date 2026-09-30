namespace OverLoad.Services.DTOs.Response;

public class CheckoutDto
{
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
}

public class TransactionDto
{
    public string TransactionId { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public string? ProPackage { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class WalletDto
{
    public decimal Balance { get; set; }
    public bool IsPro { get; set; }
    public DateTime? ProExpiresAt { get; set; }
}

public class RevenueStatsDto
{
    /// <summary>Money actually received through PayOS (wallet spending is not new revenue).</summary>
    public decimal TotalRevenue { get; set; }
    public decimal CourseRevenue { get; set; }
    public decimal ProRevenue { get; set; }
    public decimal DepositRevenue { get; set; }

    /// <summary>Courses bought through PayOS.</summary>
    public int CoursesSold { get; set; }

    /// <summary>PRO packages bought, through PayOS or the wallet.</summary>
    public int ProUpgradesSold { get; set; }

    /// <summary>Sum of all wallet balances still unspent.</summary>
    public decimal OutstandingWalletBalance { get; set; }

    public List<TransactionDto> RecentTransactions { get; set; } = new();
}
