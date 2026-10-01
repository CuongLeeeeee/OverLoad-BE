using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Responses;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Instructor-specific endpoints: course management and submit-for-review workflow.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/instructor")]
[Produces("application/json")]
[Authorize(Roles = Roles.Instructor)]
public class InstructorController : ControllerBase
{
    private readonly ICourseService _courseService;

    public InstructorController(ICourseService courseService) => _courseService = courseService;

    /// <summary>
    /// All courses owned by the signed-in instructor, across all statuses (Draft, PendingReview, Published, Rejected).
    /// Does not include courses where InstructorId belongs to someone else.
    /// </summary>
    [HttpGet("courses")]
    public async Task<ActionResult<ApiResponse<List<CourseResponseModel>>>> GetMyCourses()
    {
        var courses = await _courseService.GetInstructorCoursesAsync(User.GetUserId());
        return Ok(ApiResponse<List<CourseResponseModel>>.Ok(courses.Select(c => c.ToResponseModel()).ToList()));
    }

    /// <summary>
    /// Submit a Draft or Rejected course for admin review.
    /// The course status changes to PendingReview; the instructor cannot edit it until approved or rejected.
    /// </summary>
    [HttpPost("courses/{id:int}/submit")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseResponseModel>>> SubmitForReview(int id)
    {
        var course = await _courseService.SubmitForReviewAsync(id, User.GetUserId());
        return Ok(ApiResponse<CourseResponseModel>.Ok(course.ToResponseModel(),
            "Course submitted for admin review. You will be notified when it is approved or rejected."));
    }
}
