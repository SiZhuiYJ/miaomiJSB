using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// 聊天会话表（双人私聊）
/// </summary>
public partial class ChatSession
{
    public ulong Id { get; set; }

    public string SessionNo { get; set; } = null!;

    public sbyte SessionType { get; set; }

    public ulong CreatedByUserId { get; set; }

    public ulong? LastMessageId { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<ChatSessionMember> ChatSessionMembers { get; set; } = new List<ChatSessionMember>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
