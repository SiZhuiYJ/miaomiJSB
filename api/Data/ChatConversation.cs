using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// 聊天会话主表
/// </summary>
public partial class ChatConversation
{
    public ulong Id { get; set; }

    public string ConversationType { get; set; } = null!;

    public string? Title { get; set; }

    public ulong? OwnerUserId { get; set; }

    public string? AvatarKey { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User? OwnerUser { get; set; }

    public virtual ICollection<ChatConversationMember> ChatConversationMembers { get; set; } = new List<ChatConversationMember>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
