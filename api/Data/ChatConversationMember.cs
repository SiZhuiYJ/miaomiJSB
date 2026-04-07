using System;

namespace api.Data;

/// <summary>
/// 会话成员关系表
/// </summary>
public partial class ChatConversationMember
{
    public ulong Id { get; set; }

    public ulong ConversationId { get; set; }

    public ulong UserId { get; set; }

    public string MemberRole { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public DateTime? MuteUntil { get; set; }

    public bool IsPinned { get; set; }

    public bool IsMuted { get; set; }

    public ulong? LastReadMessageId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
