namespace OverLoad.Domain.Enums;

/// <summary>What the money was for.</summary>
public enum TransactionType
{
    CoursePurchase = 0,
    ProUpgrade = 1,

    /// <summary>Top-up of the user's wallet balance.</summary>
    Deposit = 2
}
