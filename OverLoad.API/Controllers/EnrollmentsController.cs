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
/// Enrollment administration. Learners enroll through POST /courses/{id}/enroll or the payment
/// flow, and read their own enrollments through /me/courses.
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

    /// <summary>Paginated enrollments, filterable by userId and courseId. SortBy: enrolledAt, progress.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<EnrollmentResponseModel>>>> GetAll(
        [FromQuery] EnrollmentQueryRequestModel query)
    {
        var page = await _enrollmentService.GetAllAsync(query.ToServiceDto());
        return Ok(ApiResponse<PagedResponseModel<EnrollmentResponseModel>>.Ok(page.ToResponseModel(e => e.ToResponseModel())));
    }

    /// <summary>A single enrollment.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EnrollmentResponseModel>>> GetById(int id)
    {
        var enrollment = await _enrollmentService.GetByIdAsync(id);
        return Ok(ApiResponse<EnrollmentResponseModel>.Ok(enrollment.ToResponseModel()));
    }

    /// <summary>Grant a user access to any course, bypassing payment (Admin only).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EnrollmentResponseModel>>> Grant([FromBody] GrantEnrollmentRequestModel request)
    {
        var created = await _enrollmentService.GrantAsync(request.ToServiceDto());
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<EnrollmentResponseModel>.Ok(created.ToResponseModel(), "Enrollment granted."));
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
