using System;

namespace api.Data;

/// <summary>
/// 聊天会话成员表（双人私聊固定2人）
/// </summary>
public partial class ChatSessionMember
{
    public ulong Id { get; set; }

    public ulong SessionId { get; set; }

    public ulong UserId { get; set; }

    public sbyte Role { get; set; }

    public ulong? LastReadMessageId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public uint UnreadCount { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ChatSession Session { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
