namespace OverLoad.Domain.Enums;

/// <summary>Where the money came from.</summary>
public enum PaymentMethod
{
    /// <summary>Paid through the PayOS gateway (real money received).</summary>
    PayOS = 0,

    /// <summary>Paid from the user's wallet balance.</summary>
    Wallet = 1
}
