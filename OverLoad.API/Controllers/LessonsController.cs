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

/// <summary>Lessons within courses. Content of paid lessons requires an enrollment or PRO.</summary>
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

    /// <summary>Paginated lessons with full content (staff only; learners use /courses/{id}/lessons). SortBy: title, orderIndex.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Staff)]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<LessonResponseModel>>>> GetAll(
        [FromQuery] LessonQueryRequestModel query)
    {
        var page = await _lessonService.GetAllAsync(query.ToServiceDto());
        return Ok(ApiResponse<PagedResponseModel<LessonResponseModel>>.Ok(page.ToResponseModel(l => l.ToResponseModel())));
    }

    /// <summary>A lesson with its full content. Free lessons are public; paid ones need an enrollment or PRO.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonResponseModel>>> GetById(int id)
    {
        var lesson = await _lessonService.GetByIdAsync(id, User.FindUserId(), User.IsStaff());
        return Ok(ApiResponse<LessonResponseModel>.Ok(lesson.ToResponseModel()));
    }

    /// <summary>Create a lesson. OrderIndex is assigned as the next free slot.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonResponseModel>>> Create([FromBody] CreateLessonRequestModel request)
    {
        var created = await _lessonService.CreateAsync(request.ToServiceDto());
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<LessonResponseModel>.Ok(created.ToResponseModel(), "Lesson created successfully."));
    }

    /// <summary>Update a lesson, including reordering via OrderIndex.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonResponseModel>>> Update(int id, [FromBody] UpdateLessonRequestModel request)
    {
        var lesson = await _lessonService.UpdateAsync(id, request.ToServiceDto());
        return Ok(ApiResponse<LessonResponseModel>.Ok(lesson.ToResponseModel(), "Lesson updated successfully."));
    }

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
    public async Task<ActionResult<ApiResponse<ProgressResponseModel>>> Complete(int id)
    {
        var progress = await _progressService.CompleteLessonAsync(User.GetUserId(), id);
        return Ok(ApiResponse<ProgressResponseModel>.Ok(progress.ToResponseModel(), "Lesson marked as completed."));
    }
}
