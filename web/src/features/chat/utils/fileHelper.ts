import { uploadChatFile, getChatFileUrl } from "../api";
import type { FileExtra, MessageType } from "../types";
import { extractVideoFrameToWebP } from '@/utils/convertToWebP';
import http from "@/libs/http";

// 文件大小限制 (100MB)
const MAX_FILE_SIZE = 100 * 1024 * 1024;

// 支持的文件类型
export const SUPPORTED_FILE_TYPES = {
  image: [
    "image/jpeg",
    "image/png",
    "image/gif",
    "image/webp",
    "image/bmp",
    "image/svg+xml",
  ],
  video: [
    "video/mp4",
    "video/webm",
    "video/ogg",
    "video/quicktime",
    "video/x-msvideo",
    "video/x-ms-wmv",
  ],
  audio: [
    "audio/mpeg",
    "audio/wav",
    "audio/ogg",
    "audio/mp4",
    "audio/flac",
    "audio/aac",
  ],
  document: [
    "application/pdf",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "application/vnd.ms-excel",
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "application/vnd.ms-powerpoint",
    "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    "text/plain",
    "text/markdown",
    "application/rtf",
    "text/csv",
  ],
  archive: [
    "application/zip",
    "application/x-rar-compressed",
    "application/x-7z-compressed",
    "application/x-tar",
    "application/gzip",
    "application/x-bzip2",
  ],
};

export type FileCategory = keyof typeof SUPPORTED_FILE_TYPES | "unknown";
export type FilePreviewType =
  | "image"
  | "video"
  | "audio"
  | "word"
  | "excel"
  | "pptx"
  | "pdf"
  | "text"
  | "doc"
  | "xls"
  | "ppt"
  | "archive"
  | "unknown";

export type DocumentIconType =
  | "word"
  | "excel"
  | "pptx"
  | "pdf"
  | "text"
  | "zip"
  | "rar"
  | "css"
  | "htm"
  | "java"
  | "note"
  | "unknown";

const MIME_PREVIEW_TYPE_MAP: Record<string, FilePreviewType> = {
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": "excel",
  "application/vnd.ms-excel": "xls",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document": "word",
  "application/msword": "doc",
  "application/vnd.openxmlformats-officedocument.presentationml.presentation": "pptx",
  "application/vnd.ms-powerpoint": "ppt",
  "application/pdf": "pdf",
  "text/markdown": "text",
  "text/plain": "text",
  "text/csv": "text",
  "text/html": "text",
  "application/json": "text",
  "application/xml": "text",
  "text/xml": "text",
  "text/css": "text",
  "application/css": "text",
  "application/javascript": "text",
  "text/javascript": "text",
};

const EXT_PREVIEW_TYPE_MAP: Record<string, FilePreviewType> = {
  docx: "word",
  doc: "doc",
  xlsx: "excel",
  xls: "xls",
  pptx: "pptx",
  ppt: "ppt",
  pdf: "pdf",
  txt: "text",
  md: "text",
  csv: "text",
  html: "text",
  htm: "text",
  json: "text",
  xml: "text",
  css: "text",
  js: "text",
  zip: "archive",
  rar: "archive",
  "7z": "archive",
  tar: "archive",
  gz: "archive",
  bz2: "archive",
};

const PREVIEWABLE_FILE_TYPES = new Set<FilePreviewType>([
  "image",
  "video",
  "audio",
  "word",
  "excel",
  "pptx",
  "pdf",
  "text",
  "doc",
  "xls",
  "ppt",
]);

/**
 * 获取文件类型分类
 */
export function getFileCategory(
  mimeType?: string | null,
): FileCategory {
  if (!mimeType) return "unknown";
  for (const [category, types] of Object.entries(SUPPORTED_FILE_TYPES)) {
    if (types.includes(mimeType)) {
      return category as FileCategory;
    }
  }
  return "unknown";
}

