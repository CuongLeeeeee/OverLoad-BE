using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Admin-only operations: reporting, course approval, user management.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICourseService _courseService;

    public AdminController(IPaymentService paymentService, ICourseService courseService)
    {
        _paymentService = paymentService;
        _courseService = courseService;
    }

    // ── Payment reporting ─────────────────────────────────────────────────────

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

    // ── Course approval queue ─────────────────────────────────────────────────

    /// <summary>Courses waiting for admin approval (Status = PendingReview).</summary>
    [HttpGet("courses/pending")]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<CourseResponseModel>>>> GetPendingCourses(
        [FromQuery] PagedQueryRequestModel query)
    {
        var q = new CourseQueryParams().WithPaging(query);
        var page = await _courseService.GetPendingReviewAsync(q);
        return Ok(ApiResponse<PagedResponseModel<CourseResponseModel>>.Ok(page.ToResponseModel(c => c.ToResponseModel())));
    }

    /// <summary>Approve a course: moves it from PendingReview to Published.</summary>
    [HttpPost("courses/{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourseResponseModel>>> ApproveCourse(int id)
    {
        var course = await _courseService.ApproveCourseAsync(id);
        return Ok(ApiResponse<CourseResponseModel>.Ok(course.ToResponseModel(), "Course approved and published."));
    }

    /// <summary>Reject a course: moves it back to Rejected with a reason for the instructor.</summary>
    [HttpPost("courses/{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CourseResponseModel>>> RejectCourse(int id, [FromBody] RejectCourseRequestModel request)
    {
        var course = await _courseService.RejectCourseAsync(id, request.Reason);
        return Ok(ApiResponse<CourseResponseModel>.Ok(course.ToResponseModel(), "Course rejected."));
    }

    /// <summary>Set the price of any course (Admin can price courses at any time).</summary>
    [HttpPut("courses/{id:int}/price")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseResponseModel>>> SetPrice(int id, [FromBody] SetCoursePriceRequestModel request)
    {
        var course = await _courseService.SetPriceAsync(id, request.Price);
        return Ok(ApiResponse<CourseResponseModel>.Ok(course.ToResponseModel(), "Price updated."));
    }
}
