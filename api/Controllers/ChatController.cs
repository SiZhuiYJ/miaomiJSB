using System.Security.Claims;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("mm/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private static readonly Dictionary<long, List<ChatMessageDto>> ConversationStore = new();
    private static readonly object ConversationLock = new();

    [HttpGet("messages")]
    public ActionResult<List<ChatMessageDto>> GetMessages()
    {
        var userId = GetUserId();
        lock (ConversationLock)
        {
            if (!ConversationStore.TryGetValue(userId, out var history))
            {
                return Ok(new List<ChatMessageDto>());
            }

            return Ok(history.ToList());
        }
    }

    [HttpPost("messages")]
    public ActionResult<SendChatMessageResponse> SendMessage(SendChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "消息内容不能为空" });
        }

        var userId = GetUserId();
        var userMessage = new ChatMessageDto
        {
            Role = "user",
            Content = request.Content.Trim(),
            SentAt = DateTime.UtcNow
        };

        var assistantReply = new ChatMessageDto
        {
            Role = "assistant",
            Content = $"你刚刚说的是：{userMessage.Content}",
            SentAt = DateTime.UtcNow
        };

        lock (ConversationLock)
        {
            if (!ConversationStore.TryGetValue(userId, out var history))
            {
                history = new List<ChatMessageDto>();
                ConversationStore[userId] = history;
            }

            history.Add(userMessage);
            history.Add(assistantReply);

            return Ok(new SendChatMessageResponse
            {
                Reply = assistantReply,
                History = history.ToList()
            });
        }
    }

    private long GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("无效的用户身份");
        }

        return userId;
    }
}
