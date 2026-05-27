// OverLoad.API/Controllers/ProgressController.cs
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

    /// <summary>Get paginated list of all progress records.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ProgressQueryParams query)
        => Ok(await _progressService.GetAllAsync(query));

    /// <summary>Get progress record by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _progressService.GetByIdAsync(id);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get all progress records for a user.</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _progressService.GetByUserIdAsync(userId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get all progress records for a lesson.</summary>
    [HttpGet("lesson/{lessonId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLesson(int lessonId)
    {
        var result = await _progressService.GetByLessonIdAsync(lessonId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Get progress for a specific user + lesson combination.</summary>
    [HttpGet("user/{userId:int}/lesson/{lessonId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserAndLesson(int userId, int lessonId)
    {
        var result = await _progressService.GetByUserAndLessonAsync(userId, lessonId);
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    /// <summary>Create a new progress record.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProgressRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _progressService.CreateAsync(request);
        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Update an existing progress record.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProgressRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _progressService.UpdateAsync(id, request);
        if (!result.Success)
            return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Upsert progress — creates if not exists, updates if already exists.
    /// Recommended for client-side progress tracking.
    /// </summary>
    [HttpPost("upsert")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] CreateProgressRequest request)
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