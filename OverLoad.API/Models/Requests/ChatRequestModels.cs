namespace OverLoad.API.Models.Requests;

public class ChatRequestModel
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Up to 3 recent exchanges (6 items) to keep context. The server stores no history.</summary>
    public List<ChatHistoryItemModel> RecentHistory { get; set; } = new();
}

public class ChatHistoryItemModel
{
    /// <summary>"user" or "model".</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
