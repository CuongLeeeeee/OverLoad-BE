using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OverLoad.Services.Interfaces;
using Xunit;

namespace OverLoad.Tests.Integration;

/// <summary>Checkout → gateway reports paid → wallet → PRO, through the HTTP API with a fake gateway.</summary>
public class PaymentFlowTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;

    public PaymentFlowTests(ApiFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        if (_factory.Data == null)
            await _factory.InitializeDataAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Deposit_Then_Buy_Pro_With_Wallet_Unlocks_Paid_Lessons()
    {
        var client = _factory.CreateClientFor(_factory.Data.Student);
        var paidLessonUrl = $"/api/v1/lessons/{_factory.Data.PaidCourseLessons[3].Id}";
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(paidLessonUrl)).StatusCode);

        // 1. Checkout a deposit
        var checkout = await client.PostAsJsonAsync("/api/v1/payment/checkout", new { type = "Deposit", amount = 100000 });
        Assert.Equal(HttpStatusCode.OK, checkout.StatusCode);
        var order = (await checkout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var orderCode = order.GetProperty("orderCode").GetInt64();
        Assert.StartsWith("https://pay.test/", order.GetProperty("checkoutUrl").GetString());

        // 2. The buyer pays; the webhook is late, so the return page asks for the order status
        _factory.Gateway.Statuses[orderCode] = new GatewayPaymentStatus(GatewayPaymentState.Paid, 100000);
        var status = await client.GetFromJsonAsync<JsonElement>($"/api/v1/payment/orders/{orderCode}");
        Assert.Equal("Success", status.GetProperty("data").GetProperty("status").GetString());

        // 3. Buy PRO with the wallet
        var purchase = await client.PostAsJsonAsync("/api/v1/me/wallet/purchase-pro", new { package = "Month" });
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);
        var wallet = (await purchase.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal(31000m, wallet.GetProperty("balance").GetDecimal());
        Assert.True(wallet.GetProperty("isPro").GetBoolean());

        // 4. PRO opens paid lessons and shows up in the history
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(paidLessonUrl)).StatusCode);
        var history = await client.GetFromJsonAsync<JsonElement>("/api/v1/me/transactions");
        Assert.Equal(2, history.GetProperty("data").GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Checkout_Should_Validate_Type_Specific_Fields()
    {
        var client = _factory.CreateClientFor(_factory.Data.Instructor);

        var response = await client.PostAsJsonAsync("/api/v1/payment/checkout", new { type = "Course" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(body.GetProperty("errors").EnumerateArray(), e => e.GetString()!.StartsWith("CourseId"));
    }

    [Fact]
    public async Task Admin_Revenue_Stats_Should_Return_Envelope()
    {
        var client = _factory.CreateClientFor(_factory.Data.Admin);

        var body = await client.GetFromJsonAsync<JsonElement>("/api/v1/admin/revenue-stats");

        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.True(body.GetProperty("data").TryGetProperty("totalRevenue", out _));
    }
}
