using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// 聊天消息表
/// </summary>
public partial class ChatMessage
{
    public ulong Id { get; set; }

    public ulong ConversationId { get; set; }

    public ulong SenderUserId { get; set; }

    public string MessageType { get; set; } = null!;

    public string? Content { get; set; }

    public string? Extra { get; set; }

    public ulong? ReplyToMessageId { get; set; }

    public bool IsRecalled { get; set; }

    public DateTime? RecalledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual ChatMessage? ReplyToMessage { get; set; }

    public virtual ICollection<ChatMessage> InverseReplyToMessage { get; set; } = new List<ChatMessage>();

    public virtual User SenderUser { get; set; } = null!;

    public virtual ICollection<ChatMessageReceipt> ChatMessageReceipts { get; set; } = new List<ChatMessageReceipt>();
}
