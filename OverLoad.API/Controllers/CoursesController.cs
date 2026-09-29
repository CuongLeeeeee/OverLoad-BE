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

    /// <summary>Paginated list of courses with filtering and sorting.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<CourseResponse>>>> GetAll([FromQuery] CourseQueryParams query)
        => Ok(ApiResponse<PagedResult<CourseResponse>>.Ok(await _courseService.GetAllAsync(query, User.IsStaff())));

    /// <summary>Published courses of one category.</summary>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<CourseResponse>>>> GetByCategory(
        string category, [FromQuery] CourseQueryParams query)
    {
        query.Category = category;
        return Ok(ApiResponse<PagedResult<CourseResponse>>.Ok(
            await _courseService.GetAllAsync(query, includeUnpublished: false)));
    }

    /// <summary>A single course by ID, including its lesson outline.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseDetailResponse>>> GetById(int id)
        => Ok(ApiResponse<CourseDetailResponse>.Ok(await _courseService.GetByIdAsync(id, User.IsStaff())));

    /// <summary>A course by its URL slug.</summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseDetailResponse>>> GetBySlug(string slug)
        => Ok(ApiResponse<CourseDetailResponse>.Ok(await _courseService.GetBySlugAsync(slug, User.IsStaff())));

    /// <summary>Lessons of a course with lock state, plus the caller's progress when signed in.</summary>
    [HttpGet("{id:int}/lessons")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<LessonProgressResponse>>>> GetLessons(int id)
        => Ok(ApiResponse<List<LessonProgressResponse>>.Ok(
            await _lessonService.GetByCourseIdWithProgressAsync(id, User.FindUserId(), User.IsStaff())));

    /// <summary>Enroll the signed-in user in a free course. Paid courses go through /payment.</summary>
    [HttpPost("{id:int}/enroll")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EnrollmentResponse>>> Enroll(int id)
    {
        var enrollment = await _enrollmentService.SelfEnrollAsync(User.GetUserId(), id);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<EnrollmentResponse>.Ok(enrollment, "Enrolled successfully."));
    }

    /// <summary>Create a course. The slug is generated from the title.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CourseResponse>>> Create([FromBody] CreateCourseRequest request)
    {
        var created = await _courseService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<CourseResponse>.Ok(created, "Course created successfully."));
    }

    /// <summary>Update a course.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseResponse>>> Update(int id, [FromBody] UpdateCourseRequest request)
        => Ok(ApiResponse<CourseResponse>.Ok(await _courseService.UpdateAsync(id, request), "Course updated successfully."));

    /// <summary>Delete a course and all its lessons.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        await _courseService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Course deleted successfully."));
    }
}
