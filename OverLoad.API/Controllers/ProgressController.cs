using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>
/// Read-only progress reporting for staff. Learners read and save their own progress
/// through /me/lessons/{lessonId}/progress and POST /lessons/{id}/complete.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/progress")]
[Produces("application/json")]
[Authorize(Roles = Roles.Staff)]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;

    public ProgressController(IProgressService progressService)
        => _progressService = progressService;

    /// <summary>Paginated progress records, filterable by userId, lessonId and completed. SortBy: watchTime, scroll, completedAt, createdAt.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<ProgressResponseModel>>>> GetAll(
        [FromQuery] ProgressQueryRequestModel query)
    {
        var page = await _progressService.GetAllAsync(query.ToServiceDto());
        return Ok(ApiResponse<PagedResponseModel<ProgressResponseModel>>.Ok(page.ToResponseModel(p => p.ToResponseModel())));
    }
}
