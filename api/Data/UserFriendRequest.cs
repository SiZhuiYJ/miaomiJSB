using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// Friend requests
/// </summary>
public partial class UserFriendRequest
{
    /// <summary>
    /// Primary key
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// Requester user id
    /// </summary>
    public ulong RequesterUserId { get; set; }

    /// <summary>
    /// Receiver user id
    /// </summary>
    public ulong ReceiverUserId { get; set; }

    /// <summary>
    /// Source group conversation id
    /// </summary>
    public ulong? SourceConversationId { get; set; }

    /// <summary>
    /// Request message
    /// </summary>
    public string? RequestMessage { get; set; }

    /// <summary>
    /// Request source
    /// </summary>
    public string RequestSource { get; set; } = null!;

    /// <summary>
    /// Request status
    /// </summary>
    public string RequestStatus { get; set; } = null!;

    /// <summary>
    /// Handler user id
    /// </summary>
    public ulong? HandledByUserId { get; set; }

    /// <summary>
    /// Handled time
    /// </summary>
    public DateTime? HandledAt { get; set; }

    /// <summary>
    /// Reject reason
    /// </summary>
    public string? RejectReason { get; set; }

    /// <summary>
    /// Expire time
    /// </summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// Created time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual User? HandledByUser { get; set; }

    public virtual User ReceiverUser { get; set; } = null!;

    public virtual User RequesterUser { get; set; } = null!;

    public virtual ChatConversation? SourceConversation { get; set; }

    public virtual ICollection<UserFriendship> UserFriendships { get; set; } = new List<UserFriendship>();
}
