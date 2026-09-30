using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Extensions;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
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

    /// <summary>Paginated, searchable list of users. SortBy: email, fullName, createdAt.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponseModel<UserResponseModel>>>> GetAll([FromQuery] UserQueryRequestModel query)
    {
        var page = await _userService.GetAllAsync(query.ToServiceDto());
        return Ok(ApiResponse<PagedResponseModel<UserResponseModel>>.Ok(page.ToResponseModel(u => u.ToResponseModel())));
    }

    /// <summary>A single user including enrollment history.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseModel>>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(ApiResponse<UserDetailResponseModel>.Ok(user.ToResponseModel()));
    }

    /// <summary>Create a user with any role.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserResponseModel>>> Create([FromBody] CreateUserRequestModel request)
    {
        var created = await _userService.CreateAsync(request.ToServiceDto());
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            ApiResponse<UserResponseModel>.Ok(created.ToResponseModel(), "User created successfully."));
    }

    /// <summary>Update a user, including role and verification status.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponseModel>>> Update(int id, [FromBody] UpdateUserRequestModel request)
    {
        var user = await _userService.UpdateAsync(id, request.ToServiceDto());
        return Ok(ApiResponse<UserResponseModel>.Ok(user.ToResponseModel(), "User updated successfully."));
    }

    /// <summary>Lock an account: sign-in and token refresh are refused. Admins cannot lock themselves.</summary>
    [HttpPost("{id:int}/lock")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponseModel>>> Lock(int id)
    {
        if (id == User.GetUserId())
            throw new BusinessRuleException("You cannot lock your own account.");

        var user = await _userService.SetLockedAsync(id, locked: true);
        return Ok(ApiResponse<UserResponseModel>.Ok(user.ToResponseModel(), "Account locked."));
    }

    /// <summary>Unlock an account.</summary>
    [HttpPost("{id:int}/unlock")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponseModel>>> Unlock(int id)
    {
        var user = await _userService.SetLockedAsync(id, locked: false);
        return Ok(ApiResponse<UserResponseModel>.Ok(user.ToResponseModel(), "Account unlocked."));
    }

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
