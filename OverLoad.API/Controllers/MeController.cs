using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Endpoints about the signed-in user. The user id always comes from the token, never the URL.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Produces("application/json")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IProgressService _progressService;
    private readonly IWalletService _walletService;
    private readonly IPaymentService _paymentService;

    public MeController(
        IUserService userService,
        IEnrollmentService enrollmentService,
        IProgressService progressService,
        IWalletService walletService,
        IPaymentService paymentService)
    {
        _userService = userService;
        _enrollmentService = enrollmentService;
        _progressService = progressService;
        _walletService = walletService;
        _paymentService = paymentService;
    }

    /// <summary>Profile of the signed-in user, including enrollment history.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDetailResponseModel>>> GetProfile()
    {
        var user = await _userService.GetByIdAsync(User.GetUserId());
        return Ok(ApiResponse<UserDetailResponseModel>.Ok(user.ToResponseModel()));
    }

    /// <summary>Update name, avatar and bio of the signed-in user.</summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserResponseModel>>> UpdateProfile([FromBody] UpdateProfileRequestModel request)
    {
        var user = await _userService.UpdateProfileAsync(User.GetUserId(), request.ToServiceDto());
        return Ok(ApiResponse<UserResponseModel>.Ok(user.ToResponseModel(), "Profile updated successfully."));
    }

    /// <summary>Courses the signed-in user is enrolled in, with progress.</summary>
    [HttpGet("courses")]
    public async Task<ActionResult<ApiResponse<List<MyCourseResponseModel>>>> GetMyCourses()
    {
        var courses = await _enrollmentService.GetMyCoursesAsync(User.GetUserId());
        return Ok(ApiResponse<List<MyCourseResponseModel>>.Ok(courses.Select(c => c.ToResponseModel()).ToList()));
    }

    /// <summary>Progress summary of the signed-in user in one course.</summary>
    [HttpGet("courses/{courseId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseProgressResponseModel>>> GetCourseProgress(int courseId)
    {
        var progress = await _enrollmentService.GetCourseProgressAsync(User.GetUserId(), courseId);
        return Ok(ApiResponse<CourseProgressResponseModel>.Ok(progress.ToResponseModel()));
    }

    /// <summary>Progress of the signed-in user on one lesson.</summary>
    [HttpGet("lessons/{lessonId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProgressResponseModel>>> GetLessonProgress(int lessonId)
    {
        var progress = await _progressService.GetMyLessonProgressAsync(User.GetUserId(), lessonId);
        return Ok(ApiResponse<ProgressResponseModel>.Ok(progress.ToResponseModel()));
    }

    /// <summary>
    /// Save the signed-in user's progress on a lesson (video position, scroll, checkpoints).
    /// Creates the record on the first call. Course percentage is recalculated on the server.
    /// </summary>
    [HttpPut("lessons/{lessonId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProgressResponseModel>>> SaveLessonProgress(
        int lessonId, [FromBody] SaveProgressRequestModel request)
    {
        var progress = await _progressService.SaveMyLessonProgressAsync(User.GetUserId(), lessonId, request.ToServiceDto());
        return Ok(ApiResponse<ProgressResponseModel>.Ok(progress.ToResponseModel(), "Progress saved."));
    }

    /// <summary>Wallet balance and PRO status. Pending deposits are refreshed from PayOS first.</summary>
    [HttpGet("wallet")]
    public async Task<ActionResult<ApiResponse<WalletResponseModel>>> GetWallet()
    {
        var wallet = await _walletService.GetWalletAsync(User.GetUserId());
        return Ok(ApiResponse<WalletResponseModel>.Ok(wallet.ToResponseModel()));
    }

    /// <summary>Buy a PRO package with the wallet balance. Top up through POST /payment/checkout (type Deposit).</summary>
    [HttpPost("wallet/purchase-pro")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<WalletResponseModel>>> PurchasePro([FromBody] PurchaseProRequestModel request)
    {
        var wallet = await _walletService.PurchaseProAsync(User.GetUserId(), request.Package);
        return Ok(ApiResponse<WalletResponseModel>.Ok(wallet.ToResponseModel(), "PRO upgrade successful."));
    }

    /// <summary>Your payment history (course purchases, PRO upgrades, deposits), newest first.</summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<TransactionResponseModel>>>> GetTransactions(
        [FromQuery] TransactionQueryRequestModel query)
    {
        var page = await _paymentService.GetMyTransactionsAsync(User.GetUserId(), query.ToServiceDto());
        return Ok(ApiResponse<PagedResponseModel<TransactionResponseModel>>.Ok(page.ToResponseModel(t => t.ToResponseModel())));
    }
}
