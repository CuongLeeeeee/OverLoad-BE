using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OverLoad.API.Common;
using OverLoad.Services.Exceptions;

namespace OverLoad.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id. Throws 401 when the token carries no valid id.</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        user.FindUserId() ?? throw new UnauthorizedException("Invalid token.");

    /// <summary>The user's id, or null for anonymous requests on [AllowAnonymous] endpoints.</summary>
    public static int? FindUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static bool IsStaff(this ClaimsPrincipal user) =>
        user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Instructor);

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
