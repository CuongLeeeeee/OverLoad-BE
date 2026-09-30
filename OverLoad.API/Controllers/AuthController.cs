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

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Register a new account (Student role).</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponseModel>>> Register([FromBody] RegisterRequestModel request)
    {
        var result = await _authService.RegisterAsync(request.ToServiceDto());
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponseModel>.Ok(result.ToResponseModel(), "Registration successful."));
    }

    /// <summary>Login — returns access token + refresh token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseModel>>> Login([FromBody] LoginRequestModel request)
    {
        var result = await _authService.LoginAsync(request.ToServiceDto());
        return Ok(ApiResponse<AuthResponseModel>.Ok(result.ToResponseModel(), "Login successful."));
    }

    /// <summary>Get a new access token using a valid refresh token (the refresh token is rotated).</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseModel>>> Refresh([FromBody] RefreshTokenRequestModel request)
    {
        var result = await _authService.RefreshTokenAsync(request.ToServiceDto());
        return Ok(ApiResponse<AuthResponseModel>.Ok(result.ToResponseModel(), "Token refreshed successfully."));
    }

    /// <summary>Logout — invalidates the refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object?>>> Logout()
    {
        await _authService.LogoutAsync(User.GetUserId());
        return Ok(ApiResponse.Ok("Logged out successfully."));
    }
}
