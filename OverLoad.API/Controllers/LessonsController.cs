using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Lessons within courses. Lesson content of paid courses requires enrollment.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lessons")]
[Produces("application/json")]
[Authorize]
public class LessonsController : ControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly IProgressService _progressService;

    public LessonsController(ILessonService lessonService, IProgressService progressService)
    {
        _lessonService = lessonService;
        _progressService = progressService;
    }

    /// <summary>Paginated list of lessons with full content (staff only; learners use /courses/{id}/lessons).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<ApiResponse<PagedResult<LessonResponse>>>> GetAll([FromQuery] LessonQueryParams query)
        => Ok(ApiResponse<PagedResult<LessonResponse>>.Ok(await _lessonService.GetAllAsync(query)));

    /// <summary>A lesson with its full content. Free lessons are public; paid ones need an enrollment.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonResponse>>> GetById(int id)
        => Ok(ApiResponse<LessonResponse>.Ok(await _lessonService.GetByIdAsync(id, User.FindUserId(), User.IsStaff())));

    /// <summary>Create a lesson. OrderIndex is assigned as the next free slot.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonResponse>>> Create([FromBody] CreateLessonRequest request)
    {
        var created = await _lessonService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<LessonResponse>.Ok(created, "Lesson created successfully."));
    }

    /// <summary>Update a lesson, including reordering via OrderIndex.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonResponse>>> Update(int id, [FromBody] UpdateLessonRequest request)
        => Ok(ApiResponse<LessonResponse>.Ok(await _lessonService.UpdateAsync(id, request), "Lesson updated successfully."));

    /// <summary>Delete a lesson. Course totals are recalculated.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        await _lessonService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Lesson deleted successfully."));
    }

    /// <summary>Mark a lesson as completed for the signed-in user. Course progress is recalculated on the server.</summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProgressResponse>>> Complete(int id)
        => Ok(ApiResponse<ProgressResponse>.Ok(
            await _progressService.CompleteLessonAsync(User.GetUserId(), id), "Lesson marked as completed."));
}
