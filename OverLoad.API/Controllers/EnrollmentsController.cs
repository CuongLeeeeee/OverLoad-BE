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
/// Enrollment administration. Learners enroll through POST /courses/{id}/enroll (free courses)
/// or the payment flow, and read their own enrollments through /me/courses.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/enrollments")]
[Produces("application/json")]
[Authorize(Roles = Roles.Staff)]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
        => _enrollmentService = enrollmentService;

    /// <summary>Paginated list of enrollments, filterable by userId and courseId.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EnrollmentResponse>>>> GetAll([FromQuery] EnrollmentQueryParams query)
        => Ok(ApiResponse<PagedResult<EnrollmentResponse>>.Ok(await _enrollmentService.GetAllAsync(query)));

    /// <summary>A single enrollment.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EnrollmentResponse>>> GetById(int id)
        => Ok(ApiResponse<EnrollmentResponse>.Ok(await _enrollmentService.GetByIdAsync(id)));

    /// <summary>Grant a user access to any course, bypassing payment (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EnrollmentResponse>>> Grant([FromBody] CreateEnrollmentRequest request)
    {
        var created = await _enrollmentService.GrantAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<EnrollmentResponse>.Ok(created, "Enrollment granted."));
    }

    /// <summary>Remove an enrollment (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        await _enrollmentService.UnenrollAsync(id);
        return Ok(ApiResponse.Ok("Enrollment removed."));
    }
}
