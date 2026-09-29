namespace OverLoad.Services.DTOs.Request;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItem> RecentHistory { get; set; } = new();
}

public class ChatHistoryItem
{
    /// <summary>"user" or "model".</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
