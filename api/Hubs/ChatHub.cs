
using System.Security.Claims;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace api.Hubs;

/// <summary>
/// 聊天Hub，处理实时通信功能
/// </summary>
[Authorize]
public class ChatHub(DailyCheckDbContext db) : Hub
{
    readonly DailyCheckDbContext _db = db;

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(GetUserId()));
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 订阅指定的会话以接收实时消息
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <exception cref="HubException">当用户不是会话成员时抛出</exception>
    public async Task SubscribeConversation(ulong conversationId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            throw new HubException("会话不存在或无权限订阅");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    /// <summary>
    /// 取消订阅指定的会话
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    public async Task UnsubscribeConversation(ulong conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    /// <summary>
    /// 标记消息为已读并广播给发送者
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <param name="messageId">消息ID</param>
    public async Task MarkMessageRead(ulong conversationId, ulong messageId)
    {
        var userId = GetUserId();

        // 验证权限
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId
                        && m.UserId == userId
                        && m.LeftAt == null);
        if (!isMember)
            throw new HubException("无权限操作");

        // 验证消息存在
        var message = await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId);
        if (message == null || message.SenderUserId == userId)
            return; // 不处理自己发送的消息

        // 检查是否已有回执
        var existing = await _db.ChatMessageReceipts
            .AnyAsync(r => r.MessageId == messageId && r.UserId == userId);
        if (existing)
            return;

        // 创建回执记录
        var receipt = new ChatMessageReceipt
        {
            MessageId = messageId,
            UserId = userId,
            ReadAt = DateTime.UtcNow
        };
        _db.ChatMessageReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        // 更新会话成员的 LastReadMessageId（如果是最新消息）
        var membership = await _db.ChatConversationMembers
            .FirstOrDefaultAsync(m => m.ConversationId == conversationId && m.UserId == userId);
        if (membership != null && messageId > (membership.LastReadMessageId ?? 0))
        {
            membership.LastReadMessageId = messageId;
            membership.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // 广播给会话所有成员：更新已读状态
        await Clients.Group(ChatHub.GroupName(conversationId)).SendAsync("chat:message-read", new
        {
            messageId,
            conversationId,
            readByUserId = userId,
            readAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取消息的已读详情
    /// </summary>
    /// <param name="messageId">消息ID</param>
    /// <returns>消息已读详情</returns>
    public async Task<MessageReadDetailDto> GetMessageReadDetail(ulong messageId)
    {
        var userId = GetUserId();

        var message = await _db.ChatMessages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId);
        if (message == null)
            throw new HubException("消息不存在");

        // 验证权限
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == message.ConversationId
                        && m.UserId == userId
                        && m.LeftAt == null);
        if (!isMember)
            throw new HubException("无权限查看");

        // 查询已读用户列表
        var readUsers = await _db.ChatMessageReceipts
            .Where(r => r.MessageId == messageId)
            .Join(_db.Users,
                  r => r.UserId,
                  u => u.Id,
                  (r, u) => new ReadUserDto
                  {
                      UserId = u.Id,
                      NickName = u.NickName,
                      AvatarKey = u.AvatarKey,
                      ReadAt = r.ReadAt
                  })
            .ToListAsync();

        return new MessageReadDetailDto
        {
            MessageId = messageId,
            TotalRecipients = await _db.ChatConversationMembers
                .CountAsync(m => m.ConversationId == message.ConversationId
                              && m.UserId != message.SenderUserId
                              && m.LeftAt == null),
            ReadCount = readUsers.Count,
            ReadUsers = readUsers
        };
    }

    /// <summary>
    /// 生成会话群组名称
    /// </summary>
    /// <param name="conversationId">会话ID</param>
    /// <returns>群组名称</returns>
    public static string GroupName(ulong conversationId) => $"conversation-{conversationId}";

    public static string UserGroupName(ulong userId) => $"user-{userId}";

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="HubException">当无法从令牌中获取用户ID时抛出</exception>
    ulong GetUserId()
    {
        var candidateTypes = new[] { ClaimTypes.NameIdentifier, "sub", "nameid", "user_id", "id" };

        foreach (var type in candidateTypes)
        {
            var val = Context.User?.FindFirstValue(type);
            if (!string.IsNullOrEmpty(val))
            {
                if (ulong.TryParse(val, out var id))
                    return id;

                throw new HubException($"无法解析用户ID：claim '{type}' 的值为 '{val}'，不是有效的 ulong。");
            }
        }

        throw new HubException("在令牌中未找到用户ID。");
    }
}
