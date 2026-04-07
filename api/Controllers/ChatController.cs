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
public class ChatController : ControllerBase
{
    private readonly DailyCheckDbContext _dbContext;

    public ChatController(DailyCheckDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDto>> StartConversation(StartConversationRequest request)
    {
        var userId = GetUserId();
        if (request.TargetUserId == 0 || request.TargetUserId == userId)
        {
            return BadRequest(new { message = "目标用户无效" });
        }

        var targetUser = await _dbContext.Users
            .Where(u => u.Id == request.TargetUserId && !u.IsDeleted)
            .Select(u => new { u.Id, Name = u.NickName ?? u.UserAccount, u.AvatarKey })
            .FirstOrDefaultAsync();
        if (targetUser is null)
        {
            return NotFound(new { message = "目标用户不存在" });
        }

        var existingSession = await FindExistingSession(userId, request.TargetUserId);
        if (existingSession is null)
        {
            var now = DateTime.UtcNow;
            var session = new ChatSession
            {
                SessionNo = $"S{Guid.NewGuid():N}",
                SessionType = 1,
                CreatedByUserId = userId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.ChatSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChatSessionMembers.AddRange(
                new ChatSessionMember
                {
                    SessionId = session.Id,
                    UserId = userId,
                    Role = 1,
                    JoinedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ChatSessionMember
                {
                    SessionId = session.Id,
                    UserId = request.TargetUserId,
                    Role = 1,
                    JoinedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            );
            await _dbContext.SaveChangesAsync();
            existingSession = session;
        }

        var myMember = await _dbContext.ChatSessionMembers
            .Where(m => m.SessionId == existingSession.Id && m.UserId == userId)
            .Select(m => m.UnreadCount)
            .FirstOrDefaultAsync();

        return Ok(new ConversationDto
        {
            SessionId = existingSession.Id,
            SessionNo = existingSession.SessionNo,
            TargetUserId = targetUser.Id,
            TargetUserName = targetUser.Name,
            TargetAvatarKey = targetUser.AvatarKey,
            LastMessage = string.Empty,
            LastMessageAt = existingSession.LastMessageAt,
            UnreadCount = myMember
        });
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations()
    {
        var userId = GetUserId();

        var sessions = await _dbContext.ChatSessionMembers
            .Where(m => m.UserId == userId && !m.IsDeleted)
            .Join(
                _dbContext.ChatSessions.Where(s => !s.IsDeleted),
                m => m.SessionId,
                s => s.Id,
                (m, s) => new { Member = m, Session = s }
            )
            .OrderByDescending(x => x.Session.LastMessageAt)
            .Select(x => new
            {
                x.Session.Id,
                x.Session.SessionNo,
                x.Session.LastMessageAt,
                x.Member.UnreadCount
            })
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.Id).ToList();
        if (sessionIds.Count == 0)
        {
            return Ok(new List<ConversationDto>());
        }

        var peerMap = await _dbContext.ChatSessionMembers
            .Where(m => sessionIds.Contains(m.SessionId) && m.UserId != userId && !m.IsDeleted)
            .Join(_dbContext.Users.Where(u => !u.IsDeleted), m => m.UserId, u => u.Id,
                (m, u) => new { m.SessionId, u.Id, Name = u.NickName ?? u.UserAccount, u.AvatarKey })
            .ToListAsync();

        var lastMessageMap = await _dbContext.ChatMessages
            .Where(m => sessionIds.Contains(m.SessionId) && !m.IsDeleted)
            .GroupBy(m => m.SessionId)
            .Select(g => g.OrderByDescending(x => x.Id).Select(x => new { x.SessionId, x.Content }).First())
            .ToListAsync();

        var peerLookup = peerMap.ToDictionary(x => x.SessionId, x => x);
        var lastLookup = lastMessageMap.ToDictionary(x => x.SessionId, x => x.Content);

        var result = sessions
            .Where(s => peerLookup.ContainsKey(s.Id))
            .Select(s =>
            {
                var peer = peerLookup[s.Id];
                return new ConversationDto
                {
                    SessionId = s.Id,
                    SessionNo = s.SessionNo,
                    TargetUserId = peer.Id,
                    TargetUserName = peer.Name,
                    TargetAvatarKey = peer.AvatarKey,
                    LastMessage = lastLookup.GetValueOrDefault(s.Id) ?? string.Empty,
                    LastMessageAt = s.LastMessageAt,
                    UnreadCount = s.UnreadCount
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpGet("conversations/{sessionId:ulong}/messages")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetMessages(ulong sessionId, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (!await IsSessionMember(sessionId, userId))
        {
            return Forbid();
        }

        pageSize = Math.Clamp(pageSize, 1, 100);

        var messages = await _dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId && !m.IsDeleted)
            .OrderByDescending(m => m.Id)
            .Take(pageSize)
            .OrderBy(m => m.Id)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SessionId = m.SessionId,
                SenderUserId = m.SenderUserId,
                Content = m.Content,
                SentAt = m.CreatedAt
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("conversations/{sessionId:ulong}/messages")]
    public async Task<ActionResult<SendChatMessageResponse>> SendMessage(ulong sessionId, SendChatMessageRequest request)
    {
        var userId = GetUserId();
        if (!await IsSessionMember(sessionId, userId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "消息内容不能为空" });
        }

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            SessionId = sessionId,
            SenderUserId = userId,
            MessageType = 1,
            Content = request.Content.Trim(),
            ClientMsgNo = request.ClientMsgNo,
            SendStatus = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync();

        var session = await _dbContext.ChatSessions.FirstAsync(s => s.Id == sessionId);
        session.LastMessageId = message.Id;
        session.LastMessageAt = message.CreatedAt;
        session.UpdatedAt = now;

        var otherMember = await _dbContext.ChatSessionMembers
            .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId != userId && !m.IsDeleted);
        if (otherMember is not null)
        {
            otherMember.UnreadCount += 1;
            otherMember.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new SendChatMessageResponse
        {
            Message = new ChatMessageDto
            {
                Id = message.Id,
                SessionId = message.SessionId,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                SentAt = message.CreatedAt
            }
        });
    }

    [HttpPost("conversations/{sessionId:ulong}/read")]
    public async Task<ActionResult> MarkConversationRead(ulong sessionId, MarkAsReadRequest request)
    {
        var userId = GetUserId();
        var member = await _dbContext.ChatSessionMembers
            .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId && !m.IsDeleted);
        if (member is null)
        {
            return Forbid();
        }

        member.LastReadMessageId = request.LastReadMessageId;
        member.LastReadAt = DateTime.UtcNow;
        member.UnreadCount = 0;
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "已标记已读" });
    }

    private async Task<ChatSession?> FindExistingSession(ulong userId, ulong targetUserId)
    {
        var sessionId = await _dbContext.ChatSessionMembers
            .Where(m => !m.IsDeleted && (m.UserId == userId || m.UserId == targetUserId))
            .GroupBy(m => m.SessionId)
            .Where(g => g.Select(x => x.UserId).Distinct().Count() == 2
                        && g.Any(x => x.UserId == userId)
                        && g.Any(x => x.UserId == targetUserId))
            .Select(g => g.Key)
            .FirstOrDefaultAsync();

        if (sessionId == 0)
        {
            return null;
        }

        return await _dbContext.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted);
    }

    private async Task<bool> IsSessionMember(ulong sessionId, ulong userId)
    {
        return await _dbContext.ChatSessionMembers
            .AnyAsync(m => m.SessionId == sessionId && m.UserId == userId && !m.IsDeleted);
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
}
