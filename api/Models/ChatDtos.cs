namespace api.Models;

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class SendChatMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class SendChatMessageResponse
{
    public ChatMessageDto Reply { get; set; } = new();
    public List<ChatMessageDto> History { get; set; } = new();
}
