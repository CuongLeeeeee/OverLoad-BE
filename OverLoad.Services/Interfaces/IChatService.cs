using OverLoad.Services.DTOs.Request;
using OverLoad.Services.DTOs.Response;

namespace OverLoad.Services.Interfaces;

public interface IChatService
{
    Task<ChatReplyDto> SendMessageAsync(ChatRequest request);
}
