using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
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

    /// <summary>All transactions, filterable by userId, type and status. SortBy: createdAt, amount.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<TransactionResponseModel>>>> GetTransactions(
        [FromQuery] TransactionQueryRequestModel query)
    {
        var page = await _paymentService.GetAllTransactionsAsync(query.ToServiceDto());
        return Ok(ApiResponse<PagedResponseModel<TransactionResponseModel>>.Ok(page.ToResponseModel(t => t.ToResponseModel())));
    }

    /// <summary>Revenue received through PayOS, sales counts and outstanding wallet balances.</summary>
    [HttpGet("revenue-stats")]
    public async Task<ActionResult<ApiResponse<RevenueStatsResponseModel>>> GetRevenueStats()
    {
        var stats = await _paymentService.GetRevenueStatsAsync();
        return Ok(ApiResponse<RevenueStatsResponseModel>.Ok(stats.ToResponseModel()));
    }
}
