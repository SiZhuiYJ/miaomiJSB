using System;
using System.Collections.Generic;

namespace api.Data;

/// <summary>
/// Friendships stored per user direction
/// </summary>
public partial class UserFriendship
{
    /// <summary>
    /// Primary key
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// User id
    /// </summary>
    public ulong UserId { get; set; }

    /// <summary>
    /// Friend user id
    /// </summary>
    public ulong FriendUserId { get; set; }

    /// <summary>
    /// Source friend request id
    /// </summary>
    public ulong? SourceRequestId { get; set; }

    /// <summary>
    /// Source group conversation id
    /// </summary>
    public ulong? SourceConversationId { get; set; }

    /// <summary>
    /// Friendship status
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Friend remark
    /// </summary>
    public string? FriendRemark { get; set; }

    /// <summary>
    /// Starred flag
    /// </summary>
    public bool IsStarred { get; set; }

    /// <summary>
    /// Mute flag for this friend
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Accepted time
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Operator user id who created the relation
    /// </summary>
    public ulong? CreatedByUserId { get; set; }

    /// <summary>
    /// Deleted time
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Operator user id who deleted the relation
    /// </summary>
    public ulong? DeletedByUserId { get; set; }

    /// <summary>
    /// Created time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual User? DeletedByUser { get; set; }

    public virtual User FriendUser { get; set; } = null!;

    public virtual ChatConversation? SourceConversation { get; set; }

    public virtual UserFriendRequest? SourceRequest { get; set; }

    public virtual User User { get; set; } = null!;
}
