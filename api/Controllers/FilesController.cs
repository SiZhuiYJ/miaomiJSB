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
