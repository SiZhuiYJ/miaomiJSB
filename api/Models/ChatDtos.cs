namespace api.Models;

public class StartConversationRequest
{
    public ulong TargetUserId { get; set; }
}

public class ConversationDto
{
    public ulong SessionId { get; set; }
    public string SessionNo { get; set; } = string.Empty;
    public ulong TargetUserId { get; set; }
    public string TargetUserName { get; set; } = string.Empty;
    public string? TargetAvatarKey { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime? LastMessageAt { get; set; }
    public uint UnreadCount { get; set; }
}

public class ChatMessageDto
{
    public ulong Id { get; set; }
    public ulong SessionId { get; set; }
    public ulong SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class SendChatMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public string? ClientMsgNo { get; set; }
}

public class SendChatMessageResponse
{
    public ChatMessageDto Message { get; set; } = new();
}

public class MarkAsReadRequest
{
    public ulong LastReadMessageId { get; set; }
}
