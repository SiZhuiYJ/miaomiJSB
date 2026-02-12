using System;

namespace api.Models;

/// <summary>
/// 用户注册请求参数。
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public string? UserAccount { get; set; }

    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 用户登录请求参数。
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户邮箱地址。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 登录密码明文，将在服务端进行哈希验证。
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

public class AccountLoginRequest
{
    public string UserAccount { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 认证响应数据，包含用户信息和双 token 信息。
/// </summary>
public class AuthResponse
{
    public long UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public string? UserAccount { get; set; }

    public string? AvatarKey { get; set; }

    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; set; }

    public DateTime RefreshTokenExpiresAt { get; set; }
}

public class UserBasicResponse
{
    public long UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public string? UserAccount { get; set; }

    public string? AvatarKey { get; set; }
}

public class UpdateUserProfileRequest
{
    public string? NickName { get; set; }

    public string? AvatarKey { get; set; }
}

/// <summary>
/// 更新用户账号请求参数。
/// </summary>
public class UpdateUserAccountRequest
{
    /// <summary>
    /// 新的用户账号名。
    /// </summary>
    public string UserAccount { get; set; } = string.Empty;
}

/// <summary>
/// 刷新令牌请求参数。
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// 长期刷新令牌字符串。
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 发送邮箱验证码请求参数。
/// </summary>
public class SendEmailCodeRequest
{
    /// <summary>
    /// 接收验证码的邮箱地址。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    public string? ActionType { get; set; }
}

/// <summary>
/// 校验邮箱验证码请求参数（当前预留，未公开为独立接口）。
/// </summary>
public class VerifyEmailCodeRequest
{
    /// <summary>
    /// 接收验证码的邮箱地址。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 用户提交的邮箱验证码。
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

public class DeactivateAccountRequest
{
    public string Code { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
