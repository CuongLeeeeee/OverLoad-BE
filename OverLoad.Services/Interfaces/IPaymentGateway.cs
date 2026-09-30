namespace OverLoad.Services.Interfaces;

/// <summary>
/// The payment provider (PayOS) as seen by the services. Keeping the SDK behind this interface
/// lets the payment rules be unit tested with a fake gateway.
/// </summary>
public interface IPaymentGateway
{
    Task<GatewayPaymentLink> CreatePaymentLinkAsync(GatewayPaymentLinkRequest request);

    /// <summary>Current status of an order at the provider.</summary>
    Task<GatewayPaymentStatus> GetPaymentStatusAsync(long orderCode);
}

public record GatewayPaymentLinkRequest(
    long OrderCode, long Amount, string Description, string ItemName, string ReturnUrl, string CancelUrl);

public record GatewayPaymentLink(string PaymentLinkId, string CheckoutUrl);

public enum GatewayPaymentState
{
    Pending,
    Paid,

    /// <summary>Cancelled, expired or failed — the order will never be paid.</summary>
    Closed
}

public record GatewayPaymentStatus(GatewayPaymentState State, long AmountPaid);
