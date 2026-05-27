using Microsoft.AspNetCore.Mvc;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Track and manage per-lesson user progress.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;

    public ProgressController(IProgressService progressService)
        => _progressService = progressService;

    /// <summary>Get a progress record by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _progressService.GetByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get progress for a specific user/lesson combination.</summary>
    [HttpGet("user/{userId:int}/lesson/{lessonId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserAndLesson(int userId, int lessonId)
    {
        var result = await _progressService.GetByUserAndLessonAsync(userId, lessonId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get all lesson progress records for a user.</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _progressService.GetByUserIdAsync(userId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>
    /// Create or update (upsert) progress for a user/lesson.
    /// If a record already exists it is updated; otherwise a new one is created.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert([FromBody] UpsertProgressRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _progressService.UpsertAsync(request);
        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>Delete a progress record by ID.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _progressService.DeleteAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }
}
