using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using System.Linq;
using System.Security.Cryptography;

namespace api.Infrastructure;

/// <summary>
/// 文件存储与访问服务接口，按用户隔离图片的物理存储路径。
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 为指定用户保存图片文件并返回文件标识。
    /// </summary>
    /// <param name="userId">当前登录用户的 ID。</param>
    /// <param name="file">待保存的图片文件。</param>
       /// <param name="isPublic">是否为公开文件（公开文件存储在公共路径，私有文件存储在加密路径）。</param>
    /// <param name="cancellationToken">取消操作的标记。</param>
    /// <returns>用于后续访问该图片的文件标识（不含扩展名，默认访问 .webp）。</returns>
    Task<string> SaveImageAsync(ulong userId, IFormFile file, bool isPublic, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为指定用户读取图片文件内容。
    /// </summary>
    /// <param name="userId">当前登录用户的 ID。</param>
    /// <param name="fileKey">图片文件标识。</param>
    /// <param name="isPublic">是否为公开文件。</param>
    /// <param name="cancellationToken">取消操作的标记。</param>
    /// <returns>若存在则返回文件流和内容类型，否则返回 null。</returns>
    Task<(Stream Stream, string ContentType)?> GetImageAsync(ulong userId, string fileKey, bool isPublic, CancellationToken cancellationToken = default);
}

/// <summary>
/// 基于本地文件系统的图片存储实现，每个用户使用独立目录隔离访问。
/// </summary>
public class LocalFileService(IWebHostEnvironment env) : IFileService
{
    readonly IWebHostEnvironment _env = env;

    static readonly string[] AllowedExtensions = [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    ];

    /// <inheritdoc />
    public async Task<string> SaveImageAsync(ulong userId, IFormFile file, bool isPublic, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is required");

        var originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(originalExtension))
            throw new InvalidOperationException("Only image files are allowed");

        var root = GetRootPath(userId, isPublic);
        Directory.CreateDirectory(root);

        string fileId;
        using (var hashStream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(hashStream, cancellationToken);
            fileId = Convert.ToHexString(hash).ToLowerInvariant();
        }

        var webpFilePath = Path.Combine(root, fileId + ".webp");
        if (File.Exists(webpFilePath))
            return fileId;

        try
        {
            // 如果已经是 WebP，直接保存；否则转换
            if (originalExtension == ".webp")
            {
                using var stream = file.OpenReadStream();
                using var fileStream = new FileStream(webpFilePath, FileMode.CreateNew, FileAccess.Write);
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            else
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream, cancellationToken);
                await image.SaveAsWebpAsync(webpFilePath, cancellationToken);
            }
        }
        catch (Exception)
        {
            if (File.Exists(webpFilePath))
                File.Delete(webpFilePath);
            throw new InvalidOperationException("Failed to process image.");
        }

        return fileId;
    }

    /// <inheritdoc />
    public Task<(Stream Stream, string ContentType)?> GetImageAsync(ulong userId, string fileKey, bool isPublic, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return Task.FromResult<(Stream, string)?>(null);

        // Security check
        fileKey = Path.GetFileName(fileKey);

        var root = GetRootPath(userId, isPublic);
        
        string filePath;
        string extension;

        if (Path.HasExtension(fileKey))
        {
            // Explicit extension
            filePath = Path.Combine(root, fileKey);
            extension = Path.GetExtension(fileKey).ToLowerInvariant();
        }
        else
        {
            filePath = Path.Combine(root, fileKey + ".webp");
            extension = ".webp";

            if (!File.Exists(filePath))
            {
                var candidates = AllowedExtensions
                    .Where(ext => !string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase))
                    .Select(ext => new
                    {
                        Ext = ext,
                        Path = Path.Combine(root, fileKey + ext)
                    })
                    .ToList();

                var fallback = candidates.FirstOrDefault(c => File.Exists(c.Path));
                if (fallback != null)
                {
                    filePath = fallback.Path;
                    extension = fallback.Ext.ToLowerInvariant();
                }
            }
        }

        if (!File.Exists(filePath))
            return Task.FromResult<(Stream, string)?>(null);

        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream, string)?>(new(stream, contentType));
    }

    private string GetRootPath(ulong userId, bool isPublic)
    {
        // 存放加密文件的路径改到用户文件夹的加密文件夹下：uploads/users/{userId}/encrypted
        // 公开文件夹就在公开路径下：uploads/public/users/{userId}
        if (isPublic)
        {
            return Path.Combine(_env.ContentRootPath, "uploads", "public", "users", userId.ToString());
        }
        else
        {
            return Path.Combine(_env.ContentRootPath, "uploads", "users", userId.ToString(), "encrypted");
        }
    }
}
