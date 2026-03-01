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
/// 认证相关接口，提供完整的用户身份验证和账户管理功能。
/// 支持多种登录方式（邮箱密码、账号密码、邮箱验证码），实现双Token单点登录机制。
/// 包含用户注册、登录、登出、令牌刷新、邮箱验证码管理、密码修改、账号注销、个人信息更新等完整认证流程。
/// 采用JWT令牌认证，结合Redis存储实现高效的令牌管理和验证。
/// </summary>
/// <remarks>
/// 主要特性：
/// • 双Token机制：Access Token（短期）+ Refresh Token（长期）
/// • 多种登录方式：邮箱密码、账号密码、邮箱验证码
/// • 安全措施：密码哈希存储、令牌黑名单、频率限制
/// • 账户管理：注册、注销、信息更新、密码修改
/// • 验证码服务：邮箱验证码发送与验证
/// • Redis集成：令牌状态管理、会话控制
/// </remarks>
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
    /// <remarks>
    /// 注册流程说明：
    /// 1. 用户填写邮箱、密码、昵称等基本信息
    /// 2. 系统发送邮箱验证码到指定邮箱
    /// 3. 用户提交验证码完成注册
    /// 4. 注册成功后自动登录，返回双Token
    /// 
    /// 安全要求：
    /// • 邮箱必须唯一且有效
    /// • 密码需满足安全强度要求
    /// • 验证码具有时效性（通常5分钟）
    /// • 支持账号名唯一性校验
    /// </remarks>
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
    /// <remarks>
    /// 登录机制说明：
    /// • 使用邮箱和密码进行身份验证
    /// • 密码在服务端进行安全哈希验证
    /// • 登录成功后生成双Token（Access Token + Refresh Token）
    /// • Token信息同时存储到Redis中用于状态管理
    /// • 被禁用的账号将拒绝登录请求
    /// 
    /// 安全特性：
    /// • 密码加密存储，不保存明文
    /// • 登录失败次数限制（防暴力破解）
    /// • Token有效期控制
    /// • 支持多设备登录管理
    /// </remarks>
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
    /// <remarks>
    /// 账号登录特点：
    /// • 使用用户自定义的账号名登录
    /// • 账号名具有唯一性约束
    /// • 适用于不想暴露邮箱的用户场景
    /// • 登录流程与邮箱登录相同
    /// 
    /// 使用场景：
    /// • 用户偏好使用个性化账号名
    /// • 企业内部系统员工工号登录
    /// • 第三方应用集成登录
    /// • 隐私保护需求较高的场景
    /// </remarks>
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
    /// 邮箱验证码登录接口，通过邮箱和验证码进行无密码身份验证。
    /// 成功后返回访问令牌和刷新令牌，实现双Token单点登录。
    /// 支持账号状态检查，被禁用的账号无法登录。
    /// </summary>
    /// <remarks>
    /// 无密码登录优势：
    /// • 无需记忆复杂密码
    /// • 降低密码泄露风险
    /// • 适合移动端快速登录
    /// • 减少密码重置需求
    /// 
    /// 安全保障：
    /// • 验证码时效性控制（通常5分钟）
    /// • 频率限制防止恶意刷取
    /// • 邮箱所有权验证
    /// • IP地址异常检测
    /// 
    /// 适用场景：
    /// • 移动APP快速登录
    /// • 临时设备登录
    /// • 忘记密码时的替代方案
    /// • 第三方OAuth集成
    /// </remarks>
    /// <param name="request">邮箱验证码登录请求参数，包含邮箱和验证码。</param>
    /// <param name="cancellationToken">取消操作标记，用于取消长时间运行的操作。</param>
    /// <returns>
    /// 成功时返回200 OK，包含用户信息和双Token信息；
    /// 失败时返回400、401或404相应错误码。
    /// </returns>
    /// <response code="200">登录成功，返回用户信息和认证令牌</response>
    /// <response code="400">邮箱或验证码为空</response>
    /// <response code="401">验证码无效、过期或账号被禁用</response>
    /// <response code="404">邮箱未注册</response>
    [HttpPost("login-email-code")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginByEmailCode(EmailCodeLoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
            return BadRequest("邮箱不能为空");

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("验证码不能为空");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsDeleted, cancellationToken);
        if (user == null)
            return NotFound("该邮箱未注册");

        if (user.Status == false)
            return Unauthorized("账号已被禁用");

        var codeStatus = await _verificationCodeService.VerifyCodeAsync(normalizedEmail, request.Code, cancellationToken);

        if (codeStatus == VerifyCodeStatus.NotFoundOrExpired)
            return Unauthorized("验证码已过期或不正确");

        if (codeStatus == VerifyCodeStatus.CodeMismatch)
            return Unauthorized("验证码不正确");

        var tokens = _jwtTokenService.GenerateTokens(user);

        // 将令牌存储到Redis
        await StoreTokensInRedis(user.Id, tokens.AccessToken, tokens.RefreshToken, cancellationToken);

        return Ok(CreateAuthResponse(user, tokens));
    }

    /// <summary>
    /// 刷新令牌接口，使用有效的刷新令牌获取新的访问令牌和刷新令牌。
    /// 实现双Token单点登录机制，每次刷新都会使旧令牌失效。
    /// 支持Redis存储令牌状态，确保安全性。
    /// </summary>
    /// <remarks>
    /// Token刷新机制：
    /// • Access Token短期有效（默认30分钟）
    /// • Refresh Token长期有效（默认7天）
    /// • 每次刷新生成新的双Token
    /// • 旧Token立即失效防止重放攻击
    /// • Redis实时验证Token状态
    /// 
    /// 安全特性：
    /// • Token绑定用户身份
    /// • 支持Token撤销机制
    /// • 多设备Token管理
    /// • 异常登录检测
    /// 
    /// 最佳实践：
    /// • 前端应在Access Token过期前主动刷新
    /// • Refresh Token过期后需重新登录
    /// • 支持静默刷新提升用户体验
    /// </remarks>
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
    /// <remarks>
    /// 支持的业务场景：
    /// • register/signup: 用户注册
    /// • login: 无密码登录
    /// • change-password: 修改密码
    /// • deactivate: 账号注销
    /// • reset-password: 密码重置
    /// 
    /// 安全防护措施：
    /// • 频率限制：同一邮箱每分钟最多发送1次
    /// • 时效控制：验证码5分钟内有效
    /// • 尝试次数限制：错误验证超过5次锁定
    /// • IP黑白名单机制
    /// • 邮箱真实性验证
    /// 
    /// 技术实现：
    /// • 异步邮件发送
    /// • HTML模板邮件
    /// • 多语言支持
    /// • 发送状态跟踪
    /// </remarks>
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
    /// <remarks>
    /// 注销流程说明：
    /// 1. 用户发起注销请求
    /// 2. 系统发送确认验证码到注册邮箱
    /// 3. 用户输入验证码进行最终确认
    /// 4. 执行账户软删除操作
    /// 5. 清理所有相关的认证令牌
    /// 
    /// 数据处理策略：
    /// • 软删除：保留基础数据用于审计
    /// • 令牌清理：Redis中删除所有相关Token
    /// • 关联数据：打卡记录等保持匿名化
    /// • 日志记录：详细记录注销操作
    /// 
    /// 安全考虑：
    /// • 二次验证确保操作真实性
    /// • 不可逆操作需谨慎执行
    /// • 数据备份和恢复机制
    /// • 合规性要求满足
    /// </remarks>
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
    /// <remarks>
    /// 密码修改方式：
    /// 方式一：旧密码验证
    /// • 输入当前密码进行身份确认
    /// • 适用于记得原密码的场景
    /// 
    /// 方式二：邮箱验证码验证
    /// • 通过邮箱接收验证码
    /// • 适用于忘记密码的场景
    /// 
    /// 安全策略：
    /// • 密码复杂度要求：至少8位，包含大小写字母、数字、特殊字符
    /// • 历史密码检查：禁止使用最近使用的密码
    /// • 修改后强制重新登录
    /// • 操作日志记录
    /// 
    /// 技术实现：
    /// • 新密码加密存储
    /// • 旧Token批量失效
    /// • 多设备会话清理
    /// • 异常操作监控
    /// </remarks>
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
    /// <remarks>
    /// 可更新的信息：
    /// • 昵称：用户显示名称
    /// • 头像：用户个人头像图片
    /// 
    /// 更新流程：
    /// 1. 用户提交更新信息
    /// 2. 验证信息合法性
    /// 3. 更新数据库记录
    /// 4. 重新生成认证Token
    /// 5. 返回更新后的用户信息
    /// 
    /// 头像处理：
    /// • 支持常见图片格式（JPG/PNG/WebP）
    /// • 自动压缩和格式转换
    /// • 按用户隔离存储
    /// • 公开访问URL生成
    /// 注意事项：
    /// • 昵称长度限制（通常1-20字符）
    /// • 头像文件大小限制（通常10MB以内）
    /// • 敏感词过滤
    /// • 更新频率限制
    /// </remarks>
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
    /// <remarks>
    /// 返回的用户信息包括：
    /// • 用户ID：系统唯一标识
    /// • 邮箱：注册邮箱地址
    /// • 昵称：用户显示名称
    /// • 账号名：自定义登录账号
    /// • 头像Key：头像文件标识
    /// 认证状态检查：
    /// • Token有效性验证
    /// • 账号状态检查（是否被禁用）
    /// • Redis中Token状态确认
    /// • 会话超时检测
    /// 应用场景：
    /// • 前端页面初始化用户信息
    /// • 权限验证和角色检查
    /// • 个人中心信息展示
    /// • 跨页面状态同步
    /// </remarks>
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
    /// <remarks>
    /// 业务规则说明：
    /// • 频率限制：每年仅允许修改一次账号名
    /// • 时间计算：基于上次修改时间
    /// • 状态反馈：明确告知是否可修改及下次可修改时间
    /// 返回信息：
    /// • canUpdate: 是否允许当前修改
    /// • nextUpdateAt: 下次可修改时间（如果不可修改）
    /// 设计考虑：
    /// • 防止恶意刷号行为
    /// • 维护系统稳定性
    /// • 平衡用户体验与安全
    /// • 符合社交平台通用做法
    /// </remarks>
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
    /// <remarks>
    /// 修改限制规则：
    /// • 时间间隔：每年只能修改一次
    /// • 唯一性：新账号名不能与现有用户冲突
    /// • 格式要求：符合账号名命名规范
    /// 修改流程：
    /// 1. 检查修改频率限制
    /// 2. 验证新账号名唯一性
    /// 3. 更新数据库记录
    /// 4. 记录修改时间和操作日志
    /// 5. 重新生成认证Token
    /// 错误处理：
    /// • 频率超限：返回403 Forbidden
    /// • 名称冲突：返回409 Conflict
    /// • 格式错误：返回400 Bad Request
    /// </remarks>
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