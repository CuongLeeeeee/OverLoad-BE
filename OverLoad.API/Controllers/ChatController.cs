using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OverLoad.API.Common;
using OverLoad.API.Models.Mappings;
using OverLoad.API.Models.Requests;
using OverLoad.API.Models.Responses;
using OverLoad.Services.Interfaces;

namespace OverLoad.API.Controllers;

/// <summary>AI coding assistant powered by Gemini.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chat")]
[Produces("application/json")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
        => _chatService = chatService;

    /// <summary>
    /// Send a message to the AI coding assistant.
    /// Attach up to 3 recent exchanges in RecentHistory to maintain context.
    /// Server does NOT store any chat history.
    /// </summary>
    /// <remarks>
    /// Example request:
    ///
    ///     POST /api/v1/chat
    ///     {
    ///       "message": "Explain what async/await does in C#",
    ///       "recentHistory": [
    ///         { "role": "user",  "content": "What is a delegate in C#?" },
    ///         { "role": "model", "content": "A delegate is a type-safe function pointer..." }
    ///       ]
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<ChatResponseModel>>> Chat([FromBody] ChatRequestModel request)
    {
        var reply = await _chatService.SendMessageAsync(request.ToServiceDto());
        return Ok(ApiResponse<ChatResponseModel>.Ok(reply.ToResponseModel()));
    }
}
