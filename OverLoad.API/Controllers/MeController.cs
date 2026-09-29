using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>Endpoints about the signed-in user. The user id always comes from the token, never the URL.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Produces("application/json")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IProgressService _progressService;

    public MeController(IUserService userService, IEnrollmentService enrollmentService, IProgressService progressService)
    {
        _userService = userService;
        _enrollmentService = enrollmentService;
        _progressService = progressService;
    }

    /// <summary>Profile of the signed-in user, including enrollment history.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetProfile()
        => Ok(ApiResponse<UserDetailResponse>.Ok(await _userService.GetByIdAsync(User.GetUserId())));

    /// <summary>Update name, avatar and bio of the signed-in user.</summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile([FromBody] UpdateProfileRequest request)
        => Ok(ApiResponse<UserResponse>.Ok(
            await _userService.UpdateProfileAsync(User.GetUserId(), request), "Profile updated successfully."));

    /// <summary>Courses the signed-in user is enrolled in, with progress.</summary>
    [HttpGet("courses")]
    public async Task<ActionResult<ApiResponse<List<MyCourseResponse>>>> GetMyCourses()
        => Ok(ApiResponse<List<MyCourseResponse>>.Ok(await _enrollmentService.GetMyCoursesAsync(User.GetUserId())));

    /// <summary>Progress summary of the signed-in user in one course.</summary>
    [HttpGet("courses/{courseId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CourseProgressResponse>>> GetCourseProgress(int courseId)
        => Ok(ApiResponse<CourseProgressResponse>.Ok(
            await _enrollmentService.GetCourseProgressAsync(User.GetUserId(), courseId)));

    /// <summary>Progress of the signed-in user on one lesson.</summary>
    [HttpGet("lessons/{lessonId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProgressResponse>>> GetLessonProgress(int lessonId)
        => Ok(ApiResponse<ProgressResponse>.Ok(
            await _progressService.GetMyLessonProgressAsync(User.GetUserId(), lessonId)));

    /// <summary>
    /// Save the signed-in user's progress on a lesson (video position, scroll, checkpoints).
    /// Creates the record on the first call. Course percentage is recalculated on the server.
    /// </summary>
    [HttpPut("lessons/{lessonId:int}/progress")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProgressResponse>>> SaveLessonProgress(
        int lessonId, [FromBody] SaveProgressRequest request)
        => Ok(ApiResponse<ProgressResponse>.Ok(
            await _progressService.SaveMyLessonProgressAsync(User.GetUserId(), lessonId, request), "Progress saved."));
}
