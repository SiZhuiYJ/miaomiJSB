using System;

namespace api.Data;

/// <summary>
/// 消息已读回执表
/// </summary>
public partial class ChatMessageReceipt
{
    public ulong Id { get; set; }

    public ulong MessageId { get; set; }

    public ulong UserId { get; set; }

    public DateTime ReadAt { get; set; }

    public virtual ChatMessage Message { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
