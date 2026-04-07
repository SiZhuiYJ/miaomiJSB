namespace api.Models;

public class CreateConversationRequest
{
    public string ConversationType { get; set; } = "group";

    public string? Title { get; set; }

    public List<ulong> MemberUserIds { get; set; } = new();
}

public class ConversationSummaryDto
{
    public ulong ConversationId { get; set; }

    public string ConversationType { get; set; } = "group";

    public string? Title { get; set; }

    public ulong? OwnerUserId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int MemberCount { get; set; }

    public ChatMessageListItemDto? LastMessage { get; set; }

    public ulong? LastReadMessageId { get; set; }

    public int UnreadCount { get; set; }
}

public class ConversationDetailDto
{
    public ulong ConversationId { get; set; }

    public string ConversationType { get; set; } = "group";

    public string? Title { get; set; }

    public ulong? OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ConversationMemberDto> Members { get; set; } = new();
}

public class ConversationMemberDto
{
    public ulong UserId { get; set; }

    public string? NickName { get; set; }

    public string? AvatarKey { get; set; }

    public string MemberRole { get; set; } = "member";

    public DateTime JoinedAt { get; set; }
}

public class SendMessageRequest
{
    public string MessageType { get; set; } = "text";

    public string? Content { get; set; }

    public string? Extra { get; set; }

    public ulong? ReplyToMessageId { get; set; }
}

public class ChatMessageListItemDto
{
    public ulong MessageId { get; set; }

    public ulong ConversationId { get; set; }

    public ulong SenderUserId { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string MessageType { get; set; } = "text";

    public string? Content { get; set; }

    public string? Extra { get; set; }

    public ulong? ReplyToMessageId { get; set; }

    public bool IsRecalled { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class MessageListResponse
{
    public List<ChatMessageListItemDto> Messages { get; set; } = new();

    public ulong? NextBeforeMessageId { get; set; }
}

public class MarkConversationReadRequest
{
    public ulong? LastReadMessageId { get; set; }
}
