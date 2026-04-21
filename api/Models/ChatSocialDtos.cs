using System;
using System.Collections.Generic;

namespace api.Models;

public class InviteConversationMembersRequest
{
    public List<ulong> MemberUserIds { get; set; } = new();
}

public class KickConversationMemberRequest
{
    public string? Reason { get; set; }
}

public class MuteConversationMemberRequest
{
    public string Mode { get; set; } = "temporary";
    public int? DurationMinutes { get; set; }
    public string? Reason { get; set; }
}

public class DisbandConversationRequest
{
    public string? Reason { get; set; }
}

public class CreateFriendRequestRequest
{
    public ulong? ReceiverUserId { get; set; }
    public string? ReceiverUserAccount { get; set; }
    public string? RequestMessage { get; set; }
    public ulong? SourceConversationId { get; set; }
    public string? RequestSource { get; set; }
}

public class RejectFriendRequestRequest
{
    public string? RejectReason { get; set; }
}

public class FriendUserDto
{
    public ulong UserId { get; set; }
    public string? UserAccount { get; set; }
    public string? NickName { get; set; }
    public string? AvatarKey { get; set; }
}

public class FriendshipDto
{
    public ulong Id { get; set; }
    public string Status { get; set; } = "active";
    public string? FriendRemark { get; set; }
    public bool IsStarred { get; set; }
    public bool IsMuted { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public FriendUserDto Friend { get; set; } = new();
}

public class FriendRequestDto
{
    public ulong Id { get; set; }
    public ulong RequesterUserId { get; set; }
    public ulong ReceiverUserId { get; set; }
    public ulong? SourceConversationId { get; set; }
    public string? RequestMessage { get; set; }
    public string RequestSource { get; set; } = "account";
    public string RequestStatus { get; set; } = "pending";
    public string Direction { get; set; } = "received";
    public ulong? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? ExpireAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public FriendUserDto Requester { get; set; } = new();
    public FriendUserDto Receiver { get; set; } = new();
}
