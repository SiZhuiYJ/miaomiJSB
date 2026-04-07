using System;

namespace api.Data;

/// <summary>
/// 聊天消息表
/// </summary>
public partial class ChatMessage
{
    public ulong Id { get; set; }

    public ulong SessionId { get; set; }

    public ulong SenderUserId { get; set; }

    public sbyte MessageType { get; set; }

    public string Content { get; set; } = null!;

    public string? ClientMsgNo { get; set; }

    public sbyte SendStatus { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ChatSession Session { get; set; } = null!;

    public virtual User SenderUser { get; set; } = null!;
}