/**
 * 根据文件名获取文件类型
 */
export function getFileCategoryByName(
  fileName: string,
): FileCategory {
  const ext = fileName.split(".").pop()?.toLowerCase();

  const imageExts = ["jpg", "jpeg", "png", "gif", "webp", "bmp", "svg", "ico"];
  const videoExts = ["mp4", "avi", "mov", "wmv", "flv", "mkv", "webm", "m4v"];
  const audioExts = ["mp3", "wav", "ogg", "m4a", "flac", "aac", "wma", "opus"];
  const docExts = [
    "pdf",
    "doc",
    "docx",
    "xls",
    "xlsx",
    "ppt",
    "pptx",
    "txt",
    "rtf",
    "csv",
    "md",
  ];
  const archiveExts = ["zip", "rar", "7z", "tar", "gz", "bz2"];

  if (!ext) return "unknown";
  if (imageExts.includes(ext)) return "image";
  if (videoExts.includes(ext)) return "video";
  if (audioExts.includes(ext)) return "audio";
  if (docExts.includes(ext)) return "document";
  if (archiveExts.includes(ext)) return "archive";

  return "unknown";
}

export function getFilePreviewType(
  file?: Pick<FileExtra, "mimeType" | "fileName"> | null,
): FilePreviewType {
  if (!file) return "unknown";

  const mimeType = file.mimeType?.toLowerCase() || "";
  if (mimeType.startsWith("image/")) return "image";
  if (mimeType.startsWith("video/")) return "video";
  if (mimeType.startsWith("audio/")) return "audio";

  const mappedByMime = MIME_PREVIEW_TYPE_MAP[mimeType];
  if (mappedByMime) return mappedByMime;

  const category = getFileCategory(mimeType);
  if (category === "archive") return "archive";

  const ext = file.fileName.split(".").pop()?.toLowerCase() || "";
  return EXT_PREVIEW_TYPE_MAP[ext] || "unknown";
}

export function getDocumentIconType(
  previewType: FilePreviewType,
  file?: Pick<FileExtra, "mimeType" | "fileName"> | null,
): DocumentIconType {
  if (previewType === "word" || previewType === "doc") return "word";
  if (previewType === "excel" || previewType === "xls") return "excel";
  if (previewType === "pptx" || previewType === "ppt") return "pptx";
  if (previewType === "pdf") return "pdf";

  const ext = file?.fileName.split(".").pop()?.toLowerCase() || "";
  if (previewType === "archive") return ext === "rar" ? "rar" : "zip";
  if (ext === "css") return "css";
  if (ext === "html" || ext === "htm") return "htm";
  if (ext === "js") return "java";
  if (ext === "md") return "note";
  if (previewType === "text") return "text";

  return "unknown";
}

export function getFileTypeLabel(previewType: FilePreviewType): string {
  const labels: Record<FilePreviewType, string> = {
    image: "image",
    video: "video",
    audio: "audio",
    word: "word",
    excel: "excel",
    pptx: "pptx",
    pdf: "pdf",
    text: "text",
    doc: "doc",
    xls: "xls",
    ppt: "ppt",
    archive: "archive",
    unknown: "file",
  };
  return labels[previewType];
}

export function isPreviewableFileType(previewType: FilePreviewType): boolean {
  return PREVIEWABLE_FILE_TYPES.has(previewType);
}

/**
 * 格式化文件大小
 */
export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) return "0 B";
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(k)), sizes.length - 1);
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + " " + sizes[i];
}

/**
 * 验证文件
 */
export function validateFile(file: File): { valid: boolean; error?: string } {
  if (file.size > MAX_FILE_SIZE) {
    return {
      valid: false,
      error: `文件大小超过限制（最大${formatFileSize(MAX_FILE_SIZE)}）`,
    };
  }

  if (file.size === 0) {
    return { valid: false, error: "文件不能为空" };
  }

  const category = getFileCategory(file.type);
  const categoryByName = category === "unknown" ? getFileCategoryByName(file.name) : category;
  if (categoryByName === "unknown") {
    return { valid: false, error: "不支持的文件类型" };
  }

  return { valid: true };
}

