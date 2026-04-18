
using api.Data;
using api.Hubs;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace api.Controllers;

/// <summary>
/// 閼卞﹤銇夐幒褍鍩楅崳顭掔礉婢跺嫮鎮婃导姘崇樈閸掓稑缂撻妴浣圭Х閹垰褰傞柅浣碘偓浣筋嚢閸欐牜鐡戦惄绋垮彠閸旂喕鍏?
/// </summary>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class ChatController(DailyCheckDbContext db, IHubContext<ChatHub> hubContext) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;
    readonly IHubContext<ChatHub> _hubContext = hubContext;
    static readonly HashSet<string> AllowedMessageTypes = new(["text", "image", "video", "audio", "file", "system"]);

    /// <summary>
    /// 閸掓稑缂撴稉鈧稉顏呮煀閻ㄥ嫯浜版径鈺€绱扮拠?
    /// </summary>
    /// <param name="request">閸掓稑缂撴导姘崇樈閻ㄥ嫯顕Ч鍌氬棘閺?/param>
    /// <returns>閸掓稑缂撻幋鎰閻ㄥ嫪绱扮拠婵婎嚊閹?/returns>
    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDetailDto>> CreateConversation(CreateConversationRequest request)
    {
        var userId = GetUserId();
        var conversationType = (request.ConversationType ?? string.Empty).Trim().ToLowerInvariant();
        if (conversationType is not ("direct" or "group"))
            return BadRequest(new { message = "conversationType 娴犲懏鏁幐?direct 閹?group" });

        var memberIds = request.MemberUserIds
            .Append(userId)
            .Distinct()
            .ToList();

        if (conversationType == "direct" && memberIds.Count != 2)
            return BadRequest(new { message = "Direct conversations must include exactly 2 users" });

        if (conversationType == "group" && memberIds.Count < 2)
            return BadRequest(new { message = "Group conversations must include at least 2 users" });

        var validUserIds = await _db.Users
            .Where(x => memberIds.Contains(x.Id) && !x.IsDeleted && x.Status == true)
            .Select(x => x.Id)
            .ToListAsync();

        if (validUserIds.Count != memberIds.Count)
            return BadRequest(new { message = "One or more members are invalid, deleted, or disabled" });

        if (conversationType == "direct")
        {
            var peerUserId = memberIds.Single(x => x != userId);
            var existingDirect = await _db.ChatConversations
                .Where(c => c.ConversationType == "direct" && c.IsActive == true)
                .Where(c => c.ChatConversationMembers.Any(m => m.UserId == userId && m.LeftAt == null))
                .Where(c => c.ChatConversationMembers.Any(m => m.UserId == peerUserId && m.LeftAt == null))
                .Where(c => c.ChatConversationMembers.Count(m => m.LeftAt == null) == 2)
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (existingDirect != 0)
            {
                return Ok(await BuildConversationDetail(existingDirect, userId));
            }
        }

        var now = DateTime.UtcNow;
        var conversation = new ChatConversation
        {
            ConversationType = conversationType,
            Title = conversationType == "group" ? request.Title : null,
            AvatarKey = request.AvatarKey,
            OwnerUserId = conversationType == "group" ? userId : null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var memberUserId in memberIds)
        {
            conversation.ChatConversationMembers.Add(new ChatConversationMember
            {
                UserId = memberUserId,
                MemberRole = conversationType == "group" && memberUserId == userId ? "owner" : "member",
                JoinedAt = now,
                IsMuted = false,
                IsPinned = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();

        var detail = await BuildConversationDetail(conversation.Id, userId);
        return CreatedAtAction(nameof(GetConversationById), new { conversationId = conversation.Id }, detail);
    }

    /// <summary>
    /// 鏇存柊缇ゆ垚鍛樿鑹?
    /// </summary>
    [HttpPost("conversations/{conversationId:ulong}/members/{memberUserId:ulong}/role")]
    public async Task<ActionResult<ConversationDetailDto>> UpdateConversationMemberRole(
        ulong conversationId,
        ulong memberUserId,
        UpdateConversationMemberRoleRequest request)
    {
        var userId = GetUserId();
        var conversation = await _db.ChatConversations
            .Include(c => c.ChatConversationMembers)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive == true);

        if (conversation == null)
            return NotFound(new { message = "Conversation not found" });

        if (conversation.ConversationType != "group")
            return BadRequest(new { message = "Only group conversations support member role management" });

        var currentMember = conversation.ChatConversationMembers
            .FirstOrDefault(m => m.UserId == userId && m.LeftAt == null);
        if (currentMember == null)
            return NotFound(new { message = "Conversation not found or access denied" });

        var currentRole = (currentMember.MemberRole ?? string.Empty).Trim().ToLowerInvariant();
        if (currentRole != "owner")
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the owner can update member roles" });

        var targetMember = conversation.ChatConversationMembers
            .FirstOrDefault(m => m.UserId == memberUserId && m.LeftAt == null);
        if (targetMember == null)
            return NotFound(new { message = "Target member not found" });

        if (targetMember.UserId == userId)
            return BadRequest(new { message = "Cannot change your own group role" });

        var targetRole = (targetMember.MemberRole ?? string.Empty).Trim().ToLowerInvariant();
        if (targetRole == "owner")
            return BadRequest(new { message = "Cannot change the owner role" });

        var nextRole = (request.MemberRole ?? string.Empty).Trim().ToLowerInvariant();
        if (nextRole is not ("admin" or "member"))
            return BadRequest(new { message = "memberRole only supports admin or member" });

        if (targetRole == nextRole)
        {
            var unchangedDetail = await BuildConversationDetail(conversationId, userId);
            return unchangedDetail == null
                ? NotFound(new { message = "Conversation not found" })
                : Ok(unchangedDetail);
        }

        var now = DateTime.UtcNow;
        targetMember.MemberRole = nextRole;
        targetMember.UpdatedAt = now;
        conversation.UpdatedAt = now;

        await _db.SaveChangesAsync();
        var updatedDetail = await BuildConversationDetail(conversationId, userId);
        return updatedDetail == null ? NotFound(new { message = "Conversation not found" }) : Ok(updatedDetail);
    }

    /// <summary>
    /// 閼惧嘲褰囪ぐ鎾冲閻劍鍩涢惃鍕窗鐠囨繂鍨悰?
    /// </summary>
    /// <returns>娴兼俺鐦介幗妯款洣閸掓銆?/returns>
    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetMyConversations()
    {
        var userId = GetUserId();

        var memberships = await _db.ChatConversationMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.LeftAt == null && m.Conversation.IsActive == true)
            .Select(m => new
            {
                m.ConversationId,
                m.LastReadMessageId,
                m.IsPinned,
                m.UpdatedAt
            })
            .ToListAsync();

        if (memberships.Count == 0)
            return Ok(new List<ConversationSummaryDto>());

        var conversationIds = memberships.Select(m => m.ConversationId).ToList();

        var conversationDtos = await _db.ChatConversations
            .AsNoTracking()
            .Where(c => conversationIds.Contains(c.Id))
            .Select(BuildConversationSummaryProjection(userId))
            .ToListAsync();

        var unreadCounts = await (
            from message in _db.ChatMessages.AsNoTracking()
            join member in _db.ChatConversationMembers.AsNoTracking()
                on message.ConversationId equals member.ConversationId
            where member.UserId == userId
                && member.LeftAt == null
                && conversationIds.Contains(message.ConversationId)
                && message.SenderUserId != userId
                && message.Id > (member.LastReadMessageId ?? 0UL)
            group message by message.ConversationId into grouped
            select new
            {
                ConversationId = grouped.Key,
                Count = grouped.Count()
            })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

        var membershipMap = memberships.ToDictionary(x => x.ConversationId);
        foreach (var item in conversationDtos)
        {
            item.UnreadCount = unreadCounts.GetValueOrDefault(item.Id);
        }

        var ordered = conversationDtos
            .OrderByDescending(x => membershipMap[x.Id].IsPinned)
            .ThenByDescending(x => x.LastMessage?.Id ?? 0)
            .ToList();

        return Ok(ordered);
    }

    /// <summary>
    /// 閺囧瓨鏌婃导姘崇樈娣団剝浼?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <param name="request">閺囧瓨鏌婃导姘崇樈閻ㄥ嫯顕Ч鍌氬棘閺?/param>
    /// <returns>閺囧瓨鏌婇崥搴ｆ畱娴兼俺鐦界拠锔藉剰</returns>
    [HttpPost("conversations/{conversationId:ulong}")]
    public async Task<ActionResult<ConversationDetailDto>> UpdateConversation(ulong conversationId, UpdateConversationRequest request)
    {
        var userId = GetUserId();
        var conversation = await _db.ChatConversations
            .Include(c => c.ChatConversationMembers)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive == true);

        if (conversation == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }
        if (!conversation.ChatConversationMembers.Any(m => m.UserId == userId && m.LeftAt == null))
        {
            return NotFound(new { message = "Conversation not found or access denied" });
        }
        // 閺囧瓨鏌婃导姘崇樈娣団剝浼呴崚鐗堟殶閹诡喖绨?
        var chatConversationMember = conversation.ChatConversationMembers
            .FirstOrDefault(m => m.UserId == userId && m.LeftAt == null);
        if (chatConversationMember == null)
        {
            return NotFound(new { message = "Conversation not found" });
        }
        var memberRole = (chatConversationMember.MemberRole ?? string.Empty).Trim().ToLowerInvariant();
        var canManageGroupConversation = conversation.ConversationType == "group" && (memberRole == "owner" || memberRole == "admin");
        var isUpdatingConversationMetadata =
            !string.IsNullOrWhiteSpace(request.Title)
            || !string.IsNullOrWhiteSpace(request.AvatarKey)
            || request.IsActive != null;

        if (conversation.ConversationType == "group" && isUpdatingConversationMetadata && !canManageGroupConversation)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Only the owner or an admin can update group information" });
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
            conversation.Title = request.Title;
        if (!string.IsNullOrWhiteSpace(request.AvatarKey))
            conversation.AvatarKey = request.AvatarKey;
        if (request.IsActive != null)
            conversation.IsActive = request.IsActive;
        if (request.IsMuted != null)
            chatConversationMember.IsMuted = request.IsMuted ?? chatConversationMember.IsMuted;
        if (request.IsPinned != null)
            chatConversationMember.IsPinned = request.IsPinned ?? chatConversationMember.IsPinned;

        await _db.SaveChangesAsync();
        var detail = await BuildConversationDetail(conversationId, userId);
        return detail == null ? NotFound(new { message = "Conversation not found" }) : Ok(detail);
    }

    /// <summary>
    /// 閺嶈宓両D閼惧嘲褰囨导姘崇樈鐠囷附鍎?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <returns>娴兼俺鐦界拠锔藉剰</returns>
    [HttpGet("conversations/{conversationId:ulong}")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversationById(ulong conversationId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);

        if (!isMember)
            return NotFound(new { message = "Conversation not found or access denied" });

        var detail = await BuildConversationDetail(conversationId, userId);
        return detail == null ? NotFound(new { message = "Conversation not found" }) : Ok(detail);
    }

    /// <summary>
    /// 閸︺劍瀵氱€规矮绱扮拠婵呰厬閸欐垿鈧焦绉烽幁?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <param name="request">閸欐垿鈧焦绉烽幁顖滄畱鐠囬攱鐪伴崣鍌涙殶</param>
    /// <returns>閸欐垿鈧胶娈戝☉鍫熶紖鐠囷附鍎?/returns>
    [HttpPost("conversations/{conversationId:ulong}/messages")]
    public async Task<ActionResult<MessageSummaryDto>> SendMessage(ulong conversationId, SendMessageRequest request)
    {
        var userId = GetUserId();

        var membership = await _db.ChatConversationMembers
            .Include(m => m.Conversation)
            .SingleOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);

        if (membership == null || membership.Conversation.IsActive != true)
            return NotFound(new { message = "Conversation not found or sending is not allowed" });

        var messageType = (request.MessageType ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedMessageTypes.Contains(messageType))
            return BadRequest(new { message = "messageType 娴犲懏鏁幐?text/image/video/audio/file/system" });

        if (messageType == "text" && string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "閺傚洦婀板☉鍫熶紖 content 娑撳秷鍏樻稉铏光敄" });

        // 閺傚洣娆㈢猾缁樼Х閹垶娓剁憰涔獂tra鐎涙顔岄崠鍛儓閺傚洣娆㈡穱鈩冧紖
        if ((messageType == "image" || messageType == "video" || messageType == "audio" || messageType == "file")
            && request.Extra == null)
            return BadRequest(new { message = $"{messageType} 濞戝牊浼?extra 娑撳秷鍏樻稉铏光敄閿涘矂娓堕崠鍛儓閺傚洣娆㈡穱鈩冧紖" });

        if (request.ReplyToMessageId.HasValue)
        {
            var replyExists = await _db.ChatMessages.AnyAsync(x => x.Id == request.ReplyToMessageId.Value && x.ConversationId == conversationId);
            if (!replyExists)
                return BadRequest(new { message = "replyToMessageId does not exist or does not belong to this conversation" });
        }

        if (messageType == "image" || messageType == "video" || messageType == "audio" || messageType == "file")
            request.Content = request.Extra?.FileKey;

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            MessageType = messageType,
            Content = request.Content,
            Extra = request.Extra == null ? null : JsonSerializer.Serialize(request.Extra),
            ReplyToMessageId = request.ReplyToMessageId,
            IsRecalled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ChatMessages.Add(message);
        membership.UpdatedAt = now;
        membership.Conversation.UpdatedAt = now;

        await _db.SaveChangesAsync();
        membership.LastReadMessageId = message.Id;
        await _db.SaveChangesAsync();

        var result = await BuildMessageSummary(message.Id);

        await _hubContext.Clients.Group(ChatHub.GroupName(conversationId)).SendAsync("chat:message-updated", new
        {
            conversationId,
            messageId = message.Id,
            messageType = messageType,
            createdAt = now,
            action = "create",
            message = result
        });

        return Ok(result);
    }

    /// <summary>
    /// 閹俱倕娲栧☉鍫熶紖
    /// </summary>
    /// <param name="messageId">濞戝牊浼匢D</param>
    /// <returns>閹俱倕娲栭崥搴ｆ畱濞戝牊浼呴幗妯款洣</returns>
    [HttpPost("messages/{messageId:ulong}/recall")]
    public async Task<ActionResult<MessageSummaryDto>> RecallMessage(ulong messageId)
    {
        const int recallWindowMinutes = 5;
        var userId = GetUserId();

        var message = await _db.ChatMessages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
            return NotFound(new { message = "Message not found" });

        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == message.ConversationId && m.UserId == userId && m.LeftAt == null);

        if (!isMember || message.Conversation.IsActive != true)
            return NotFound(new { message = "Conversation not found or access denied" });

        if (message.SenderUserId != userId)
            return BadRequest(new { message = "Only your own messages can be recalled" });

        if (message.IsRecalled)
            return Ok(await BuildMessageSummary(message.Id));

        var now = DateTime.UtcNow;
        if (message.CreatedAt.AddMinutes(recallWindowMinutes) < now)
            return BadRequest(new { message = $"Messages older than {recallWindowMinutes} minutes cannot be recalled" });

        message.IsRecalled = true;
        message.RecalledAt = now;
        message.Content = null;
        message.Extra = null;
        message.UpdatedAt = now;

        await _db.SaveChangesAsync();

        var result = await BuildMessageSummary(message.Id);

        await _hubContext.Clients.Group(ChatHub.GroupName(message.ConversationId)).SendAsync("chat:message-updated", new
        {
            conversationId = message.ConversationId,
            messageId = message.Id,
            action = "recall",
            updatedAt = now,
            message = result
        });

        return Ok(result);
    }

    /// <summary>
    /// 閼惧嘲褰囨导姘崇樈娑擃厾娈戝☉鍫熶紖閸掓銆?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <param name="beforeMessageId">閼惧嘲褰囧銈嗙Х閹枠D娑斿澧犻惃鍕Х閹?/param>
    /// <param name="pageSize">濮ｅ繘銆夋径褍鐨敍宀勭帛鐠併倓璐?0</param>
    /// <returns>濞戝牊浼呴幗妯款洣閸掓銆?/returns>
    [HttpGet("conversations/{conversationId:ulong}/messages")]
    public async Task<ActionResult<List<MessageSummaryDto>>> GetMessages(ulong conversationId, [FromQuery] ulong? beforeMessageId, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "Conversation not found or access denied" });

        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        if (beforeMessageId.HasValue)
            query = query.Where(m => m.Id < beforeMessageId.Value);

        var messages = await query
            .OrderByDescending(m => m.Id)
            .Take(normalizedPageSize)
            .Select(m => new MessageSummaryDto
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                SenderNickName = m.SenderUser.NickName,
                MessageType = m.MessageType,
                Content = m.IsRecalled ? null : m.Content,
                Extra = m.IsRecalled ? null : m.Extra,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage == null ? null : new MessageReferenceDto
                {
                    Id = m.ReplyToMessage.Id,
                    SenderUserId = m.ReplyToMessage.SenderUserId,
                    SenderNickName = m.ReplyToMessage.SenderUser.NickName,
                    MessageType = m.ReplyToMessage.MessageType,
                    Content = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Content,
                    Extra = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Extra,
                    IsRecalled = m.ReplyToMessage.IsRecalled,
                    CreatedAt = m.ReplyToMessage.CreatedAt
                },
                IsRecalled = m.IsRecalled,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
        messages.Reverse();
        return Ok(messages);
    }

    /// <summary>
    /// 閼惧嘲褰囨导姘崇樈娑擃厾娈戝☉鍫熶紖婢х偤鍣?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <param name="afterMessageId">閼惧嘲褰囧銈嗙Х閹枠D娑斿鎮楅惃鍕Х閹?/param>
    /// <param name="pageSize">濮ｅ繘銆夋径褍鐨敍宀勭帛鐠併倓璐?0</param>
    /// <returns>濞戝牊浼呮晶鐐哄櫤閺佺増宓?/returns>
    [HttpGet("conversations/{conversationId:ulong}/messages/delta")]
    public async Task<ActionResult<MessageDeltaDto>> GetMessageDelta(ulong conversationId, [FromQuery] ulong? afterMessageId, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "Conversation not found or access denied" });

        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var threshold = afterMessageId ?? 0;
        var lastMessageId = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id)
            .Select(m => (ulong?)m.Id)
            .FirstOrDefaultAsync() ?? 0;

        var records = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.Id > threshold)
            .OrderBy(m => m.Id)
            .Take(normalizedPageSize + 1)
            .Select(m => new MessageSummaryDto
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                SenderNickName = m.SenderUser.NickName,
                MessageType = m.MessageType,
                Content = m.IsRecalled ? null : m.Content,
                Extra = m.IsRecalled ? null : m.Extra,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage == null ? null : new MessageReferenceDto
                {
                    Id = m.ReplyToMessage.Id,
                    SenderUserId = m.ReplyToMessage.SenderUserId,
                    SenderNickName = m.ReplyToMessage.SenderUser.NickName,
                    MessageType = m.ReplyToMessage.MessageType,
                    Content = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Content,
                    Extra = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Extra,
                    IsRecalled = m.ReplyToMessage.IsRecalled,
                    CreatedAt = m.ReplyToMessage.CreatedAt
                },
                IsRecalled = m.IsRecalled,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        var hasMore = records.Count > normalizedPageSize;
        if (hasMore)
            records = records.Take(normalizedPageSize).ToList();

        return Ok(new MessageDeltaDto
        {
            LastMessageId = lastMessageId,
            HasMore = hasMore,
            Messages = records
        });
    }

    /// <summary>
    /// 閺嶅洩顔囨导姘崇樈娑撳搫鍑＄拠?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <param name="request">閺嶅洩顔囧鑼额嚢閻ㄥ嫯顕Ч鍌氬棘閺?/param>
    /// <returns>閹垮秳缍旂紒鎾寸亯</returns>
    [HttpPost("conversations/{conversationId:ulong}/read")]
    public async Task<ActionResult> MarkConversationRead(ulong conversationId, ReadConversationRequest request)
    {
        var userId = GetUserId();

        var membership = await _db.ChatConversationMembers
            .SingleOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (membership == null)
            return NotFound(new { message = "Conversation not found or access denied" });

        ulong? targetMessageId = request.LastReadMessageId;

        if (!targetMessageId.HasValue)
        {
            targetMessageId = await _db.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Id)
                .Select(m => (ulong?)m.Id)
                .FirstOrDefaultAsync();
        }

        if (targetMessageId.HasValue)
        {
            var exists = await _db.ChatMessages.AnyAsync(m => m.Id == targetMessageId.Value && m.ConversationId == conversationId);
            if (!exists)
                return BadRequest(new { message = "lastReadMessageId does not exist or does not belong to this conversation" });
        }

        var previousLastRead = membership.LastReadMessageId ?? 0;
        membership.LastReadMessageId = targetMessageId;
        membership.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (targetMessageId.HasValue && targetMessageId.Value > previousLastRead)
        {
            var readIds = await _db.ChatMessages
                .Where(m => m.ConversationId == conversationId
                    && m.Id > previousLastRead
                    && m.Id <= targetMessageId.Value
                    && m.SenderUserId != userId)
                .Select(m => m.Id)
                .ToListAsync();

            if (readIds.Count > 0)
            {
                var existingReceiptMessageIds = await _db.ChatMessageReceipts
                    .Where(r => r.UserId == userId && readIds.Contains(r.MessageId))
                    .Select(r => r.MessageId)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                var newReceipts = readIds
                    .Except(existingReceiptMessageIds)
                    .Select(messageId => new ChatMessageReceipt
                    {
                        MessageId = messageId,
                        UserId = userId,
                        ReadAt = now
                    })
                    .ToList();

                if (newReceipts.Count > 0)
                {
                    _db.ChatMessageReceipts.AddRange(newReceipts);
                    await _db.SaveChangesAsync();
                }
            }
        }

        return Ok(new { lastReadMessageId = membership.LastReadMessageId });
    }

    /// <summary>
    /// 閼惧嘲褰囧☉鍫熶紖閻ㄥ嫬鍑＄拠鏄忣嚊閹?
    /// </summary>
    /// <param name="messageId">濞戝牊浼匢D</param>
    /// <returns>濞戝牊浼呭鑼额嚢鐠囷附鍎?/returns>
    [HttpGet("messages/{messageId:ulong}/read-status")]
    public async Task<ActionResult<MessageReadDetailDto>> GetMessageReadStatus(ulong messageId)
    {
        var userId = GetUserId();

        var message = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null)
            return NotFound(new { message = "Message not found" });

        // 妤犲矁鐦夐弶鍐
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == message.ConversationId
                        && m.UserId == userId
                        && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "Access denied" });

        // 閺屻儴顕楀鑼额嚢閻劍鍩?
        var readUsers = await _db.ChatMessageReceipts
            .Where(r => r.MessageId == messageId)
            .Join(_db.Users, r => r.UserId, u => u.Id, (r, u) => new ReadUserDto
            {
                UserId = u.Id,
                NickName = u.NickName,
                AvatarKey = u.AvatarKey,
                ReadAt = r.ReadAt
            })
            .ToListAsync();

        // 鐠侊紕鐣婚幀缁樺复閺€鎯扳偓鍛殶閿涘牊甯撻梽銈呭絺闁浇鈧拑绱?
        var totalRecipients = await _db.ChatConversationMembers
            .CountAsync(m => m.ConversationId == message.ConversationId
                          && m.UserId != message.SenderUserId
                          && m.LeftAt == null);

        return Ok(new MessageReadDetailDto
        {
            MessageId = messageId,
            TotalRecipients = totalRecipients,
            ReadCount = readUsers.Count,
            ReadUsers = readUsers
        });
    }

    /// <summary>
    /// 閺嬪嫬缂撴导姘崇樈閹芥顩﹂幎鏇炲
    /// </summary>
    /// <param name="userId">閻劍鍩汭D</param>
    /// <returns>娴兼俺鐦介幗妯款洣閹舵洖濂栫悰銊ㄦ彧瀵?/returns>
    static Expression<Func<ChatConversation, ConversationSummaryDto>> BuildConversationSummaryProjection(ulong userId)
    {
        return c => new ConversationSummaryDto
        {
            Id = c.Id,
            ConversationType = c.ConversationType,
            IsActive = c.IsActive ?? true,
            UpdatedAt = c.UpdatedAt,
            Title = c.ConversationType == "direct"
                ? c.ChatConversationMembers
                    .Where(m => m.UserId != userId && m.LeftAt == null)
                    .Select(m => m.User.NickName)
                    .FirstOrDefault() ?? c.Title
                : c.Title,
            AvatarKey = c.ConversationType == "direct"
                ? c.ChatConversationMembers
                    .Where(m => m.UserId != userId && m.LeftAt == null)
                    .Select(m => m.User.AvatarKey)
                    .FirstOrDefault() ?? c.AvatarKey
                : c.AvatarKey,
            AvatarUserId = c.ConversationType == "direct"
                ? c.ChatConversationMembers
                    .Where(m => m.UserId != userId && m.LeftAt == null)
                    .Select(m => (ulong?)m.UserId)
                    .FirstOrDefault()
                : c.OwnerUserId,
            IsPinned = c.ChatConversationMembers
                .Where(m => m.UserId == userId && m.LeftAt == null)
                .Select(m => m.IsPinned)
                .FirstOrDefault(),
            IsMuted = c.ChatConversationMembers
                .Where(m => m.UserId == userId && m.LeftAt == null)
                .Select(m => m.IsMuted)
                .FirstOrDefault(),
            LastMessage = c.ChatMessages
                .OrderByDescending(m => m.Id)
                .Select(m => new MessageSummaryDto
                {
                    Id = m.Id,
                    SenderUserId = m.SenderUserId,
                    SenderNickName = m.SenderUser.NickName,
                    MessageType = m.MessageType,
                    Content = m.IsRecalled ? null : m.Content,
                    Extra = m.IsRecalled ? null : m.Extra,
                    ReplyToMessageId = m.ReplyToMessageId,
                    ReplyToMessage = m.ReplyToMessage == null ? null : new MessageReferenceDto
                    {
                        Id = m.ReplyToMessage.Id,
                        SenderUserId = m.ReplyToMessage.SenderUserId,
                        SenderNickName = m.ReplyToMessage.SenderUser.NickName,
                        MessageType = m.ReplyToMessage.MessageType,
                        Content = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Content,
                        Extra = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Extra,
                        IsRecalled = m.ReplyToMessage.IsRecalled,
                        CreatedAt = m.ReplyToMessage.CreatedAt
                    },
                    IsRecalled = m.IsRecalled,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefault()
        };
    }

    async Task<MessageSummaryDto> BuildMessageSummary(ulong messageId)
    {
        return await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.Id == messageId)
            .Select(m => new MessageSummaryDto
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                SenderNickName = m.SenderUser.NickName,
                MessageType = m.MessageType,
                Content = m.IsRecalled ? null : m.Content,
                Extra = m.IsRecalled ? null : m.Extra,
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage == null ? null : new MessageReferenceDto
                {
                    Id = m.ReplyToMessage.Id,
                    SenderUserId = m.ReplyToMessage.SenderUserId,
                    SenderNickName = m.ReplyToMessage.SenderUser.NickName,
                    MessageType = m.ReplyToMessage.MessageType,
                    Content = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Content,
                    Extra = m.ReplyToMessage.IsRecalled ? null : m.ReplyToMessage.Extra,
                    IsRecalled = m.ReplyToMessage.IsRecalled,
                    CreatedAt = m.ReplyToMessage.CreatedAt
                },
                IsRecalled = m.IsRecalled,
                CreatedAt = m.CreatedAt
            })
            .SingleAsync();
    }

    /// <summary>
    /// 閺嬪嫬缂撴导姘崇樈鐠囷附鍎?
    /// </summary>
    /// <param name="conversationId">娴兼俺鐦絀D</param>
    /// <param name="userId">閻劍鍩汭D</param>
    /// <returns>娴兼俺鐦界拠锔藉剰</returns>
    async Task<ConversationDetailDto?> BuildConversationDetail(ulong conversationId, ulong userId)
    {
        return await _db.ChatConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new ConversationDetailDto
            {
                Id = c.Id,
                ConversationType = c.ConversationType,
                Title = c.ConversationType == "direct"
                    ? c.ChatConversationMembers
                        .Where(m => m.UserId != userId && m.LeftAt == null)
                        .Select(m => m.User.NickName)
                        .FirstOrDefault() ?? c.Title
                    : c.Title,
                AvatarKey = c.ConversationType == "direct"
                    ? c.ChatConversationMembers
                        .Where(m => m.UserId != userId && m.LeftAt == null)
                        .Select(m => m.User.AvatarKey)
                        .FirstOrDefault() ?? c.AvatarKey
                    : c.AvatarKey,
                AvatarUserId = c.ConversationType == "direct"
                    ? c.ChatConversationMembers
                        .Where(m => m.UserId != userId && m.LeftAt == null)
                        .Select(m => (ulong?)m.UserId)
                        .FirstOrDefault()
                    : c.OwnerUserId,
                IsActive = c.IsActive ?? true,
                IsPinned = c.ChatConversationMembers
                    .Where(m => m.UserId == userId && m.LeftAt == null)
                    .Select(m => m.IsPinned)
                    .FirstOrDefault(),
                IsMuted = c.ChatConversationMembers
                    .Where(m => m.UserId == userId && m.LeftAt == null)
                    .Select(m => m.IsMuted)
                    .FirstOrDefault(),
                OwnerUserId = c.OwnerUserId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Members = c.ChatConversationMembers
                    .Where(m => m.LeftAt == null)
                    .OrderBy(m => m.JoinedAt)
                    .Select(m => new ConversationMemberDto
                    {
                        UserId = m.UserId,
                        NickName = m.User.NickName,
                        AvatarKey = m.User.AvatarKey,
                        MemberRole = m.MemberRole,
                        JoinedAt = m.JoinedAt,
                        LastReadMessageId = m.LastReadMessageId
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// 閼惧嘲褰囪ぐ鎾冲閻劍鍩汭D
    /// </summary>
    /// <returns>閻劍鍩汭D</returns>
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

