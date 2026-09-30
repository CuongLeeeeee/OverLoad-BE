using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.Interfaces;
using PayOS;
using PayOS.Models.Webhooks;

namespace OverLoad.API.Controllers;

/// <summary>
/// PayOS checkout and webhook. Wallet endpoints live under /me/wallet, transaction history under
/// /me/transactions, and reporting under /admin.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payment")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private const string PaidCode = "00";

    private readonly IPaymentService _paymentService;
    private readonly PayOSClient _payOS;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, PayOSClient payOS, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _payOS = payOS;
        _logger = logger;
    }

    /// <summary>
    /// Start a PayOS payment for a course, a PRO package or a wallet deposit.
    /// Redirect the user to the returned checkoutUrl.
    /// </summary>
    /// <remarks>
    ///     { "type": "Course",  "courseId": 12 }
    ///     { "type": "Pro",     "package": "Year" }
    ///     { "type": "Deposit", "amount": 100000 }
    /// </remarks>
    [HttpPost("checkout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<CheckoutResponseModel>>> Checkout([FromBody] CheckoutRequestModel request)
    {
        var checkout = await _paymentService.CheckoutAsync(User.GetUserId(), request.ToServiceDto());
        return Ok(ApiResponse<CheckoutResponseModel>.Ok(checkout.ToResponseModel(), "Payment link created."));
    }

    /// <summary>
    /// Status of one of your orders — call it from the PayOS return page. A pending order is
    /// refreshed from PayOS first, so the result is correct even if the webhook is late.
    /// </summary>
    [HttpGet("orders/{orderCode:long}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TransactionResponseModel>>> GetOrder(long orderCode)
    {
        var order = await _paymentService.GetMyOrderAsync(User.GetUserId(), orderCode);
        return Ok(ApiResponse<TransactionResponseModel>.Ok(order.ToResponseModel()));
    }

    /// <summary>PayOS webhook. The signature is verified before anything is applied.</summary>
    /// <remarks>
    /// Returns 200 for every correctly signed call — including unknown orders such as the test
    /// call PayOS sends when the webhook URL is registered — so PayOS does not keep retrying.
    /// The unversioned /api/payment/webhook route is kept because it is the URL registered in the
    /// PayOS dashboard; remove it once the dashboard points at /api/v1/payment/webhook.
    /// </remarks>
    [HttpPost("webhook")]
    [HttpPost("/api/payment/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object?>>> Webhook([FromBody] Webhook webhook)
    {
        WebhookData data;
        try
        {
            data = await _payOS.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rejected PayOS webhook with an invalid signature");
            return BadRequest(ApiResponse.Fail("Invalid webhook signature."));
        }

        if (data.Code != PaidCode)
            return Ok(ApiResponse.Ok($"Ignored: payment code {data.Code}."));

        var known = await _paymentService.ConfirmPaidAsync(data.OrderCode, data.Amount);
        return Ok(ApiResponse.Ok(known ? "Payment confirmed." : "Unknown order ignored."));
    }
}
