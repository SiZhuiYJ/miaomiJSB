using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using api.Data;
using api.Infrastructure;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

/// <summary>
/// 认证相关接口，包括注册、登录和刷新令牌。
/// </summary>
[ApiController]
[Route("mm/[controller]")]
public class AuthController(DailyCheckDbContext db, IJwtTokenService jwtTokenService, IVerificationCodeService verificationCodeService) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;
    readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    readonly IVerificationCodeService _verificationCodeService = verificationCodeService;

    /// <summary>
    /// 用户注册，创建新账号并返回访问令牌和刷新令牌。
    /// </summary>
    /// <param name="request">注册请求参数。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns>包含用户信息、访问令牌及刷新令牌的响应。</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("需要电子邮件和密码");

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("需要验证码");

        var codeStatus = await _verificationCodeService.VerifyCodeAsync(normalizedEmail, request.Code, cancellationToken);

        if (codeStatus == VerifyCodeStatus.NotFoundOrExpired)
            return BadRequest("验证码已过期或错误");

        if (codeStatus == VerifyCodeStatus.CodeMismatch)
            return BadRequest("验证码不正确");

        var exists = await _db.Users.AnyAsync(x => x.Email == normalizedEmail && !x.IsDeleted, cancellationToken);
        if (exists)
            return Conflict("Email already exists");

        string? UserAccount = null;
        if (!string.IsNullOrWhiteSpace(request.UserAccount))
        {
            UserAccount = request.UserAccount.Trim();
            var UserAccountExists = await _db.Users.AnyAsync(x => x.UserAccount == UserAccount && !x.IsDeleted, cancellationToken);
            if (UserAccountExists)
                return Conflict("UserAccount already exists");
        }

        var user = new User
        {
            UserAccount = UserAccount ?? string.Empty,
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
            NickName = request.NickName,
            Status = true,
            Role = "user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var tokens = _jwtTokenService.GenerateTokens(user);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 用户登录，校验邮箱和密码并返回访问令牌和刷新令牌。
    /// </summary>
    /// <param name="request">登录请求参数。</param>
    /// <returns>包含用户信息、访问令牌及刷新令牌的响应。</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);
        if (user == null)
            return Unauthorized("Invalid credentials");

        if (user.Status == false)
            return Unauthorized("Account is disabled");

        if (!PasswordHasher.Verify(user.PasswordHash, request.Password))
            return Unauthorized("Invalid credentials");

        var tokens = _jwtTokenService.GenerateTokens(user);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 用户登录，校验账号和密码并返回访问令牌和刷新令牌。
    /// </summary>
    /// <param name="request">登录请求参数。</param>
    /// <returns>包含用户信息、访问令牌及刷新令牌的响应。</returns>
    [HttpPost("login-account")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginByAccount(AccountLoginRequest request)
    {
        var UserAccount = request.UserAccount.Trim();
        if (string.IsNullOrWhiteSpace(UserAccount))
            return BadRequest("UserAccount is required");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.UserAccount == UserAccount && !x.IsDeleted);
        if (user == null)
            return Unauthorized("Invalid credentials");

        if (user.Status == false)
            return Unauthorized("Account is disabled");

        if (!PasswordHasher.Verify(user.PasswordHash, request.Password))
            return Unauthorized("Invalid credentials");

        var tokens = _jwtTokenService.GenerateTokens(user);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 使用刷新令牌获取新的访问令牌和刷新令牌，实现双 token 单点登录。
    /// </summary>
    /// <param name="request">包含刷新令牌的请求。</param>
    /// <returns>新的访问令牌、刷新令牌及过期时间。</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request)
    {
        var principal = _jwtTokenService.ValidateRefreshToken(request.RefreshToken);
        if (principal == null)
            return Unauthorized("Invalid refresh token");

        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub == null || !ulong.TryParse(sub, out var userId))
            return Unauthorized("Invalid refresh token");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (user == null)
            return Unauthorized("User not found");

        if (user.Status == false)
            return Unauthorized("Account is disabled");

        var tokens = _jwtTokenService.GenerateTokens(user);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 发送注册或登录使用的邮箱验证码。
    /// </summary>
    /// <param name="request">包含目标邮箱地址的请求参数。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns>发送结果。</returns>
    [HttpPost("email-code")]
    [AllowAnonymous]
    public async Task<ActionResult> SendEmailCode(SendEmailCodeRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email is required");

        var actionTypeKey = string.IsNullOrWhiteSpace(request.ActionType)
            ? "register"
            : request.ActionType.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(x => x.Email == email && !x.IsDeleted, cancellationToken);

        if (actionTypeKey is "register" or "signup")
        {
            if (exists)
                return Conflict("Email already registered");
        }
        else if (actionTypeKey is "login" or "change-password" or "deactivate" or "reset-password")
        {
            if (!exists)
                return BadRequest("Email not registered");
        }
        else
        {
            return BadRequest("Invalid action type");
        }

        var actionLabel = actionTypeKey switch
        {
            "register" => "注册",
            "signup" => "注册",
            "login" => "登录",
            "change-password" => "修改密码",
            "deactivate" => "注销账号",
            "reset-password" => "重置密码",
            _ => "验证"
        };

        var status = await _verificationCodeService.SendCodeAsync(email, actionLabel, cancellationToken);

        return status switch
        {
            SendCodeStatus.Success => Ok(),
            SendCodeStatus.TooFrequent => StatusCode(StatusCodes.Status429TooManyRequests, "Too many requests, please try again later"),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
    /// <summary>
    /// 用户自行注销账号。
    /// </summary>
    /// <param name="request">注销请求参数。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns>注销结果。</returns>
    [HttpPost("deactivate")]
    [Authorize]
    public async Task<ActionResult> DeactivateAccount(DeactivateAccountRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("需要验证码");

        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound();

        var codeStatus = await _verificationCodeService.VerifyCodeAsync(user.Email, request.Code, cancellationToken);
        if (codeStatus == VerifyCodeStatus.NotFoundOrExpired)
            return BadRequest("验证码已过期或错误");

        if (codeStatus == VerifyCodeStatus.CodeMismatch)
            return BadRequest("验证码不正确");

        user.IsDeleted = true;
        user.Status = false;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var log = new SoftDeleteLog
        {
            TableName = "users",
            RecordId = user.Id,
            DeleterUserId = user.Id,
            Reason = "用户自行注销账号",
            DeletedAt = DateTime.UtcNow
        };

        _db.SoftDeleteLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// 修改当前登录用户的密码。
    /// </summary>
    /// <param name="request">修改密码请求参数。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns>修改结果。</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("需要新密码");

        var hasOldPassword = !string.IsNullOrWhiteSpace(request.OldPassword);
        var hasCode = !string.IsNullOrWhiteSpace(request.Code);
        if (!hasOldPassword && !hasCode)
            return BadRequest("需要验证码或旧密码");

        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound();

        var oldPasswordVerified = false;
        if (hasOldPassword)
        {
            if (PasswordHasher.Verify(user.PasswordHash, request.OldPassword))
            {
                oldPasswordVerified = true;
            }
            else if (!hasCode)
            {
                return Unauthorized("旧密码错误");
            }
        }

        var codeVerified = false;
        if (hasCode)
        {
            var codeStatus = await _verificationCodeService.VerifyCodeAsync(user.Email, request.Code, cancellationToken);
            if (codeStatus == VerifyCodeStatus.Success)
            {
                codeVerified = true;
            }
            else if (!oldPasswordVerified)
            {
                return codeStatus == VerifyCodeStatus.NotFoundOrExpired
                    ? BadRequest("验证码已过期或错误")
                    : BadRequest("验证码不正确");
            }
        }

        if (!oldPasswordVerified && !codeVerified)
            return BadRequest("需要验证码或旧密码");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// 更新当前登录用户的个人资料（只能更新昵称和头像）。
    /// </summary>
    /// <param name="request">更新个人资料的请求参数。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns>更新结果。</returns>
    [HttpPost("profile")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> UpdateProfile(UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound();

        if (request.NickName != null)
            user.NickName = request.NickName;

        if (request.AvatarKey != null)
            user.AvatarKey = request.AvatarKey;

        user.UpdatedAt = DateTime.UtcNow;

        var tokens = _jwtTokenService.GenerateTokens(user);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user, tokens));
    }
    /// <summary>
    /// 获取当前登录用户的基本信息。
    /// </summary>
    /// <returns>当前用户的基本信息。</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserBasicResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound();

        if (user.Status == false)
            return Unauthorized("Account is disabled");

        return Ok(new UserBasicResponse
        {
            UserId = (long)user.Id,
            Email = user.Email,
            NickName = user.NickName,
            UserAccount = user.UserAccount,
            AvatarKey = user.AvatarKey
        });
    }

    /// <summary>
    /// 检查当前用户是否可以修改账号名。
    /// 规则：一年内只能修改一次。
    /// </summary>
    /// <returns>包含是否可修改及下次可修改时间的响应。</returns>
    [HttpGet("account/status")]
    [Authorize]
    public async Task<ActionResult> CheckAccountUpdateStatus()
    {
        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (user == null)
            return NotFound();

        var canUpdate = true;
        DateTime? nextUpdateAt = null;

        if (user.AccountUpdatedAt.HasValue)
        {
            var nextAllowedDate = user.AccountUpdatedAt.Value.AddYears(1);
            if (DateTime.UtcNow < nextAllowedDate)
            {
                canUpdate = false;
                nextUpdateAt = nextAllowedDate;
            }
        }

        return Ok(new
        {
            canUpdate,
            nextUpdateAt
        });
    }

    /// <summary>
    /// 更新用户账号名（一年一次）。
    /// </summary>
    /// <param name="request">包含新账号名的请求。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns>更新结果。</returns>
    [HttpPost("account")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> UpdateUserAccount(UpdateUserAccountRequest request, CancellationToken cancellationToken)
    {
        var newUserAccount = request.UserAccount.Trim();
        if (string.IsNullOrWhiteSpace(newUserAccount))
            return BadRequest("UserAccount is required");

        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound();

        // Check if update is allowed
        if (user.AccountUpdatedAt.HasValue)
        {
            var nextAllowedDate = user.AccountUpdatedAt.Value.AddYears(1);
            if (DateTime.UtcNow < nextAllowedDate)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "UserAccount can only be updated once a year",
                    nextUpdateAt = nextAllowedDate
                });
            }
        }

        // Check uniqueness
        var exists = await _db.Users.AnyAsync(x => x.UserAccount == newUserAccount && x.Id != userId && !x.IsDeleted, cancellationToken);
        if (exists)
            return Conflict("UserAccount already exists");

        user.UserAccount = newUserAccount;
        user.AccountUpdatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var tokens = _jwtTokenService.GenerateTokens(user);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 根据用户信息和令牌对构建认证响应模型。
    /// </summary>
    /// <param name="user">当前用户实体。</param>
    /// <param name="tokens">访问令牌和刷新令牌信息。</param>
    /// <returns>认证响应 DTO。</returns>
    static AuthResponse CreateAuthResponse(User user, TokenPair tokens)
    {
        return new AuthResponse
        {
            UserId = (long)user.Id,
            Email = user.Email,
            NickName = user.NickName,
            UserAccount = user.UserAccount,
            AvatarKey = user.AvatarKey,
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt
        };
    }

    /// <summary>
    /// 验证用户名是否已存在。
    /// </summary>
    /// <param name="UserAccount">用户名</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    [Route("validate-account")]
    public async Task<ActionResult> ValidateAccount(string UserAccount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UserAccount))
            return BadRequest("UserAccount is required");
        var exists = await _db.Users.AnyAsync(x => x.UserAccount == UserAccount.Trim() && !x.IsDeleted, cancellationToken);
        if (exists)
            return Conflict("UserAccount already exists");
        return Ok();
    }

    ulong GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var subClaim = User.FindFirstValue("sub");
        var value = idClaim ?? subClaim;
        return value == null ? throw new InvalidOperationException("User id not found in token") : ulong.Parse(value);
    }
}
