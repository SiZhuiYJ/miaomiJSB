using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class ChatController(DailyCheckDbContext db) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;
    static readonly HashSet<string> AllowedMessageTypes = new(["text", "image", "file", "system"]);

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDetailDto>> CreateConversation(CreateConversationRequest request)
    {
        var userId = GetUserId();
        var conversationType = (request.ConversationType ?? string.Empty).Trim().ToLowerInvariant();
        if (conversationType is not ("direct" or "group"))
            return BadRequest(new { message = "conversationType 仅支持 direct 或 group" });

        var memberIds = request.MemberUserIds
            .Append(userId)
            .Distinct()
            .ToList();

        if (conversationType == "direct" && memberIds.Count != 2)
            return BadRequest(new { message = "direct 会话必须恰好 2 人" });

        if (conversationType == "group" && memberIds.Count < 2)
            return BadRequest(new { message = "group 会话至少 2 人" });

        var validUserIds = await _db.Users
            .Where(x => memberIds.Contains(x.Id) && !x.IsDeleted && x.Status == true)
            .Select(x => x.Id)
            .ToListAsync();

        if (validUserIds.Count != memberIds.Count)
            return BadRequest(new { message = "存在无效成员（不存在、已删除或已禁用）" });

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
                return Ok(await BuildConversationDetail(existingDirect));
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

        var detail = await BuildConversationDetail(conversation.Id);
        return CreatedAtAction(nameof(GetConversationById), new { conversationId = conversation.Id }, detail);
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> GetMyConversations()
    {
        var userId = GetUserId();

        var memberships = await _db.ChatConversationMembers
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
            .Where(c => conversationIds.Contains(c.Id))
            .Select(c => new ConversationSummaryDto
            {
                Id = c.Id,
                ConversationType = c.ConversationType,
                Title = c.Title,
                AvatarKey = c.AvatarKey,
                IsActive = c.IsActive ?? true,
                UpdatedAt = c.UpdatedAt,
                LastMessage = c.ChatMessages
                    .OrderByDescending(m => m.Id)
                    .Select(m => new MessageSummaryDto
                    {
                        Id = m.Id,
                        SenderUserId = m.SenderUserId,
                        SenderNickName = m.SenderUser.NickName,
                        MessageType = m.MessageType,
                        Content = m.Content,
                        Extra = m.Extra,
                        ReplyToMessageId = m.ReplyToMessageId,
                        IsRecalled = m.IsRecalled,
                        CreatedAt = m.CreatedAt
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var item in conversationDtos)
        {
            var membership = memberships.First(x => x.ConversationId == item.Id);
            var lastReadMessageId = membership.LastReadMessageId ?? 0;
            item.UnreadCount = await _db.ChatMessages
                .Where(m => m.ConversationId == item.Id && m.Id > lastReadMessageId && m.SenderUserId != userId)
                .CountAsync();
        }

        var ordered = conversationDtos
            .OrderByDescending(x => memberships.First(m => m.ConversationId == x.Id).IsPinned)
            .ThenByDescending(x => x.LastMessage?.Id ?? 0)
            .ToList();

        return Ok(ordered);
    }

    [HttpGet("conversations/{conversationId:ulong}")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversationById(ulong conversationId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);

        if (!isMember)
            return NotFound(new { message = "会话不存在或无权限访问" });

        var detail = await BuildConversationDetail(conversationId);
        return detail == null ? NotFound(new { message = "会话不存在" }) : Ok(detail);
    }

    [HttpPost("conversations/{conversationId:ulong}/messages")]
    public async Task<ActionResult<MessageSummaryDto>> SendMessage(ulong conversationId, SendMessageRequest request)
    {
        var userId = GetUserId();

        var membership = await _db.ChatConversationMembers
            .Include(m => m.Conversation)
            .SingleOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);

        if (membership == null || membership.Conversation.IsActive != true)
            return NotFound(new { message = "会话不存在或无权限发送消息" });

        var messageType = (request.MessageType ?? string.Empty).Trim().ToLowerInvariant();
        if (!AllowedMessageTypes.Contains(messageType))
            return BadRequest(new { message = "messageType 仅支持 text/image/file/system" });

        if (messageType == "text" && string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "文本消息 content 不能为空" });

        if (request.ReplyToMessageId.HasValue)
        {
            var replyExists = await _db.ChatMessages.AnyAsync(x => x.Id == request.ReplyToMessageId.Value && x.ConversationId == conversationId);
            if (!replyExists)
                return BadRequest(new { message = "replyToMessageId 不存在或不属于当前会话" });
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            MessageType = messageType,
            Content = request.Content,
            Extra = request.Extra,
            ReplyToMessageId = request.ReplyToMessageId,
            IsRecalled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ChatMessages.Add(message);
        membership.LastReadMessageId = message.Id;
        membership.UpdatedAt = now;
        membership.Conversation.UpdatedAt = now;

        await _db.SaveChangesAsync();

        var result = await _db.ChatMessages
            .Where(m => m.Id == message.Id)
            .Select(m => new MessageSummaryDto
            {
                Id = m.Id,
                SenderUserId = m.SenderUserId,
                SenderNickName = m.SenderUser.NickName,
                MessageType = m.MessageType,
                Content = m.Content,
                Extra = m.Extra,
                ReplyToMessageId = m.ReplyToMessageId,
                IsRecalled = m.IsRecalled,
                CreatedAt = m.CreatedAt
            })
            .SingleAsync();

        return Ok(result);
    }

    [HttpGet("conversations/{conversationId:ulong}/messages")]
    public async Task<ActionResult<List<MessageSummaryDto>>> GetMessages(ulong conversationId, [FromQuery] ulong? beforeMessageId, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "会话不存在或无权限访问" });

        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ChatMessages
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
                Content = m.Content,
                Extra = m.Extra,
                ReplyToMessageId = m.ReplyToMessageId,
                IsRecalled = m.IsRecalled,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        messages.Reverse();
        return Ok(messages);
    }

    [HttpPost("conversations/{conversationId:ulong}/read")]
    public async Task<ActionResult> MarkConversationRead(ulong conversationId, ReadConversationRequest request)
    {
        var userId = GetUserId();

        var membership = await _db.ChatConversationMembers
            .SingleOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (membership == null)
            return NotFound(new { message = "会话不存在或无权限访问" });

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
                return BadRequest(new { message = "lastReadMessageId 不存在或不属于当前会话" });
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

    async Task<ConversationDetailDto?> BuildConversationDetail(ulong conversationId)
    {
        return await _db.ChatConversations
            .Where(c => c.Id == conversationId)
            .Select(c => new ConversationDetailDto
            {
                Id = c.Id,
                ConversationType = c.ConversationType,
                Title = c.Title,
                AvatarKey = c.AvatarKey,
                IsActive = c.IsActive ?? true,
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

                throw new InvalidOperationException($"无法解析用户ID：claim '{type}' 的值为 '{val}'，不是有效的 ulong。");
            }
        }

        throw new InvalidOperationException("在令牌中未找到用户ID。");
    }
}
