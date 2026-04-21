using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// 会话成员关系表（支持邀请、踢出、禁言）
/// </summary>
public partial class ChatConversationMember
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public ulong ConversationId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// 成员角色
    /// </summary>
    public string MemberRole { get; set; } = null!;

    /// <summary>
    /// Membership status
    /// </summary>
    public string MembershipStatus { get; set; } = null!;

    /// <summary>
    /// 加入时间
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Invite time
    /// </summary>
    public DateTime? InvitedAt { get; set; }

    /// <summary>
    /// Inviter user id
    /// </summary>
    public ulong? InvitedByUserId { get; set; }

    /// <summary>
    /// 离开时间（NULL表示仍在会话中）
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    /// Removal operator user id
    /// </summary>
    public ulong? RemovedByUserId { get; set; }

    /// <summary>
    /// Leave or removal reason
    /// </summary>
    public string? RemovedReason { get; set; }

    /// <summary>
    /// 禁言截至时间（NULL表示不禁言）
    /// </summary>
    public DateTime? MuteUntil { get; set; }

    /// <summary>
    /// Mute mode
    /// </summary>
    public string? MuteMode { get; set; }

    /// <summary>
    /// Mute reason
    /// </summary>
    public string? MuteReason { get; set; }

    /// <summary>
    /// Mute start time
    /// </summary>
    public DateTime? MutedAt { get; set; }

    /// <summary>
    /// Mute operator user id
    /// </summary>
    public ulong? MutedByUserId { get; set; }

    /// <summary>
    /// 是否置顶会话：1是，0否
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// 是否消息免打扰：1是，0否
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// 最后已读消息ID（逻辑引用）
    /// </summary>
    public ulong? LastReadMessageId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User? InvitedByUser { get; set; }

    public virtual User? MutedByUser { get; set; }

    public virtual User? RemovedByUser { get; set; }

    public virtual User User { get; set; } = null!;
}
