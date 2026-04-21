using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// Group action logs
/// </summary>
public partial class ChatGroupActionLog
{
    /// <summary>
    /// Primary key
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Group conversation id
    /// </summary>
    public ulong ConversationId { get; set; }

    /// <summary>
    /// Group action type
    /// </summary>
    public string ActionType { get; set; } = null!;

    /// <summary>
    /// Operator user id
    /// </summary>
    public ulong? OperatorUserId { get; set; }

    /// <summary>
    /// Target user id
    /// </summary>
    public ulong? TargetUserId { get; set; }

    /// <summary>
    /// Related system message id
    /// </summary>
    public ulong? RelatedMessageId { get; set; }

    /// <summary>
    /// Action reason
    /// </summary>
    public string? ActionReason { get; set; }

    /// <summary>
    /// Extra JSON payload
    /// </summary>
    public string? ActionPayload { get; set; }

    /// <summary>
    /// Created time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual ChatConversation Conversation { get; set; } = null!;

    public virtual User? OperatorUser { get; set; }

    public virtual ChatMessage? RelatedMessage { get; set; }

    public virtual User? TargetUser { get; set; }
}
