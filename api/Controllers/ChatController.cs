
using api.Data;
using api.Hubs;
using api.Models;
using Microsoft.AspNetCore.Authorization;
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
/// 聊天控制器，处理会话创建、消息发送、读取等相关功能
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
    /// 创建一个新的聊天会话
    /// </summary>
    /// <param name="request">创建会话的请求参数</param>
    /// <returns>创建成功的会话详情</returns>
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
    /// 获取当前用户的会话列表
    /// </summary>
    /// <returns>会话摘要列表</returns>
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
    /// 更新会话信息
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="request">更新会话的请求参数</param>
    /// <returns>更新后的会话详情</returns>
    [HttpPost("conversations/{conversationId:ulong}")]
    public async Task<ActionResult<ConversationDetailDto>> UpdateConversation(ulong conversationId, UpdateConversationRequest request)
    {
        var userId = GetUserId();
        var conversation = await _db.ChatConversations
            .Include(c => c.ChatConversationMembers)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive == true);

        if (conversation == null)
        {
            return NotFound(new { message = "会话不存在" });
        }
        if (!conversation.ChatConversationMembers.Any(m => m.UserId == userId && m.LeftAt == null))
        {
            return NotFound(new { message = "会话不存在或无权限访问" });
        }
        // 更新会话信息到数据库
        var chatConversationMember = conversation.ChatConversationMembers
            .FirstOrDefault(m => m.UserId == userId && m.LeftAt == null);
        if (chatConversationMember == null)
        {
            return NotFound(new { message = "会话不存在" });
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
        return detail == null ? NotFound(new { message = "会话不存在" }) : Ok(detail);
    }

    /// <summary>
    /// 根据ID获取会话详情
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <returns>会话详情</returns>
    [HttpGet("conversations/{conversationId:ulong}")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversationById(ulong conversationId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);

        if (!isMember)
            return NotFound(new { message = "会话不存在或无权限访问" });

        var detail = await BuildConversationDetail(conversationId, userId);
        return detail == null ? NotFound(new { message = "会话不存在" }) : Ok(detail);
    }

    /// <summary>
    /// 在指定会话中发送消息
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="request">发送消息的请求参数</param>
    /// <returns>发送的消息详情</returns>
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
            return BadRequest(new { message = "messageType 仅支持 text/image/video/audio/file/system" });

        if (messageType == "text" && string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { message = "文本消息 content 不能为空" });

        // 文件类消息需要extra字段包含文件信息
        if ((messageType == "image" || messageType == "video" || messageType == "audio" || messageType == "file")
            && request.Extra == null)
            return BadRequest(new { message = $"{messageType} 消息 extra 不能为空，需包含文件信息" });

        if (request.ReplyToMessageId.HasValue)
        {
            var replyExists = await _db.ChatMessages.AnyAsync(x => x.Id == request.ReplyToMessageId.Value && x.ConversationId == conversationId);
            if (!replyExists)
                return BadRequest(new { message = "replyToMessageId 不存在或不属于当前会话" });
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
    /// 撤回消息
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <returns>撤回后的消息摘要</returns>
    [HttpPost("messages/{messageId:ulong}/recall")]
    public async Task<ActionResult<MessageSummaryDto>> RecallMessage(ulong messageId)
    {
        const int recallWindowMinutes = 5;
        var userId = GetUserId();

        var message = await _db.ChatMessages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
            return NotFound(new { message = "消息不存在" });

        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == message.ConversationId && m.UserId == userId && m.LeftAt == null);

        if (!isMember || message.Conversation.IsActive != true)
            return NotFound(new { message = "会话不存在或无权限访问" });

        if (message.SenderUserId != userId)
            return BadRequest(new { message = "只能撤回自己发送的消息" });

        if (message.IsRecalled)
            return Ok(await BuildMessageSummary(message.Id));

        var now = DateTime.UtcNow;
        if (message.CreatedAt.AddMinutes(recallWindowMinutes) < now)
            return BadRequest(new { message = $"消息发送超过{recallWindowMinutes}分钟，无法撤回" });

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
    /// 获取会话中的消息列表
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="beforeMessageId">获取此消息ID之前的消息</param>
    /// <param name="pageSize">每页大小，默认为20</param>
    /// <returns>消息摘要列表</returns>
    [HttpGet("conversations/{conversationId:ulong}/messages")]
    public async Task<ActionResult<List<MessageSummaryDto>>> GetMessages(ulong conversationId, [FromQuery] ulong? beforeMessageId, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "会话不存在或无权限访问" });

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
    /// 获取会话中的消息增量
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="afterMessageId">获取此消息ID之后的消息</param>
    /// <param name="pageSize">每页大小，默认为50</param>
    /// <returns>消息增量数据</returns>
    [HttpGet("conversations/{conversationId:ulong}/messages/delta")]
    public async Task<ActionResult<MessageDeltaDto>> GetMessageDelta(ulong conversationId, [FromQuery] ulong? afterMessageId, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AsNoTracking()
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "会话不存在或无权限访问" });

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
    /// 标记会话为已读
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="request">标记已读的请求参数</param>
    /// <returns>操作结果</returns>
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

    /// <summary>
    /// 获取消息的已读详情
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <returns>消息已读详情</returns>
    [HttpGet("messages/{messageId:ulong}/read-status")]
    public async Task<ActionResult<MessageReadDetailDto>> GetMessageReadStatus(ulong messageId)
    {
        var userId = GetUserId();

        var message = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null)
            return NotFound(new { message = "消息不存在" });

        // 验证权限
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == message.ConversationId
                        && m.UserId == userId
                        && m.LeftAt == null);
        if (!isMember)
            return NotFound(new { message = "无权限查看" });

        // 查询已读用户
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

        // 计算总接收者数（排除发送者）
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
    /// 构建会话摘要投影
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>会话摘要投影表达式</returns>
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
    /// 构建会话详情
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="userId">用户ID</param>
    /// <returns>会话详情</returns>
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
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID</returns>
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
