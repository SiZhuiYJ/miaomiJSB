using api.Data;
using api.Hubs;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api.Controllers;

[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class FriendsController(DailyCheckDbContext db, IHubContext<ChatHub> hubContext) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;
    readonly IHubContext<ChatHub> _hubContext = hubContext;

    [HttpGet]
    public async Task<ActionResult<List<FriendshipDto>>> GetMyFriends()
    {
        var userId = GetUserId();
        var friendships = await _db.UserFriendships
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Status == "active")
            .OrderByDescending(f => f.IsStarred)
            .ThenByDescending(f => f.AcceptedAt ?? f.CreatedAt)
            .Select(f => new FriendshipDto
            {
                Id = f.Id,
                Status = f.Status,
                FriendRemark = f.FriendRemark,
                IsStarred = f.IsStarred,
                IsMuted = f.IsMuted,
                AcceptedAt = f.AcceptedAt,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                Friend = new FriendUserDto
                {
                    UserId = f.FriendUserId,
                    UserAccount = f.FriendUser.UserAccount,
                    NickName = f.FriendUser.NickName,
                    AvatarKey = f.FriendUser.AvatarKey
                }
            })
            .ToListAsync();

        return Ok(friendships);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<List<FriendRequestDto>>> GetMyFriendRequests([FromQuery] string? status = null)
    {
        var userId = GetUserId();
        await ExpirePendingRequests();

        var normalizedStatus = NormalizeValue(status);
        var query = _db.UserFriendRequests
            .AsNoTracking()
            .Where(r => r.RequesterUserId == userId || r.ReceiverUserId == userId);

        if (normalizedStatus is "pending" or "accepted" or "rejected" or "cancelled" or "expired")
            query = query.Where(r => r.RequestStatus == normalizedStatus);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(BuildFriendRequestProjection(userId))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<FriendSearchResultDto>>> SearchUsers([FromQuery] string? keyword)
    {
        var userId = GetUserId();
        await ExpirePendingRequests();

        var normalizedKeyword = NormalizeReason(keyword);
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
            return BadRequest(new { message = "keyword is required" });

        var now = DateTime.UtcNow;
        var users = await _db.Users
            .AsNoTracking()
            .Where(u =>
                u.Id != userId &&
                !u.IsDeleted &&
                u.Status == true &&
                (u.UserAccount == normalizedKeyword ||
                 (u.NickName != null && u.NickName.Contains(normalizedKeyword))))
            .OrderByDescending(u => u.UserAccount == normalizedKeyword)
            .ThenByDescending(u => u.NickName == normalizedKeyword)
            .ThenBy(u => u.Id)
            .Select(u => new FriendSearchResultDto
            {
                UserId = u.Id,
                UserAccount = u.UserAccount,
                NickName = u.NickName,
                AvatarKey = u.AvatarKey,
                IsFriend = _db.UserFriendships.Any(f =>
                    f.UserId == userId &&
                    f.FriendUserId == u.Id &&
                    f.Status == "active"),
                HasPendingSentRequest = _db.UserFriendRequests.Any(r =>
                    r.RequesterUserId == userId &&
                    r.ReceiverUserId == u.Id &&
                    r.RequestStatus == "pending" &&
                    (r.ExpireAt == null || r.ExpireAt > now)),
                HasPendingReceivedRequest = _db.UserFriendRequests.Any(r =>
                    r.RequesterUserId == u.Id &&
                    r.ReceiverUserId == userId &&
                    r.RequestStatus == "pending" &&
                    (r.ExpireAt == null || r.ExpireAt > now))
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("requests")]
    public async Task<ActionResult<FriendRequestDto>> CreateFriendRequest(CreateFriendRequestRequest request)
    {
        var userId = GetUserId();
        await ExpirePendingRequests();

        var receiver = await ResolveReceiver(request);
        if (receiver == null)
            return BadRequest(new { message = "Receiver user was not found" });

        if (receiver.Id == userId)
            return BadRequest(new { message = "Cannot add yourself as a friend" });

        if (await _db.UserFriendships.AnyAsync(f =>
                f.UserId == userId &&
                f.FriendUserId == receiver.Id &&
                f.Status == "active"))
        {
            return BadRequest(new { message = "You are already friends with this user" });
        }

        if (request.SourceConversationId.HasValue)
        {
            var sourceConversationValid = await _db.ChatConversations
                .AsNoTracking()
                .AnyAsync(c =>
                    c.Id == request.SourceConversationId.Value &&
                    c.IsActive == true &&
                    c.ConversationType == "group" &&
                    c.ChatConversationMembers.Any(m => m.UserId == userId && m.LeftAt == null) &&
                    c.ChatConversationMembers.Any(m => m.UserId == receiver.Id && m.LeftAt == null));

            if (!sourceConversationValid)
                return BadRequest(new { message = "Source conversation is invalid or the target user is not in that group" });
        }

        var existingPending = await _db.UserFriendRequests
            .FirstOrDefaultAsync(r =>
                r.RequesterUserId == userId &&
                r.ReceiverUserId == receiver.Id &&
                r.RequestStatus == "pending");

        if (existingPending != null)
        {
            var existingDto = await BuildFriendRequestDto(existingPending.Id, userId);
            return existingDto == null
                ? BadRequest(new { message = "A pending friend request already exists" })
                : Ok(existingDto);
        }

        var reversePending = await _db.UserFriendRequests
            .FirstOrDefaultAsync(r =>
                r.RequesterUserId == receiver.Id &&
                r.ReceiverUserId == userId &&
                r.RequestStatus == "pending" &&
                (r.ExpireAt == null || r.ExpireAt > DateTime.UtcNow));

        if (reversePending != null)
        {
            var acceptedAt = DateTime.UtcNow;
            reversePending.RequestStatus = "accepted";
            reversePending.HandledByUserId = userId;
            reversePending.HandledAt = acceptedAt;
            reversePending.UpdatedAt = acceptedAt;

            await UpsertFriendships(
                reversePending.RequesterUserId,
                reversePending.ReceiverUserId,
                reversePending.Id,
                reversePending.SourceConversationId ?? request.SourceConversationId,
                userId,
                acceptedAt);

            await _db.SaveChangesAsync();
            await NotifyFriendRequestsUpdated(reversePending.RequesterUserId, reversePending.ReceiverUserId);
            await NotifyFriendshipsUpdated(reversePending.RequesterUserId, reversePending.ReceiverUserId);

            var acceptedDto = await BuildFriendRequestDto(reversePending.Id, userId);
            return acceptedDto == null
                ? BadRequest(new { message = "Friend request was accepted but could not be loaded" })
                : Ok(acceptedDto);
        }

        var now = DateTime.UtcNow;
        var friendRequest = new UserFriendRequest
        {
            RequesterUserId = userId,
            ReceiverUserId = receiver.Id,
            SourceConversationId = request.SourceConversationId,
            RequestMessage = NormalizeReason(request.RequestMessage),
            RequestSource = NormalizeRequestSource(request.RequestSource),
            RequestStatus = "pending",
            ExpireAt = now.AddDays(7),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.UserFriendRequests.Add(friendRequest);
        await _db.SaveChangesAsync();
        await NotifyFriendRequestsUpdated(friendRequest.RequesterUserId, friendRequest.ReceiverUserId);

        var detail = await BuildFriendRequestDto(friendRequest.Id, userId);
        return detail == null
            ? BadRequest(new { message = "Friend request was created but could not be loaded" })
            : Ok(detail);
    }

    [HttpPost("requests/{requestId:ulong}/accept")]
    public async Task<ActionResult<FriendRequestDto>> AcceptFriendRequest(ulong requestId)
    {
        var userId = GetUserId();
        await ExpirePendingRequests();

        var friendRequest = await _db.UserFriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverUserId == userId);

        if (friendRequest == null)
            return NotFound(new { message = "Friend request not found" });

        if (friendRequest.RequestStatus != "pending")
            return BadRequest(new { message = "Only pending friend requests can be accepted" });

        var now = DateTime.UtcNow;
        friendRequest.RequestStatus = "accepted";
        friendRequest.HandledByUserId = userId;
        friendRequest.HandledAt = now;
        friendRequest.UpdatedAt = now;

        await UpsertFriendships(
            friendRequest.RequesterUserId,
            friendRequest.ReceiverUserId,
            friendRequest.Id,
            friendRequest.SourceConversationId,
            userId,
            now);

        await _db.SaveChangesAsync();
        await NotifyFriendRequestsUpdated(friendRequest.RequesterUserId, friendRequest.ReceiverUserId);
        await NotifyFriendshipsUpdated(friendRequest.RequesterUserId, friendRequest.ReceiverUserId);

        var detail = await BuildFriendRequestDto(friendRequest.Id, userId);
        return detail == null
            ? BadRequest(new { message = "Friend request was accepted but could not be loaded" })
            : Ok(detail);
    }

    [HttpPost("requests/{requestId:ulong}/reject")]
    public async Task<ActionResult<FriendRequestDto>> RejectFriendRequest(
        ulong requestId,
        [FromBody] RejectFriendRequestRequest? request = null)
    {
        var userId = GetUserId();
        await ExpirePendingRequests();

        var friendRequest = await _db.UserFriendRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReceiverUserId == userId);

        if (friendRequest == null)
            return NotFound(new { message = "Friend request not found" });

        if (friendRequest.RequestStatus != "pending")
            return BadRequest(new { message = "Only pending friend requests can be rejected" });

        var now = DateTime.UtcNow;
        friendRequest.RequestStatus = "rejected";
        friendRequest.HandledByUserId = userId;
        friendRequest.HandledAt = now;
        friendRequest.RejectReason = NormalizeReason(request?.RejectReason);
        friendRequest.UpdatedAt = now;

        await _db.SaveChangesAsync();
        await NotifyFriendRequestsUpdated(friendRequest.RequesterUserId, friendRequest.ReceiverUserId);

        var detail = await BuildFriendRequestDto(friendRequest.Id, userId);
        return detail == null
            ? BadRequest(new { message = "Friend request was rejected but could not be loaded" })
            : Ok(detail);
    }

    async Task<User?> ResolveReceiver(CreateFriendRequestRequest request)
    {
        if (request.ReceiverUserId.HasValue)
        {
            return await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == request.ReceiverUserId.Value &&
                !u.IsDeleted &&
                u.Status == true);
        }

        var receiverUserAccount = NormalizeReason(request.ReceiverUserAccount);
        if (string.IsNullOrWhiteSpace(receiverUserAccount))
            return null;

        return await _db.Users.FirstOrDefaultAsync(u =>
            u.UserAccount == receiverUserAccount &&
            !u.IsDeleted &&
            u.Status == true);
    }

    async Task UpsertFriendships(
        ulong requesterUserId,
        ulong receiverUserId,
        ulong sourceRequestId,
        ulong? sourceConversationId,
        ulong operatorUserId,
        DateTime acceptedAt)
    {
        var pair = await _db.UserFriendships
            .Where(f =>
                (f.UserId == requesterUserId && f.FriendUserId == receiverUserId) ||
                (f.UserId == receiverUserId && f.FriendUserId == requesterUserId))
            .ToListAsync();

        UpsertDirectionalFriendship(pair, requesterUserId, receiverUserId, sourceRequestId, sourceConversationId, operatorUserId, acceptedAt);
        UpsertDirectionalFriendship(pair, receiverUserId, requesterUserId, sourceRequestId, sourceConversationId, operatorUserId, acceptedAt);
    }

    void UpsertDirectionalFriendship(
        List<UserFriendship> pair,
        ulong userId,
        ulong friendUserId,
        ulong sourceRequestId,
        ulong? sourceConversationId,
        ulong operatorUserId,
        DateTime acceptedAt)
    {
        var friendship = pair.FirstOrDefault(f => f.UserId == userId && f.FriendUserId == friendUserId);
        if (friendship == null)
        {
            _db.UserFriendships.Add(new UserFriendship
            {
                UserId = userId,
                FriendUserId = friendUserId,
                SourceRequestId = sourceRequestId,
                SourceConversationId = sourceConversationId,
                Status = "active",
                AcceptedAt = acceptedAt,
                CreatedByUserId = operatorUserId,
                CreatedAt = acceptedAt,
                UpdatedAt = acceptedAt
            });
            return;
        }

        friendship.SourceRequestId = sourceRequestId;
        friendship.SourceConversationId = sourceConversationId;
        friendship.Status = "active";
        friendship.AcceptedAt = acceptedAt;
        friendship.DeletedAt = null;
        friendship.DeletedByUserId = null;
        friendship.UpdatedAt = acceptedAt;
        friendship.CreatedByUserId ??= operatorUserId;
    }

    async Task ExpirePendingRequests()
    {
        var now = DateTime.UtcNow;
        var expiredRequests = await _db.UserFriendRequests
            .Where(r => r.RequestStatus == "pending" && r.ExpireAt != null && r.ExpireAt <= now)
            .ToListAsync();

        if (expiredRequests.Count == 0)
            return;

        foreach (var request in expiredRequests)
        {
            request.RequestStatus = "expired";
            request.HandledAt ??= now;
            request.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
    }

    async Task<FriendRequestDto?> BuildFriendRequestDto(ulong requestId, ulong userId)
    {
        return await _db.UserFriendRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(BuildFriendRequestProjection(userId))
            .SingleOrDefaultAsync();
    }

    static Expression<Func<UserFriendRequest, FriendRequestDto>> BuildFriendRequestProjection(ulong userId)
    {
        return r => new FriendRequestDto
        {
            Id = r.Id,
            RequesterUserId = r.RequesterUserId,
            ReceiverUserId = r.ReceiverUserId,
            SourceConversationId = r.SourceConversationId,
            RequestMessage = r.RequestMessage,
            RequestSource = r.RequestSource,
            RequestStatus = r.RequestStatus,
            Direction = r.RequesterUserId == userId ? "sent" : "received",
            HandledByUserId = r.HandledByUserId,
            HandledAt = r.HandledAt,
            RejectReason = r.RejectReason,
            ExpireAt = r.ExpireAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Requester = new FriendUserDto
            {
                UserId = r.RequesterUserId,
                UserAccount = r.RequesterUser.UserAccount,
                NickName = r.RequesterUser.NickName,
                AvatarKey = r.RequesterUser.AvatarKey
            },
            Receiver = new FriendUserDto
            {
                UserId = r.ReceiverUserId,
                UserAccount = r.ReceiverUser.UserAccount,
                NickName = r.ReceiverUser.NickName,
                AvatarKey = r.ReceiverUser.AvatarKey
            }
        };
    }

    static string NormalizeValue(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    static string? NormalizeReason(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    static string NormalizeRequestSource(string? value)
        => NormalizeValue(value) switch
        {
            "group" => "group",
            "search" => "search",
            "system" => "system",
            _ => "account"
        };

    Task NotifyFriendRequestsUpdated(params ulong[] userIds)
        => NotifyUsers("chat:friend-requests-updated", userIds);

    Task NotifyFriendshipsUpdated(params ulong[] userIds)
        => NotifyUsers("chat:friendships-updated", userIds);

    async Task NotifyUsers(string eventName, IEnumerable<ulong> userIds)
    {
        var groups = userIds
            .Where(id => id != 0)
            .Distinct()
            .Select(ChatHub.UserGroupName)
            .ToList();

        if (groups.Count == 0)
            return;

        await _hubContext.Clients.Groups(groups).SendAsync(eventName, new
        {
            triggeredAt = DateTime.UtcNow
        });
    }

    ulong GetUserId()
    {
        var candidateTypes = new[] { ClaimTypes.NameIdentifier, "sub", "nameid", "user_id", "id" };

        foreach (var type in candidateTypes)
        {
            var val = User.FindFirstValue(type);
            if (!string.IsNullOrEmpty(val))
            {
                if (ulong.TryParse(val, out var id))
                    return id;

                throw new InvalidOperationException($"Unable to parse user ID from claim '{type}' with value '{val}'");
            }
        }

        throw new InvalidOperationException("User ID claim was not found in the token");
    }
}
