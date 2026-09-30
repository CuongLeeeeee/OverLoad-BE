using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Payment reporting (Admin only).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public AdminController(IPaymentService paymentService) => _paymentService = paymentService;

    /// <summary>All transactions, filterable by userId, type and status.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionResponse>>>> GetTransactions(
        [FromQuery] TransactionQueryParams query)
        => Ok(ApiResponse<PagedResult<TransactionResponse>>.Ok(await _paymentService.GetAllTransactionsAsync(query)));

    /// <summary>Revenue received through PayOS, sales counts and outstanding wallet balances.</summary>
    [HttpGet("revenue-stats")]
    public async Task<ActionResult<ApiResponse<RevenueStatsResponse>>> GetRevenueStats()
        => Ok(ApiResponse<RevenueStatsResponse>.Ok(await _paymentService.GetRevenueStatsAsync()));
}
