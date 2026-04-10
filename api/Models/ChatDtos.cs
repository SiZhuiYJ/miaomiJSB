using System;
using System.Collections.Generic;

namespace api.Models;

public class CreateConversationRequest
{
    public string ConversationType { get; set; } = "direct";
    public string? Title { get; set; }
    public string? AvatarKey { get; set; }
    public List<ulong> MemberUserIds { get; set; } = new();
}

// 更新会话信息
public class UpdateConversationRequest
{
    public string? Title { get; set; }
    public string? AvatarKey { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPinned { get; set; }
    public bool? IsMuted { get; set; }
}

public class ConversationMemberDto
{
    public ulong UserId { get; set; }
    public string? NickName { get; set; }
    public string? AvatarKey { get; set; }
    public string MemberRole { get; set; } = "member";
    public DateTime JoinedAt { get; set; }
    public ulong? LastReadMessageId { get; set; }
}

public class MessageSummaryDto
{
    public ulong Id { get; set; }
    public ulong SenderUserId { get; set; }
    public string? SenderNickName { get; set; }
    public string MessageType { get; set; } = "text";
    public string? Content { get; set; }
    public string? Extra { get; set; }
    public ulong? ReplyToMessageId { get; set; }
    public bool IsRecalled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationSummaryDto
{
    public ulong Id { get; set; }
    public string ConversationType { get; set; } = "direct";
    public string? Title { get; set; }
    public string? AvatarKey { get; set; }
    public ulong? AvatarUserId { get; set; }
    public bool IsActive { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UnreadCount { get; set; }
    public MessageSummaryDto? LastMessage { get; set; }
}

public class ConversationDetailDto
{
    public ulong Id { get; set; }
    public string ConversationType { get; set; } = "direct";
    public string? Title { get; set; }
    public string? AvatarKey { get; set; }
    public ulong? AvatarUserId { get; set; }
    public bool IsActive { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public ulong? OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ConversationMemberDto> Members { get; set; } = new();
}

public class SendMessageRequest
{
    public string MessageType { get; set; } = "text";
    public string? Content { get; set; }
    public string? Extra { get; set; }
    public ulong? ReplyToMessageId { get; set; }
}


public class MessageDeltaDto
{
    public ulong LastMessageId { get; set; }
    public bool HasMore { get; set; }
    public List<MessageSummaryDto> Messages { get; set; } = new();
}

public class ReadConversationRequest
{
    public ulong? LastReadMessageId { get; set; }
}
