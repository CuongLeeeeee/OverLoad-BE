using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace OverLoad.API.Infrastructure;

/// <summary>PayOS SDK adapter for <see cref="IPaymentGateway"/>.</summary>
public class PayOSGateway : IPaymentGateway
{
    private readonly PayOSClient _client;
    private readonly ILogger<PayOSGateway> _logger;

    public PayOSGateway(PayOSClient client, ILogger<PayOSGateway> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<GatewayPaymentLink> CreatePaymentLinkAsync(GatewayPaymentLinkRequest request)
    {
        try
        {
            var result = await _client.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest
            {
                OrderCode = request.OrderCode,
                Amount = request.Amount,
                Description = request.Description,
                ReturnUrl = request.ReturnUrl,
                CancelUrl = request.CancelUrl,
                Items = new List<PaymentLinkItem>
                {
                    new() { Name = request.ItemName, Quantity = 1, Price = request.Amount }
                }
            });

            return new GatewayPaymentLink(result.PaymentLinkId, result.CheckoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS create payment link failed for order {OrderCode}", request.OrderCode);
            throw new ExternalServiceException("Could not connect to the payment gateway. Please try again.");
        }
    }

    public async Task<GatewayPaymentStatus> GetPaymentStatusAsync(long orderCode)
    {
        PaymentLink link;
        try
        {
            link = await _client.PaymentRequests.GetAsync(orderCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS status lookup failed for order {OrderCode}", orderCode);
            throw new ExternalServiceException("Could not reach the payment gateway.");
        }

        var state = link.Status switch
        {
            PaymentLinkStatus.Paid => GatewayPaymentState.Paid,
            PaymentLinkStatus.Cancelled or PaymentLinkStatus.Expired or PaymentLinkStatus.Failed => GatewayPaymentState.Closed,
            _ => GatewayPaymentState.Pending   // Pending, Processing, Underpaid
        };

        return new GatewayPaymentStatus(state, link.AmountPaid);
    }
}
