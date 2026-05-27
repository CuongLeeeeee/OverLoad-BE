using Microsoft.AspNetCore.Mvc;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Manage user enrollments in courses.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
        => _enrollmentService = enrollmentService;

    /// <summary>Get a paginated list of enrollments, filterable by user or course.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] EnrollmentQueryParams query)
        => Ok(await _enrollmentService.GetAllAsync(query));

    /// <summary>Get a specific enrollment by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _enrollmentService.GetByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get all enrollments for a specific user.</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _enrollmentService.GetByUserIdAsync(userId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get all enrollments for a specific course.</summary>
    [HttpGet("course/{courseId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var result = await _enrollmentService.GetByCourseIdAsync(courseId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Enroll a user into a course.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enroll([FromBody] CreateEnrollmentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _enrollmentService.EnrollAsync(request);
        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Update enrollment progress (percentage, completion, last accessed).</summary>
    [HttpPatch("{id:int}/progress")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateEnrollmentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _enrollmentService.UpdateProgressAsync(id, request);
        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Unenroll (remove) a user from a course.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unenroll(int id)
    {
        var result = await _enrollmentService.UnenrollAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }
}
