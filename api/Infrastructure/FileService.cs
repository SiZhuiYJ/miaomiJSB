using api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace api.Infrastructure;

/// <summary>
/// 文件存储与访问服务接口，按用户隔离文件的物理存储路径。
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

    /// <summary>
    /// 为指定用户保存通用文件（视频、音频、文档、压缩包等）并返回文件信息。
    /// </summary>
    /// <param name="userId">当前登录用户的 ID。</param>
    /// <param name="conversationId">会话ID（用于隐私文件权限控制）。</param>
    /// <param name="file">待保存的文件。</param>
    /// <param name="cancellationToken">取消操作的标记。</param>
    /// <returns>文件信息（文件Key、原始文件名、文件大小、MIME类型）。</returns>
    Task<FileInfoResult> SaveChatFileAsync(ulong userId, ulong conversationId, IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取聊天文件内容（需要验证会话成员权限）。
    /// </summary>
    /// <param name="userId">当前登录用户的 ID。</param>
    /// <param name="fileKey">文件标识。</param>
    /// <param name="cancellationToken">取消操作的标记。</param>
    /// <returns>若存在则返回文件流和内容类型，否则返回 null。</returns>
    Task<(Stream Stream, string ContentType, string FileName)?> GetChatFileAsync(ulong userId, string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 为指定会话保存头像文件。
    /// </summary>
    /// <param name="userId">当前登录用户的 ID。</param>
    /// <param name="conversationId">会话ID。</param>
    /// <param name="file">待保存的头像文件。</param>
    /// <param name="cancellationToken">取消操作的标记。</param>
    /// <returns>文件标识。</returns>
    Task<string> SaveConversationAvatarAsync(ulong userId, ulong conversationId, IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取会话头像（需要验证会话成员权限）。
    /// </summary>
    /// <param name="userId">当前登录用户的 ID。</param>
    /// <param name="conversationId">会话ID。</param>
    /// <param name="fileKey">头像文件标识。</param>
    /// <param name="cancellationToken">取消操作的标记。</param>
    /// <returns>若存在则返回文件流和内容类型，否则返回 null。</returns>
    Task<(Stream Stream, string ContentType)?> GetConversationAvatarAsync(ulong userId, ulong conversationId, string fileKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// 文件信息结果
/// </summary>
public class FileInfoResult
{
    public string FileKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// 基于本地文件系统的文件存储实现，按用户和会话隔离存储。
/// </summary>
public class LocalFileService(IWebHostEnvironment env, DailyCheckDbContext db) : IFileService
{
    readonly IWebHostEnvironment _env = env;
    readonly DailyCheckDbContext _db = db;
    const long MaxImageBytes = 10 * 1024 * 1024; // 10MB for images
    const long MaxChatFileBytes = 100 * 1024 * 1024; // 100MB for chat files

    static readonly string[] AllowedImageExtensions = [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    ];

    // 支持的文件扩展名和对应的MIME类型
    static readonly Dictionary<string, string> SupportedFileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // 图片
        { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".png", "image/png" },
        { ".gif", "image/gif" }, { ".webp", "image/webp" }, { ".bmp", "image/bmp" },
        { ".svg", "image/svg+xml" }, { ".ico", "image/x-icon" },
        
        // 视频
        { ".mp4", "video/mp4" }, { ".avi", "video/x-msvideo" }, { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" }, { ".flv", "video/x-flv" }, { ".mkv", "video/x-matroska" },
        { ".webm", "video/webm" }, { ".m4v", "video/x-m4v" },
        
        // 音频
        { ".mp3", "audio/mpeg" }, { ".wav", "audio/wav" }, { ".ogg", "audio/ogg" },
        { ".m4a", "audio/mp4" }, { ".flac", "audio/flac" }, { ".aac", "audio/aac" },
        { ".wma", "audio/x-ms-wma" }, { ".opus", "audio/opus" },
        
        // 文档
        { ".pdf", "application/pdf" }, { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".txt", "text/plain" }, { ".rtf", "application/rtf" },
        { ".csv", "text/csv" }, { ".md", "text/markdown" },
        
        // 压缩包
        { ".zip", "application/zip" }, { ".rar", "application/x-rar-compressed" },
        { ".7z", "application/x-7z-compressed" }, { ".tar", "application/x-tar" },
        { ".gz", "application/gzip" }, { ".bz2", "application/x-bzip2" },
        
        // 代码和文本
        { ".json", "application/json" }, { ".xml", "application/xml" },
        { ".html", "text/html" }, { ".css", "text/css" },
        { ".js", "application/javascript" }, { ".ts", "application/typescript" }
    };

    /// <inheritdoc />
    public async Task<string> SaveImageAsync(ulong userId, IFormFile file, bool isPublic, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File is required");
        if (file.Length > MaxImageBytes)
            throw new InvalidOperationException("Image exceeds maximum size limit");

        var originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(originalExtension))
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
                var candidates = AllowedImageExtensions
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

    /// <inheritdoc />
    public async Task<FileInfoResult> SaveChatFileAsync(ulong userId, ulong conversationId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("文件不能为空");

        if (file.Length > MaxChatFileBytes)
            throw new InvalidOperationException($"文件大小超过限制（最大{MaxChatFileBytes / 1024 / 1024}MB）");

        // 验证会话成员权限
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId
                && m.UserId == userId
                && m.LeftAt == null, cancellationToken);

        if (!isMember)
            throw new InvalidOperationException("您不是该会话的成员，无权上传文件");

        var originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // 检查文件类型是否支持
        if (!SupportedFileTypes.ContainsKey(originalExtension))
            throw new InvalidOperationException($"不支持的文件类型：{originalExtension}");

        // 生成文件Key（使用SHA256哈希 + 时间戳避免冲突）
        string fileKey;
        string storedExtension;
        string storedContentType;

        using (var hashStream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(hashStream, cancellationToken);
            var timestamp = DateTime.UtcNow.Ticks;
            fileKey = $"{Convert.ToHexString(hash).ToLowerInvariant()}_{timestamp:x}";
        }

        // 存储路径：按会话隔离
        var chatFilesRoot = Path.Combine(_env.ContentRootPath, "uploads", "chat", conversationId.ToString());
        Directory.CreateDirectory(chatFilesRoot);

        // 判断是否为图片，如果是则转换为WebP
        bool isImage = AllowedImageExtensions.Contains(originalExtension);

        if (isImage)
        {
            // 图片转换为WebP格式
            storedExtension = ".webp";
            storedContentType = "image/webp";
            var webpFilePath = Path.Combine(chatFilesRoot, fileKey + storedExtension);

            try
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream, cancellationToken);
                await image.SaveAsWebpAsync(webpFilePath, cancellationToken);
            }
            catch (Exception)
            {
                if (File.Exists(webpFilePath))
                    File.Delete(webpFilePath);
                throw new InvalidOperationException("图片处理失败");
            }
        }
        else
        {
            // 非图片文件直接保存
            storedExtension = originalExtension;
            storedContentType = SupportedFileTypes.GetValueOrDefault(originalExtension, "application/octet-stream");
            var filePath = Path.Combine(chatFilesRoot, fileKey + storedExtension);

            try
            {
                using var stream = file.OpenReadStream();
                using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            catch (Exception)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                throw new InvalidOperationException("文件保存失败");
            }
        }

        return new FileInfoResult
        {
            FileKey = fileKey, // 返回不带扩展名的fileKey
            OriginalFileName = Path.GetFileNameWithoutExtension(file.FileName),
            FileSize = isImage ? 0 : file.Length, // 图片转换后大小会变化，这里不返回准确值
            ContentType = storedContentType
        };
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string FileName)?> GetChatFileAsync(ulong userId, string fileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return null;

        // 安全检查：防止目录遍历攻击
        fileKey = Path.GetFileName(fileKey);

        // 判断文件key是否是当前用户的
        var message = await _db.ChatMessages.Where(m => m.Content == fileKey).FirstOrDefaultAsync(cancellationToken);
        var isMember = new ChatConversationMember();

        if (message == null)
            return null;

        var conversation_id = message.ConversationId;

        if (message.SenderUserId != userId)
        {
            // 不是当前用户的文件，继续验证会话成员权限
            isMember = await _db.ChatConversationMembers.Where(m => m.ConversationId == message.ConversationId
                     && m.UserId == userId
                     && m.LeftAt == null).FirstOrDefaultAsync(cancellationToken);
            if (isMember == null)
                return null;
            else conversation_id = isMember.ConversationId;

        }

        // 构建文件路径
        var chatFilesRoot = Path.Combine(_env.ContentRootPath, "uploads", "chat", conversation_id.ToString());

        string filePath;
        string extension;

        if (Path.HasExtension(fileKey))
        {
            // Explicit extension
            filePath = Path.Combine(chatFilesRoot, fileKey);
            extension = Path.GetExtension(fileKey).ToLowerInvariant();
        }
        else
        {
            filePath = Path.Combine(chatFilesRoot, fileKey + ".webp");
            extension = ".webp";

            if (!File.Exists(filePath))
            {
                var candidates = AllowedImageExtensions
                    .Where(ext => !string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase))
                    .Select(ext => new
                    {
                        Ext = ext,
                        Path = Path.Combine(chatFilesRoot, fileKey + ext)
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
            return null;

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
        return new(stream, contentType, fileKey);
    }

    /// <inheritdoc />
    public async Task<string> SaveConversationAvatarAsync(ulong userId, ulong conversationId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("文件不能为空");

        // 验证会话成员权限（只有成员才能更新群头像）
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId
                && m.UserId == userId
                && m.LeftAt == null, cancellationToken);

        if (!isMember)
            throw new InvalidOperationException("您不是该会话的成员，无权上传头像");

        var originalExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(originalExtension))
            throw new InvalidOperationException("只支持图片文件作为头像");

        // 生成文件Key
        string fileKey;
        using (var hashStream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(hashStream, cancellationToken);
            var timestamp = DateTime.UtcNow.Ticks;
            fileKey = $"avatar_{Convert.ToHexString(hash).ToLowerInvariant()}_{timestamp:x}";
        }

        // 存储路径：按会话ID存储
        var avatarRoot = Path.Combine(_env.ContentRootPath, "uploads", "chat", conversationId.ToString(), "avatars");
        Directory.CreateDirectory(avatarRoot);

        var webpFilePath = Path.Combine(avatarRoot, fileKey + ".webp");

        try
        {
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
            throw new InvalidOperationException("头像处理失败");
        }

        return fileKey;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType)?> GetConversationAvatarAsync(ulong userId, ulong conversationId, string fileKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return null;

        // 验证会话成员权限
        var isMember = await _db.ChatConversationMembers
            .AnyAsync(m => m.ConversationId == conversationId
                && m.UserId == userId
                && m.LeftAt == null, cancellationToken);

        if (!isMember)
            return null; // 无权限时返回null

        // 安全检查
        fileKey = Path.GetFileName(fileKey);

        // 构建文件路径
        var avatarRoot = Path.Combine(_env.ContentRootPath, "uploads", "chat", conversationId.ToString(), "avatars");

        string filePath;
        string extension;

        if (Path.HasExtension(fileKey))
        {
            filePath = Path.Combine(avatarRoot, fileKey);
            extension = Path.GetExtension(fileKey).ToLowerInvariant();
        }
        else
        {
            filePath = Path.Combine(avatarRoot, fileKey + ".webp");
            extension = ".webp";

            if (!File.Exists(filePath))
            {
                var candidates = AllowedImageExtensions
                    .Where(ext => !string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase))
                    .Select(ext => new
                    {
                        Ext = ext,
                        Path = Path.Combine(avatarRoot, fileKey + ext)
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
            return null;

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
        return new(stream, contentType);
    }
}
