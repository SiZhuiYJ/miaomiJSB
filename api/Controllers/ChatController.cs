using System.Security.Claims;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("mm/chat")]
[Authorize]
public class ChatController(DailyCheckDbContext db) : ControllerBase
{
    private readonly DailyCheckDbContext _db = db;

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDetailDto>> CreateConversation(CreateConversationRequest request)
    {
        var currentUserId = GetUserId();
        var conversationType = NormalizeConversationType(request.ConversationType);
        if (conversationType == null)
        {
            return BadRequest(new { message = "conversationType 仅支持 direct 或 group" });
        }

        var memberIds = request.MemberUserIds
            .Append(currentUserId)
            .Distinct()
            .ToList();

        if (memberIds.Count < 2)
        {
            return BadRequest(new { message = "聊天成员至少需要2人" });
        }

        if (conversationType == "direct" && memberIds.Count != 2)
        {
            return BadRequest(new { message = "direct 会话只能包含2名成员" });
        }

        var validUserCount = await _db.Users.CountAsync(x => memberIds.Contains(x.Id) && !x.IsDeleted && x.Status == true);
        if (validUserCount != memberIds.Count)
        {
            return BadRequest(new { message = "成员列表包含不存在或不可用用户" });
        }

        if (conversationType == "direct")
        {
            var existingConversationId = await _db.ChatConversations
                .Where(c => c.ConversationType == "direct" && c.IsActive)
                .Where(c => c.ChatConversationMembers.Count(m => m.LeftAt == null) == 2)
                .Where(c => c.ChatConversationMembers.Any(m => m.UserId == memberIds[0] && m.LeftAt == null))
                .Where(c => c.ChatConversationMembers.Any(m => m.UserId == memberIds[1] && m.LeftAt == null))
                .Select(c => (ulong?)c.Id)
                .FirstOrDefaultAsync();

            if (existingConversationId.HasValue)
            {
                return await GetConversation(existingConversationId.Value);
            }
        }

        var now = DateTime.UtcNow;
        var conversation = new ChatConversation
        {
            ConversationType = conversationType,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            OwnerUserId = conversationType == "group" ? currentUserId : null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var memberId in memberIds)
        {
            var role = memberId == currentUserId
                ? (conversationType == "group" ? "owner" : "member")
                : "member";
            conversation.ChatConversationMembers.Add(new ChatConversationMember
            {
                UserId = memberId,
                MemberRole = role,
                JoinedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();

        return await GetConversation(conversation.Id);
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetMyConversations()
    {
        var currentUserId = GetUserId();
        var currentUserIdLocal = currentUserId;

        var list = await _db.ChatConversations
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Where(c => c.ChatConversationMembers.Any(m => m.UserId == currentUserId && m.LeftAt == null))
            .Select(c => new ConversationSummaryDto
            {
                ConversationId = c.Id,
                ConversationType = c.ConversationType,
                Title = c.Title,
                OwnerUserId = c.OwnerUserId,
                UpdatedAt = c.UpdatedAt,
                MemberCount = c.ChatConversationMembers.Count(m => m.LeftAt == null),
                LastReadMessageId = c.ChatConversationMembers
                    .Where(m => m.UserId == currentUserId && m.LeftAt == null)
                    .Select(m => m.LastReadMessageId)
                    .FirstOrDefault(),
                LastMessage = c.ChatMessages
                    .OrderByDescending(m => m.Id)
                    .Select(m => new ChatMessageListItemDto
                    {
                        MessageId = m.Id,
                        ConversationId = m.ConversationId,
                        SenderUserId = m.SenderUserId,
                        SenderName = m.SenderUser.NickName ?? m.SenderUser.UserAccount,
                        MessageType = m.MessageType,
                        Content = m.Content,
                        Extra = m.Extra,
                        ReplyToMessageId = m.ReplyToMessageId,
                        IsRecalled = m.IsRecalled,
                        CreatedAt = m.CreatedAt
                    })
                    .FirstOrDefault(),
                UnreadCount = c.ChatMessages.Count(m =>
                    m.Id > (c.ChatConversationMembers
                        .Where(cm => cm.UserId == currentUserIdLocal && cm.LeftAt == null)
                        .Select(cm => cm.LastReadMessageId ?? 0UL)
                        .FirstOrDefault())
                    && m.SenderUserId != currentUserIdLocal)
            })
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("conversations/{conversationId:long}")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversation(ulong conversationId)
    {
        var currentUserId = GetUserId();
        var exists = await _db.ChatConversationMembers.AnyAsync(x => x.ConversationId == conversationId && x.UserId == currentUserId && x.LeftAt == null);
        if (!exists)
        {
            return NotFound(new { message = "会话不存在或无权限访问" });
        }

        var data = await _db.ChatConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId && c.IsActive)
            .Select(c => new ConversationDetailDto
            {
                ConversationId = c.Id,
                ConversationType = c.ConversationType,
                Title = c.Title,
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
                        JoinedAt = m.JoinedAt
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        return data == null ? NotFound(new { message = "会话不存在" }) : Ok(data);
    }

    [HttpPost("conversations/{conversationId:long}/messages")]
    public async Task<ActionResult<ChatMessageListItemDto>> SendMessage(ulong conversationId, SendMessageRequest request)
    {
        var currentUserId = GetUserId();
        var member = await _db.ChatConversationMembers
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == currentUserId && x.LeftAt == null);
        if (member == null)
        {
            return NotFound(new { message = "会话不存在或无权限发送消息" });
        }

        var messageType = NormalizeMessageType(request.MessageType);
        if (messageType == null)
        {
            return BadRequest(new { message = "messageType 仅支持 text/image/file/system" });
        }

        if (messageType == "text" && string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "文本消息 content 不能为空" });
        }

        if (request.ReplyToMessageId.HasValue)
        {
            var replyMessageExists = await _db.ChatMessages.AnyAsync(x => x.Id == request.ReplyToMessageId.Value && x.ConversationId == conversationId);
            if (!replyMessageExists)
            {
                return BadRequest(new { message = "replyToMessageId 不存在或不属于当前会话" });
            }
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = currentUserId,
            MessageType = messageType,
            Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim(),
            Extra = request.Extra,
            ReplyToMessageId = request.ReplyToMessageId,
            IsRecalled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ChatMessages.Add(message);

        var conversation = await _db.ChatConversations.FirstAsync(x => x.Id == conversationId);
        conversation.UpdatedAt = now;

        await _db.SaveChangesAsync();

        member.LastReadMessageId = message.Id;
        member.UpdatedAt = now;
        await _db.SaveChangesAsync();

        var sender = await _db.Users.AsNoTracking().Where(x => x.Id == currentUserId).Select(x => new { x.NickName, x.UserAccount }).FirstAsync();
        return Ok(new ChatMessageListItemDto
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            SenderUserId = message.SenderUserId,
            SenderName = sender.NickName ?? sender.UserAccount,
            MessageType = message.MessageType,
            Content = message.Content,
            Extra = message.Extra,
            ReplyToMessageId = message.ReplyToMessageId,
            IsRecalled = message.IsRecalled,
            CreatedAt = message.CreatedAt
        });
    }

    [HttpGet("conversations/{conversationId:long}/messages")]
    public async Task<ActionResult<MessageListResponse>> GetMessages(ulong conversationId, [FromQuery] ulong? beforeMessageId = null, [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetUserId();
        var exists = await _db.ChatConversationMembers.AnyAsync(x => x.ConversationId == conversationId && x.UserId == currentUserId && x.LeftAt == null);
        if (!exists)
        {
            return NotFound(new { message = "会话不存在或无权限访问" });
        }

        var size = Math.Clamp(pageSize, 1, 100);
        var query = _db.ChatMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.Id)
            .AsQueryable();

        if (beforeMessageId.HasValue)
        {
            query = query.Where(x => x.Id < beforeMessageId.Value);
        }

        var messages = await query
            .Take(size)
            .Select(m => new ChatMessageListItemDto
            {
                MessageId = m.Id,
                ConversationId = m.ConversationId,
                SenderUserId = m.SenderUserId,
                SenderName = m.SenderUser.NickName ?? m.SenderUser.UserAccount,
                MessageType = m.MessageType,
                Content = m.Content,
                Extra = m.Extra,
                ReplyToMessageId = m.ReplyToMessageId,
                IsRecalled = m.IsRecalled,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        messages.Reverse();

        return Ok(new MessageListResponse
        {
            Messages = messages,
            NextBeforeMessageId = messages.Count == size ? messages.First().MessageId : null
        });
    }

    [HttpPost("conversations/{conversationId:long}/read")]
    public async Task<ActionResult> MarkConversationRead(ulong conversationId, MarkConversationReadRequest request)
    {
        var currentUserId = GetUserId();
        var member = await _db.ChatConversationMembers
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == currentUserId && x.LeftAt == null);
        if (member == null)
        {
            return NotFound(new { message = "会话不存在或无权限访问" });
        }

        ulong? targetMessageId = request.LastReadMessageId;
        if (!targetMessageId.HasValue)
        {
            targetMessageId = await _db.ChatMessages
                .Where(x => x.ConversationId == conversationId)
                .OrderByDescending(x => x.Id)
                .Select(x => (ulong?)x.Id)
                .FirstOrDefaultAsync();
        }

        if (targetMessageId.HasValue)
        {
            var valid = await _db.ChatMessages.AnyAsync(x => x.Id == targetMessageId.Value && x.ConversationId == conversationId);
            if (!valid)
            {
                return BadRequest(new { message = "lastReadMessageId 不属于当前会话" });
            }

            member.LastReadMessageId = targetMessageId;
        }

        member.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "已更新已读状态", lastReadMessageId = member.LastReadMessageId });
    }

    private ulong GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!ulong.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("无效的用户身份");
        }

        return userId;
    }

    private static string? NormalizeConversationType(string? type)
    {
        var value = (type ?? "group").Trim().ToLowerInvariant();
        return value is "direct" or "group" ? value : null;
    }

    private static string? NormalizeMessageType(string? type)
    {
        var value = (type ?? "text").Trim().ToLowerInvariant();
        return value is "text" or "image" or "file" or "system" ? value : null;
    }
}