/**
 * 上传图片/视频/音频等文件并生成消息extra
 */
export async function uploadFileForMessage(
  conversationId: number,
  file: File,
): Promise<{ extra: FileExtra; messageType: MessageType }> {
  // 验证文件
  const validation = validateFile(file);
  if (!validation.valid) {
    throw new Error(validation.error);
  }
  // 判断是否为视频
  if (file.type.startsWith('video/')) {
    // 1. 生成封面文件
    const coverFile = await extractVideoFrameToWebP(file, { quality: 1 });

    // 2. 并行上传视频和封面
    const [videoInfo, coverInfo] = await Promise.all([
      uploadChatFile(conversationId, file),
      uploadChatFile(conversationId, coverFile),
    ]);

    // 3. 获取视频时长
    const duration = await getMediaDuration(file);

    // 4. 构造 extra
    const extra: FileExtra = {
      fileName: file.name,
      fileSize: file.size,
      fileKey: videoInfo.data.fileKey,          // 视频 key（带 .mp4 等扩展名）
      fileUrl: getChatFileUrl(videoInfo.data.fileKey),
      mimeType: file.type,
      // thumbnailKey: coverInfo.data.fileKey,      // 封面 key（.webp）
      thumbnailUrl: coverInfo.data.fileKey,
      duration: duration ?? undefined,
    };
    return { extra, messageType: 'video' };
  }

  // 上传文件
  const response = await uploadChatFile(conversationId, file);
  const fileInfo = response.data;

  // 构建文件URL
  const fileUrl = getChatFileUrl(fileInfo.fileKey);

  // 确定消息类型
  const category = getFileCategory(fileInfo.contentType);
  let messageType: MessageType;

  switch (category) {
    case "image":
      messageType = "image";
      break;
    case "video":
      messageType = "video";
      break;
    case "audio":
      messageType = "audio";
      break;
    default:
      messageType = "file";
  }

  // 构建FileExtra对象
  const fileExtra: FileExtra = {
    fileName: fileInfo.originalFileName,
    fileSize: fileInfo.fileSize,
    fileUrl: fileUrl,
    fileKey: fileInfo.fileKey,
    mimeType: fileInfo.contentType,
  };

  // 对于图片，尝试获取尺寸
  if (messageType === "image") {
    try {
      const dimensions = await getImageDimensions(file);
      if (dimensions) {
        fileExtra.width = dimensions.width;
        fileExtra.height = dimensions.height;
      }
    } catch (e) {
      console.warn("Failed to get image dimensions:", e);
    }
  }

  // 对于视频/音频，可以尝试获取时长
  if (messageType === "video" || messageType === "audio") {
    try {
      const duration = await getMediaDuration(file);
      if (duration) {
        fileExtra.duration = duration;
      }
    } catch (e) {
      console.warn("Failed to get media duration:", e);
    }
  }

  return {
    extra: fileExtra,
    messageType,
  };
}

/**
 * 获取图片尺寸
 */
function getImageDimensions(
  file: File,
): Promise<{ width: number; height: number } | null> {
  return new Promise((resolve) => {
    const img = new Image();
    const url = URL.createObjectURL(file);

    img.onload = () => {
      const dimensions = { width: img.width, height: img.height };
      URL.revokeObjectURL(url);
      resolve(dimensions);
    };

    img.onerror = () => {
      URL.revokeObjectURL(url);
      resolve(null);
    };

    img.src = url;
  });
}

/**
 * 获取媒体文件时长
 */
