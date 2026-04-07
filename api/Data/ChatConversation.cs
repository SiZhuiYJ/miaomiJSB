using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// 聊天会话主表
/// </summary>
public partial class ChatConversation
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// 会话类型：direct=双人，group=多人/群聊
    /// </summary>
    public string ConversationType { get; set; } = null!;

    /// <summary>
    /// 会话标题（群聊可配置）
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 群主/创建者用户ID
    /// </summary>
    public ulong? OwnerUserId { get; set; }

    /// <summary>
    /// 会话头像
    /// </summary>
    public string? AvatarKey { get; set; }

    /// <summary>
    /// 是否可用：1可用，0停用
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatConversationMember> ChatConversationMembers { get; set; } = new List<ChatConversationMember>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual User? OwnerUser { get; set; }
}
