using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.Services.Common;
using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;
using OverLoad.Services.Exceptions;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>User administration (Admin only). Users manage their own profile through /me.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>Paginated, searchable list of users.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetAll([FromQuery] UserQueryParams query)
        => Ok(ApiResponse<PagedResult<UserResponse>>.Ok(await _userService.GetAllAsync(query)));

    /// <summary>A single user including enrollment history.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetById(int id)
        => Ok(ApiResponse<UserDetailResponse>.Ok(await _userService.GetByIdAsync(id)));

    /// <summary>Create a user with any role.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Create([FromBody] CreateUserRequest request)
    {
        var created = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<UserResponse>.Ok(created, "User created successfully."));
    }

    /// <summary>Update a user, including role and verification status.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(int id, [FromBody] UpdateUserRequest request)
        => Ok(ApiResponse<UserResponse>.Ok(await _userService.UpdateAsync(id, request), "User updated successfully."));

    /// <summary>Delete a user. Admins cannot delete their own account.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        if (id == User.GetUserId())
            throw new BusinessRuleException("You cannot delete your own account.");

        await _userService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("User deleted successfully."));
    }
}
