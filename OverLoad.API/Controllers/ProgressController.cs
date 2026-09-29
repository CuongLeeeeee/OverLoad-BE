using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
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

    /// <summary>Paginated progress records, filterable by userId, lessonId and completed.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProgressResponse>>>> GetAll([FromQuery] ProgressQueryParams query)
        => Ok(ApiResponse<PagedResult<ProgressResponse>>.Ok(await _progressService.GetAllAsync(query)));
}
