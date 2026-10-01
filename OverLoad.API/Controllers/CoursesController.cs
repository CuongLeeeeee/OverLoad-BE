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

/// <summary>
/// Course catalogue. Reading is public (draft courses are visible to staff only);
/// writing is limited to Admin and Instructor.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/courses")]
[Produces("application/json")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILessonService _lessonService;
    private readonly IEnrollmentService _enrollmentService;

    public CoursesController(
        ICourseService courseService,
        ILessonService lessonService,
        IEnrollmentService enrollmentService)
    {
        _courseService = courseService;
        _lessonService = lessonService;
        _enrollmentService = enrollmentService;
    }

    /// <summary>Paginated list of courses. SortBy: title, category, totalLessons, createdAt.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<CourseResponseModel>>>> GetAll(
        [FromQuery] CourseQueryRequestModel query)
    {
        var page = await _courseService.GetAllAsync(query.ToServiceDto(), User.IsStaff());
        return Ok(ApiResponse<PagedResponseModel<CourseResponseModel>>.Ok(page.ToResponseModel(c => c.ToResponseModel())));
    }

    /// <summary>Published courses of one category.</summary>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<CourseResponseModel>>>> GetByCategory(
        string category, [FromQuery] CourseQueryRequestModel query)
    {
        var filter = query.ToServiceDto();
        filter.Category = category;
        var page = await _courseService.GetAllAsync(filter, includeUnpublished: false);
        return Ok(ApiResponse<PagedResponseModel<CourseResponseModel>>.Ok(page.ToResponseModel(c => c.ToResponseModel())));
    }

    /// <summary>A single course by ID, including its lesson outline.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseDetailResponseModel>>> GetById(int id)
    {
        var course = await _courseService.GetByIdAsync(id, User.IsStaff());
        return Ok(ApiResponse<CourseDetailResponseModel>.Ok(course.ToResponseModel()));
    }

    /// <summary>A course by its URL slug.</summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseDetailResponseModel>>> GetBySlug(string slug)
    {
        var course = await _courseService.GetBySlugAsync(slug, User.IsStaff());
        return Ok(ApiResponse<CourseDetailResponseModel>.Ok(course.ToResponseModel()));
    }

    /// <summary>Lessons of a course with lock state, plus the caller's progress when signed in.</summary>
    [HttpGet("{id:int}/lessons")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<LessonProgressResponseModel>>>> GetLessons(int id)
    {
        var lessons = await _lessonService.GetByCourseIdWithProgressAsync(id, User.FindUserId(), User.IsStaff());
        return Ok(ApiResponse<List<LessonProgressResponseModel>>.Ok(lessons.Select(l => l.ToResponseModel()).ToList()));
    }

    /// <summary>Enroll the signed-in user: free courses, or any course with active PRO. Paid courses go through /payment.</summary>
    [HttpPost("{id:int}/enroll")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EnrollmentResponseModel>>> Enroll(int id)
    {
        var enrollment = await _enrollmentService.SelfEnrollAsync(User.GetUserId(), id);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<EnrollmentResponseModel>.Ok(enrollment.ToResponseModel(), "Enrolled successfully."));
    }

    /// <summary>Create a course. Always starts as Draft; the slug is generated from the title.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CourseResponseModel>>> Create([FromBody] CreateCourseRequestModel request)
    {
        var created = await _courseService.CreateAsync(request.ToServiceDto(), User.GetUserId());
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<CourseResponseModel>.Ok(created.ToResponseModel(), "Course created successfully."));
    }

    /// <summary>Update a course. Instructors can only edit their own courses in Draft or Rejected state.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseResponseModel>>> Update(int id, [FromBody] UpdateCourseRequestModel request)
    {
        var course = await _courseService.UpdateAsync(id, request.ToServiceDto(), User.GetUserId(), User.IsInRole(Roles.Admin));
        return Ok(ApiResponse<CourseResponseModel>.Ok(course.ToResponseModel(), "Course updated successfully."));
    }

    /// <summary>Delete a course and all its lessons. Instructors can only delete their own courses.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        await _courseService.DeleteAsync(id, User.GetUserId(), User.IsInRole(Roles.Admin));
        return Ok(ApiResponse.Ok("Course deleted successfully."));
    }
}
