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
using StackExchange.Redis;

namespace api.Controllers;

/// <summary>
/// 认证相关接口，提供用户注册、登录、登出、刷新令牌、账号管理等功能。
/// 支持邮箱密码登录、账号密码登录两种方式，实现双Token单点登录机制。
/// 包含邮箱验证码发送与验证、密码修改、账号注销、个人信息更新等完整认证流程。
/// </summary>
[ApiController]
[Route("mm/[controller]")]
public class AuthController(
    DailyCheckDbContext db,
    IConfiguration config,
    IJwtTokenService jwtTokenService,
    IVerificationCodeService verificationCodeService,
    IConnectionMultiplexer redis) : ControllerBase
{
    readonly DailyCheckDbContext _db = db;
    readonly IConfiguration _config = config;
    readonly IJwtTokenService _jwtTokenService = jwtTokenService;
    readonly IVerificationCodeService _verificationCodeService = verificationCodeService;
    readonly IDatabase _redisDb = redis.GetDatabase();

    /// <summary>
    /// 用户注册接口，创建新账号并返回访问令牌和刷新令牌。
    /// 实现双Token单点登录机制，注册成功后自动登录。
    /// 需要提供有效的邮箱验证码进行身份验证。
    /// </summary>
    /// <param name="request">注册请求参数，包含邮箱、密码、昵称、账号名和验证码。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消长时间运行的操作。</param>
    /// <returns>
    /// 成功时返回200 OK，包含用户基本信息和双Token信息；
    /// 失败时返回相应的错误状态码和消息。
    /// </returns>
    /// <response code="200">注册成功，返回用户信息和认证令牌</response>
    /// <response code="400">请求参数错误，如缺少必要字段或验证码无效</response>
    /// <response code="409">邮箱或账号名已被注册</response>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("邮箱和密码不能为空");

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("需要验证码");

        var codeStatus = await _verificationCodeService.VerifyCodeAsync(normalizedEmail, request.Code, cancellationToken);

        if (codeStatus == VerifyCodeStatus.NotFoundOrExpired)
            return BadRequest("验证码已过期或错误");

        if (codeStatus == VerifyCodeStatus.CodeMismatch)
            return BadRequest("验证码不正确");

        var exists = await _db.Users.AnyAsync(x => x.Email == normalizedEmail && !x.IsDeleted, cancellationToken);
        if (exists)
            return Conflict("该邮箱已被注册");

        string? UserAccount = null;
        if (!string.IsNullOrWhiteSpace(request.UserAccount))
        {
            UserAccount = request.UserAccount.Trim();
            var UserAccountExists = await _db.Users.AnyAsync(x => x.UserAccount == UserAccount && !x.IsDeleted, cancellationToken);
            if (UserAccountExists)
                return Conflict("该用户名已被使用");
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

        // 将令牌存储到Redis
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, cancellationToken);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 用户邮箱登录接口，通过邮箱和密码进行身份验证。
    /// 成功后返回访问令牌和刷新令牌，实现双Token单点登录。
    /// 支持账号状态检查，被禁用的账号无法登录。
    /// </summary>
    /// <param name="request">登录请求参数，包含邮箱和密码。</param>
    /// <returns>
    /// 成功时返回200 OK，包含用户信息和双Token信息；
    /// 失败时返回401 Unauthorized或其他相应错误码。
    /// </returns>
    /// <response code="200">登录成功，返回用户信息和认证令牌</response>
    /// <response code="401">认证失败，邮箱或密码错误，或账号被禁用</response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted);
        if (user == null)
            return Unauthorized("邮箱或密码错误");

        if (user.Status == false)
            return Unauthorized("账号已被禁用");

        if (!PasswordHasher.Verify(user.PasswordHash, request.Password))
            return Unauthorized("邮箱或密码错误");

        var tokens = _jwtTokenService.GenerateTokens(user);

        // 将令牌存储到Redis
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, CancellationToken.None);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 用户账号登录接口，通过自定义账号名和密码进行身份验证。
    /// 成功后返回访问令牌和刷新令牌，实现双Token单点登录。
    /// 账号名需在注册时设置或后续更新，支持唯一性校验。
    /// </summary>
    /// <param name="request">账号登录请求参数，包含账号名和密码。</param>
    /// <returns>
    /// 成功时返回200 OK，包含用户信息和双Token信息；
    /// 失败时返回400或401相应错误码。
    /// </returns>
    /// <response code="200">登录成功，返回用户信息和认证令牌</response>
    /// <response code="400">账号名为空</response>
    /// <response code="401">认证失败，账号或密码错误，或账号被禁用</response>
    [HttpPost("login-account")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginByAccount(AccountLoginRequest request)
    {
        var UserAccount = request.UserAccount.Trim();
        if (string.IsNullOrWhiteSpace(UserAccount))
            return BadRequest("用户名不能为空");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.UserAccount == UserAccount && !x.IsDeleted);
        if (user == null)
            return Unauthorized("用户名或密码错误");

        if (user.Status == false)
            return Unauthorized("账号已被禁用");

        if (!PasswordHasher.Verify(user.PasswordHash, request.Password))
            return Unauthorized("邮箱或密码错误");

        var tokens = _jwtTokenService.GenerateTokens(user);

        // 将令牌存储到Redis
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, CancellationToken.None);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 刷新令牌接口，使用有效的刷新令牌获取新的访问令牌和刷新令牌。
    /// 实现双Token单点登录机制，每次刷新都会使旧令牌失效。
    /// 支持Redis存储令牌状态，确保安全性。
    /// </summary>
    /// <param name="request">刷新令牌请求参数，包含有效的刷新令牌。</param>
    /// <returns>
    /// 成功时返回200 OK，包含新的双Token信息；
    /// 失败时返回401 Unauthorized。
    /// </returns>
    /// <response code="200">刷新成功，返回新的认证令牌</response>
    /// <response code="401">刷新令牌无效、过期或用户状态异常</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request)
    {
        var principal = _jwtTokenService.ValidateRefreshToken(request.RefreshToken);
        if (principal == null)
            return Unauthorized("刷新令牌无效");

        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub == null || !ulong.TryParse(sub, out var userId))
            return Unauthorized("刷新令牌无效");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (user == null)
            return Unauthorized("用户不存在");

        if (user.Status == false)
            return Unauthorized("账号已被禁用");

        // 检查刷新令牌是否存在于Redis中
        var storedRefreshToken = await _redisDb.StringGetAsync($"refresh_token:{userId}");
        if (storedRefreshToken.IsNullOrEmpty || storedRefreshToken != request.RefreshToken)
        {
            return Unauthorized("刷新令牌无效或已过期");
        }

        var tokens = _jwtTokenService.GenerateTokens(user);

        // 更新Redis中的令牌
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, CancellationToken.None);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 发送邮箱验证码接口，支持多种业务场景的验证码发送。
    /// 根据不同的actionType参数，执行相应的邮箱状态验证。
    /// 支持频率限制，防止恶意刷取验证码。
    /// </summary>
    /// <param name="request">发送验证码请求参数，包含邮箱地址和操作类型。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消邮件发送操作。</param>
    /// <returns>
    /// 成功时返回200 OK；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">验证码发送成功</response>
    /// <response code="400">邮箱为空、操作类型不合法或邮箱状态不匹配</response>
    /// <response code="429">请求过于频繁，超出频率限制</response>
    [HttpPost("email-code")]
    [AllowAnonymous]
    public async Task<ActionResult> SendEmailCode(SendEmailCodeRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("邮箱不能为空");

        var actionTypeKey = string.IsNullOrWhiteSpace(request.ActionType)
            ? "register"
            : request.ActionType.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(x => x.Email == email && !x.IsDeleted, cancellationToken);

        if (actionTypeKey is "register" or "signup")
        {
            if (exists)
                return Conflict("该邮箱已注册");
        }
        else if (actionTypeKey is "login" or "change-password" or "deactivate" or "reset-password")
        {
            if (!exists)
                return BadRequest("该邮箱未注册");
        }
        else
        {
            return BadRequest("操作类型无效");
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
    /// 用户账号注销接口，永久删除用户账户。
    /// 需要提供有效的邮箱验证码进行二次确认。
    /// 注销后用户无法再次登录，相关数据进行软删除处理。
    /// 同时清理Redis中的用户令牌，确保立即生效。
    /// </summary>
    /// <param name="request">账号注销请求参数，包含邮箱验证码。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消注销操作。</param>
    /// <returns>
    /// 成功时返回204 No Content；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="204">账号注销成功</response>
    /// <response code="400">验证码无效或已过期</response>
    /// <response code="404">用户不存在</response>
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
            return BadRequest("验证码已过期或不正确");

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

        // 从Redis中删除用户的令牌
        await DeleteTokensFromRedis(userId);

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// 修改用户密码接口，支持通过旧密码或邮箱验证码两种方式进行身份验证。
    /// 修改成功后会清理Redis中的用户令牌，强制用户重新登录。
    /// 新密码需满足安全要求，建议包含大小写字母、数字和特殊字符。
    /// </summary>
    /// <param name="request">修改密码请求参数，包含旧密码、新密码和验证码。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消密码修改操作。</param>
    /// <returns>
    /// 成功时返回204 No Content；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="204">密码修改成功</response>
    /// <response code="400">新密码为空，或未提供有效的身份验证信息</response>
    /// <response code="401">旧密码错误或账号状态异常</response>
    /// <response code="404">用户不存在</response>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("新密码不能为空");

        var hasOldPassword = !string.IsNullOrWhiteSpace(request.OldPassword);
        var hasCode = !string.IsNullOrWhiteSpace(request.Code);
        if (!hasOldPassword && !hasCode)
            return BadRequest("请输入验证码或旧密码进行验证");

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
                return Unauthorized("旧密码输入错误");
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
                    ? BadRequest("验证码已过期或不正确")
                    : BadRequest("验证码输入错误");
            }
        }

        if (!oldPasswordVerified && !codeVerified)
            return BadRequest("请输入验证码或旧密码进行验证");

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // 从Redis中删除用户的令牌
        await DeleteTokensFromRedis(userId);

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// 更新用户个人资料接口，支持修改昵称和头像。
    /// 更新成功后会重新生成认证令牌，确保信息同步。
    /// 头像需先通过文件上传接口获取有效的AvatarKey。
    /// </summary>
    /// <param name="request">更新个人资料请求参数，包含昵称和头像Key。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消更新操作。</param>
    /// <returns>
    /// 成功时返回200 OK，包含更新后的用户信息和新的认证令牌；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">个人资料更新成功</response>
    /// <response code="400">请求参数错误</response>
    /// <response code="404">用户不存在</response>
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

        // 更新Redis中的令牌
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user, tokens));
    }
    /// <summary>
    /// 获取当前登录用户基本信息接口。
    /// 验证用户的认证状态和账号有效性。
    /// 检查Redis中存储的访问令牌是否仍然有效。
    /// </summary>
    /// <param name="cancellationToken">取消操作标记，用于取消查询操作。</param>
    /// <returns>
    /// 成功时返回200 OK，包含用户基本信息；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">获取用户信息成功</response>
    /// <response code="401">账号被禁用或会话已过期</response>
    /// <response code="404">用户不存在</response>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserBasicResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound();

        if (user.Status == false)
            return Unauthorized("账号已被禁用");

        // 检查令牌是否仍然有效
        var storedAccessToken = await _redisDb.StringGetAsync($"access_token:{userId}");
        if (storedAccessToken.IsNullOrEmpty)
        {
            return Unauthorized("会话已过期，请重新登录");
        }

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
    /// 检查账号名修改状态接口。
    /// 根据业务规则，每个用户每年只能修改一次账号名。
    /// 返回当前是否允许修改以及下次可修改的时间。
    /// </summary>
    /// <returns>
    /// 成功时返回200 OK，包含修改状态信息；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">获取修改状态成功</response>
    /// <response code="404">用户不存在</response>
    [HttpGet("account/status")]
    [Authorize]
    public async Task<ActionResult> CheckAccountUpdateStatus()
    {
        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (user == null)
            return NotFound("用户不存在");

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
    /// 更新用户账号名接口，遵循一年一次的修改频率限制。
    /// 新账号名需满足唯一性要求，不能与其他用户冲突。
    /// 更新成功后会重新生成认证令牌。
    /// </summary>
    /// <param name="request">更新账号名请求参数，包含新的账号名。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消更新操作。</param>
    /// <returns>
    /// 成功时返回200 OK，包含更新后的用户信息和新的认证令牌；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">账号名更新成功</response>
    /// <response code="400">账号名为空</response>
    /// <response code="403">修改频率超限，未满一年</response>
    /// <response code="409">账号名已被占用</response>
    [HttpPost("account")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> UpdateUserAccount(UpdateUserAccountRequest request, CancellationToken cancellationToken)
    {
        var newUserAccount = request.UserAccount.Trim();
        if (string.IsNullOrWhiteSpace(newUserAccount))
            return BadRequest("用户名不能为空");

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
                    message = "用户名每年只能修改一次",
                    nextUpdateAt = nextAllowedDate
                });
            }
        }

        // Check uniqueness
        var exists = await _db.Users.AnyAsync(x => x.UserAccount == newUserAccount && x.Id != userId && !x.IsDeleted, cancellationToken);
        if (exists)
            return Conflict("该用户名已被使用");

        user.UserAccount = newUserAccount;
        user.AccountUpdatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var tokens = _jwtTokenService.GenerateTokens(user);

        // 更新Redis中的令牌
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, cancellationToken);

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
    /// 验证账号名可用性接口。
    /// 检查指定账号名是否已被其他用户占用。
    /// 用于注册或修改账号名时的前置验证。
    /// </summary>
    /// <param name="UserAccount">待验证的账号名。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消验证操作。</param>
    /// <returns>
    /// 成功时返回200 OK表示账号名可用，或409 Conflict表示已被占用；
    /// 参数错误时返回400 Bad Request。
    /// </returns>
    /// <response code="200">账号名可用</response>
    /// <response code="400">账号名为空</response>
    /// <response code="409">账号名已被占用</response>
    [HttpPost]
    [AllowAnonymous]
    [Route("validate-account")]
    public async Task<ActionResult> ValidateAccount(string UserAccount, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(UserAccount))
            return BadRequest("用户名不能为空");
        var exists = await _db.Users.AnyAsync(x => x.UserAccount == UserAccount.Trim() && !x.IsDeleted, cancellationToken);
        if (exists)
            return Conflict("该用户名已被使用");
        return Ok();
    }

    ulong GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var subClaim = User.FindFirstValue("sub");
        var value = idClaim ?? subClaim;
        return value == null ? throw new InvalidOperationException("User id not found in token") : ulong.Parse(value);
    }

    /// <summary>
    /// 将访问令牌和刷新令牌存储到Redis
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消操作标记</param>
    private async Task StoreTokensInRedis(ulong userId, string accessToken, string refreshToken, CancellationToken cancellationToken)
    {
        // 存储访问令牌，设置过期时间 从appsettings获取更合理的过期时间
        var accessTokenExpiry = _config.GetValue<int>("jwt:AccessTokenMinutes", 1); // 默认值30
        await _redisDb.StringSetAsync($"access_token:{userId}", accessToken, TimeSpan.FromMinutes(accessTokenExpiry));

        // 存储刷新令牌，设置过期时间
        var refreshTokenExpiry = _config.GetValue<int>("jwt:RefreshTokenDays", 7); // 默认值7
        await _redisDb.StringSetAsync($"refresh_token:{userId}", refreshToken, TimeSpan.FromDays(refreshTokenExpiry));
    }

    /// <summary>
    /// 从Redis删除用户的令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    private async Task DeleteTokensFromRedis(ulong userId)
    {
        await _redisDb.KeyDeleteAsync($"access_token:{userId}");
        await _redisDb.KeyDeleteAsync($"refresh_token:{userId}");
    }
}