function getMediaDuration(file: File): Promise<number | null> {
  return new Promise((resolve) => {
    const video = document.createElement("video");
    const url = URL.createObjectURL(file);

    video.onloadedmetadata = () => {
      const duration = video.duration;
      URL.revokeObjectURL(url);
      resolve(duration && isFinite(duration) ? duration : null);
    };

    video.onerror = () => {
      URL.revokeObjectURL(url);
      resolve(null);
    };

    video.preload = "metadata";
    video.src = url;
  });
}

/**
 *  * 解析消息的extra字段（兼容大小写不一的字段名）
 */
export function parseMessageExtra(extra?: string | null): FileExtra | null {
  if (!extra) return null;
  try {
    const raw = JSON.parse(extra);
    // 标准化常见的大小写不一致字段
    if (raw.MimeType && !raw.mimeType) {
      raw.mimeType = raw.MimeType;
      delete raw.MimeType;
    }
    if (raw.FileName && !raw.fileName) {
      raw.fileName = raw.FileName;
      delete raw.FileName;
    }
    if (raw.FileSize && !raw.fileSize) {
      raw.fileSize = raw.FileSize;
      delete raw.FileSize;
    }
    if (raw.FileUrl && !raw.fileUrl) {
      raw.fileUrl = raw.FileUrl;
      delete raw.FileUrl;
    }
    if (raw.FileKey && !raw.fileKey) {
      raw.fileKey = raw.FileKey;
      delete raw.FileKey;
    }
    if (raw.ThumbnailUrl && !raw.thumbnailUrl) {
      raw.thumbnailUrl = raw.ThumbnailUrl;
      delete raw.ThumbnailUrl;
    }
    if (raw.Width && !raw.width) {
      raw.width = raw.Width;
      delete raw.Width;
    }
    if (raw.Height && !raw.height) {
      raw.height = raw.Height;
      delete raw.Height;
    }
    if (raw.Duration && !raw.duration) {
      raw.duration = raw.Duration;
      delete raw.Duration;
    }
    return raw as FileExtra;
  } catch {
    return null;
  }
}

/**
 * 获取文件图标（用于文档和压缩包）
 */
export function getFileIcon(category: string): string {
  const icons: Record<string, string> = {
    image: "🖼️",
    video: "🎬",
    audio: "🎵",
    document: "📄",
    archive: "📦",
    unknown: "📎",
  };
  return icons[category] || "📎";
}

/**
 * 获取聊天文件的Blob数据（带Token认证）
 * @param fileKey 文件Key（会自动去除扩展名）
 * @returns Blob对象
 */
export async function fetchChatFile(fileKey: string): Promise<Blob> {
  if (!fileKey) {
    throw new Error("文件Key不能为空");
  }

  // 去除扩展名，统一使用不带扩展名的fileKey
  const lastDot = fileKey.lastIndexOf(".");
  const cleanFileKey = lastDot > 0 ? fileKey.substring(0, lastDot) : fileKey;

  const baseUrl = import.meta.env.VITE_API_BASE_URL || "";
  const normalizedBaseUrl = baseUrl.endsWith("/")
    ? baseUrl.slice(0, -1)
    : baseUrl;
  const url = `${normalizedBaseUrl}/mm/files/chat/${cleanFileKey}`;

  const token = localStorage.getItem("accessToken");
  const headers = token ? { Authorization: `Bearer ${token}` } : {};

  try {
    const response = await http.get(url, {
      responseType: "blob",
      headers,
    });

    return response.data as Blob;
  } catch (error) {
    console.error("获取文件失败:", error);
    throw new Error("文件获取失败");
  }
}

/**
 * 将Blob转换为Object URL
 * @param blob Blob对象
 * @returns Object URL
 */
export function createBlobUrl(blob: Blob): string {
  return URL.createObjectURL(blob);
}

/**
 * 释放Object URL
 * @param url Object URL
 */
export function revokeBlobUrl(url: string): void {
  if (url) {
    URL.revokeObjectURL(url);
  }
}
