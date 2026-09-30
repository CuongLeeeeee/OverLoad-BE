using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.Tests;

/// <summary>In-memory stand-in for PayOS: records created links and returns scripted statuses.</summary>
public class FakePaymentGateway : IPaymentGateway
{
    public List<GatewayPaymentLinkRequest> CreatedLinks { get; } = new();

    /// <summary>Status returned per order code; orders not listed are Pending.</summary>
    public Dictionary<long, GatewayPaymentStatus> Statuses { get; } = new();

    public bool Unreachable { get; set; }

    public Task<GatewayPaymentLink> CreatePaymentLinkAsync(GatewayPaymentLinkRequest request)
    {
        if (Unreachable) throw new ExternalServiceException("Gateway down");

        CreatedLinks.Add(request);
        return Task.FromResult(new GatewayPaymentLink($"plink-{request.OrderCode}", $"https://pay.test/{request.OrderCode}"));
    }

    public Task<GatewayPaymentStatus> GetPaymentStatusAsync(long orderCode)
    {
        if (Unreachable) throw new ExternalServiceException("Gateway down");

        return Task.FromResult(Statuses.TryGetValue(orderCode, out var status)
            ? status
            : new GatewayPaymentStatus(GatewayPaymentState.Pending, 0));
    }
}
