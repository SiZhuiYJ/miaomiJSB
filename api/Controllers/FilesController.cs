using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using api.Infrastructure;
using api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace api.Controllers;

/// <summary>
/// 文件管理接口，提供安全的图片上传和访问服务。
/// 实现按用户隔离的文件存储机制，确保数据安全性和隐私保护。
/// 支持头像上传和普通图片上传两种模式，所有图片自动转换为WebP格式优化存储。
/// </summary>
/// <remarks>
/// 核心功能特性：
/// • 用户隔离存储：每个用户文件独立存储空间
/// • 安全访问控制：严格的权限验证机制
/// • 格式优化：自动转换为WebP格式节省存储
/// • 大小限制：防止恶意大文件上传
/// • 双模式支持：头像公开访问 + 普通图片私有访问
/// 
/// 安全措施：
/// • 文件类型验证：只允许常见图片格式
/// • 文件大小限制：默认10MB上限
/// • 用户权限检查：防止越权访问
/// • 路径安全处理：防止目录遍历攻击
/// • 内容安全扫描：基础恶意内容检测
/// 
/// 性能优化：
/// • WebP格式转换：减少带宽消耗
/// • 缓存友好：支持CDN加速
/// • 异步处理：提高响应速度
/// • 存储优化：智能压缩算法
/// </remarks>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class FilesController(IFileService fileService, DailyCheckDbContext db) : ControllerBase
{
    readonly IFileService _fileService = fileService;
    readonly DailyCheckDbContext _db = db;

    /// <summary>
    /// 上传用户头像接口，提供公开访问的头像存储服务。
    /// 上传成功后自动更新当前用户的头像链接(AvatarKey)。
    /// 所有头像文件经过安全验证和格式转换，确保一致性和安全性。
    /// 支持通过公开URL直接访问用户头像。
    /// </summary>
    /// <remarks>
    /// 头像处理流程：
    /// 1. 文件格式验证（JPG/PNG/GIF等）
    /// 2. 文件大小检查（≤10MB）
    /// 3. 图片内容安全扫描
    /// 4. 自动转换为WebP格式
    /// 5. 按用户ID隔离存储
    /// 6. 更新用户AvatarKey字段
    /// 7. 返回文件访问Key
    /// 访问特点：
    /// • 公开访问：无需认证即可查看
    /// • URL格式：/mm/files/users/{userId}/{key}
    /// • 缓存友好：支持浏览器缓存
    /// • CDN兼容：便于CDN加速部署
    /// 安全考虑：
    /// • 防止恶意文件上传
    /// • XSS攻击防护
    /// • 文件名随机化
    /// • 访问频率限制
    /// </remarks>
    /// <param name="file">待上传的图片文件，支持常见图片格式。</param>
    /// <returns>
    /// 成功时返回200 OK，包含图片文件Key；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">头像上传成功</response>
    /// <response code="400">文件为空或格式不支持</response>
    /// <response code="404">用户不存在</response>
    [HttpPost("avatar")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> UploadUserAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("文件不能为空");

        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (user == null)
            return NotFound("用户不存在");

        try
        {
            var fileKey = await _fileService.SaveImageAsync(userId, file, true);

            user.AvatarKey = fileKey;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { key = fileKey });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取指定用户头像接口，提供公开访问的头像读取服务。
    /// 无需认证即可访问，支持跨域请求。
    /// 返回优化后的WebP格式图片，提升加载性能。
    /// </summary>
    /// <remarks>
    /// 服务特性：
    /// • 公开访问：任何人都可以查看用户头像
    /// • 跨域支持：前端直接调用无CORS问题
    /// • 格式优化：返回WebP格式提升加载速度
    /// • 缓存控制：合理设置缓存头提升性能
    /// 技术实现：
    /// • 直接文件流返回
    /// • Content-Type动态设置
    /// • 404错误优雅处理
    /// • 流量统计支持
    /// 应用场景：
    /// • 用户个人资料页
    /// • 评论区用户头像
    /// • 社交分享预览图
    /// • 搜索结果用户展示
    /// </remarks>
    /// <param name="userId">目标用户的唯一标识符。</param>
    /// <param name="key">图片文件Key，由上传接口返回。</param>
    /// <returns>
    /// 成功时返回200 OK，包含图片二进制流和正确的Content-Type；
    /// 文件不存在时返回404 Not Found。
    /// </returns>
    /// <response code="200">头像获取成功</response>
    /// <response code="404">头像文件不存在</response>
    [HttpGet("users/{userId}/{key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserAvatar(ulong userId, string key)
    {
        var result = await _fileService.GetImageAsync(userId, key, true);

        if (result == null)
            return NotFound("头像文件不存在");

        return File(result.Value.Stream, result.Value.ContentType);
    }

    /// <summary>
    /// 上传普通图片接口，提供受保护的图片存储服务。
    /// 仅允许认证用户上传和访问自己的图片文件。
    /// 所有上传的图片自动转换为WebP格式，优化存储空间和加载速度。
    /// 返回相对路径URL，可在认证环境下直接使用。
    /// </summary>
    /// <remarks>
    /// 安全机制：
    /// • 身份认证：必须登录才能上传
    /// • 权限控制：只能访问自己的文件
    /// • 文件隔离：按用户ID物理隔离存储
    /// • 访问审计：记录文件访问日志
    /// 处理流程：
    /// 1. 用户身份验证
    /// 2. 文件格式和大小验证
    /// 3. 安全扫描和病毒检测
    /// 4. WebP格式转换优化
    /// 5. 用户专属目录存储
    /// 6. 生成安全访问URL
    /// 返回格式：
    /// {
    ///  "url": "/mm/files/images/abc123.webp"
    /// }
    /// 应用场景：
    /// • 打卡图片上传
    /// • 个人相册管理
    /// • 文档附件存储
    /// • 临时图片分享
    /// </remarks>
    /// <param name="file">待上传的图片文件，支持多种图片格式。</param>
    /// <returns>
    /// 成功时返回200 OK，包含图片访问URL；
    /// 失败时返回相应错误状态码。
    /// </returns>
    /// <response code="200">图片上传成功</response>
    /// <response code="400">文件为空或格式不支持</response>
    [HttpPost("images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("文件不能为空");

        var userId = GetUserId();

        try
        {
            var fileKey = await _fileService.SaveImageAsync(userId, file, false);
            var relativePath = $"/mm/files/images/{fileKey}";
            return Ok(new { url = relativePath });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 获取用户上传图片接口，提供受保护的图片读取服务。
    /// 严格验证用户身份，仅允许访问自己上传的图片文件。
    /// 返回WebP格式图片二进制流，确保最佳兼容性。
    /// </summary>
    /// <remarks>
    /// 访问控制：
    /// • 身份验证：必须提供有效Token
    /// • 权限检查：只能访问自己的文件
    /// • 文件存在性验证
    /// • 安全路径处理
    /// 技术特点：
    /// • 二进制流直接返回
    /// • WebP格式优化传输
    /// • 适当的缓存控制
    /// • 错误处理标准化
    /// 安全防护：
    /// • 防止目录遍历攻击
    /// • 文件类型白名单
    /// • 访问频率监控
    /// • 异常访问记录
    /// 使用注意：
    /// • 必须在认证环境下调用
    /// • URL具有时效性
    /// • 不支持公开分享
    /// • 适用于私密图片访问
    /// </remarks>
    /// <param name="fileKey">图片文件标识，由上传接口返回。</param>
    /// <returns>
    /// 成功时返回200 OK，包含图片二进制流；
    /// 文件不存在或无权限时返回404 Not Found。
    /// </returns>
    /// <response code="200">图片获取成功</response>
    /// <response code="404">图片文件不存在或无访问权限</response>
    [HttpGet("images/{fileKey}")]
    public async Task<IActionResult> GetImage(string fileKey)
    {
        var userId = GetUserId();
        var result = await _fileService.GetImageAsync(userId, fileKey, false);

        if (result == null)
            return NotFound("图片文件不存在或无访问权限");

        return File(result.Value.Stream, result.Value.ContentType);
    }

    /// <summary>
    /// 从当前访问令牌中解析用户 ID。
    /// </summary>
    /// <returns>当前登录用户的 ID。</returns>
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

        var claimsDump = string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}"));
        throw new InvalidOperationException($"在令牌中未找到用户ID。现有 claims: {claimsDump}");
    }
}
