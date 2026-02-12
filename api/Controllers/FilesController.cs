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
/// 文件相关接口，提供按用户隔离的图片上传与访问能力。
/// </summary>
[ApiController]
[Route("mm/[controller]")]
[Authorize]
public class FilesController(IFileService fileService, DailyCheckDbContext db) : ControllerBase
{
    readonly IFileService _fileService = fileService;
    readonly DailyCheckDbContext _db = db;

    /// <summary>
    /// 上传当前登录用户的头像（公开访问）。
    /// 上传成功后会自动更新该用户的头像链接。
    /// </summary>
    /// <param name="file">待上传的图片文件。</param>
    /// <returns>包含图片文件 Key 的响应。</returns>
    [HttpPost("avatar")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> UploadUserAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var userId = GetUserId();
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId && !x.IsDeleted);
        if (user == null)
            return NotFound("User not found");

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
    /// 获取指定用户的头像（公开接口）。
    /// </summary>
    /// <param name="userId">指定的用户ID。</param>
    /// <param name="key">图片文件 Key。</param>
    /// <returns>图片二进制内容。</returns>
    [HttpGet("users/{userId}/{key}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserAvatar(ulong userId, string key)
    {
        var result = await _fileService.GetImageAsync(userId, key, true);

        if (result == null)
            return NotFound("Image file not found");

        return File(result.Value.Stream, result.Value.ContentType);
    }

    /// <summary>
    /// 上传单张图片文件并返回仅当前用户可访问的图片 URL。
    /// 所有上传的图片都将自动转换为 WebP 格式存储。
    /// </summary>
    /// <param name="file">待上传的图片文件。</param>
    /// <returns>包含图片访问 URL 的响应。</returns>
    [HttpPost("images")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

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
    /// 获取当前用户已上传的图片内容，仅可访问自己的图片。
    /// </summary>
    /// <param name="fileKey">上传时返回的图片标识。</param>
    /// <returns>图片二进制内容。</returns>
    [HttpGet("images/{fileKey}")]
    public async Task<IActionResult> GetImage(string fileKey)
    {
        var userId = GetUserId();
        var result = await _fileService.GetImageAsync(userId, fileKey, false);

        if (result == null)
            return NotFound();

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
