using System.Text.Json;
using OverLoad.API.Common;
using OverLoad.Services.Exceptions;

namespace OverLoad.API.Middleware;

/// <summary>
/// Turns exceptions into the ApiResponse envelope. Expected business failures (AppException)
/// map to 4xx with their message; anything else is logged and returned as a generic 500
/// so internal details never reach the client.
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, StatusCodeFor(ex), ApiResponse.Fail(ex.Message, ex.Errors.Count > 0 ? ex.Errors : null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, ApiResponse.Fail("An unexpected error occurred."));
        }
    }

    private static int StatusCodeFor(AppException ex) => ex switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ConflictException => StatusCodes.Status409Conflict,
        UnauthorizedException => StatusCodes.Status401Unauthorized,
        ForbiddenException => StatusCodes.Status403Forbidden,
        ExternalServiceException => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status400BadRequest
    };

    private static Task WriteAsync(HttpContext context, int statusCode, ApiResponse<object?> body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
