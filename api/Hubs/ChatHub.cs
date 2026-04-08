using System.Security.Claims;
using api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace api.Hubs;

[Authorize]
public class ChatHub(DailyCheckDbContext db) : Hub
{
    readonly DailyCheckDbContext _db = db;

    public async Task SubscribeConversation(ulong conversationId)
    {
        var userId = GetUserId();
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId && m.UserId == userId && m.LeftAt == null);
        if (!isMember)
            throw new HubException("会话不存在或无权限订阅");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task UnsubscribeConversation(ulong conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public static string GroupName(ulong conversationId) => $"conversation-{conversationId}";

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
