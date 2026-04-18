import type { AxiosProgressEvent } from "axios";
import { API_BASE_URL } from "@/config";
import http from "@/libs/http";
import { convertToWebP, extractVideoFrameToWebP } from "@/utils/convertToWebP";
import { getChatFileUrl, uploadChatFile } from "../api";
import type { FileExtra, MessageType } from "../types";

const MAX_FILE_SIZE = 100 * 1024 * 1024;

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
} as const;

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
  | "markdown"
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
  | "markdown"
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
  "text/markdown": "markdown",
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
  md: "markdown",
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
  "markdown",
  "doc",
  "xls",
  "ppt",
]);

export interface PreparedMessageUploadFiles {
  files: File[];
  invalidFiles: { name: string; error: string }[];
  failedFiles: { name: string; error: string }[];
}

export interface FileUploadProgressOptions {
  onProgress?: (progress: number) => void;
  onThumbnailReady?: (thumbnailUrl: string) => void;
}

export function getFileCategory(mimeType?: string | null): FileCategory {
  if (!mimeType) return "unknown";
  for (const [category, types] of Object.entries(SUPPORTED_FILE_TYPES)) {
    if (types.includes(mimeType as never)) {
      return category as FileCategory;
    }
  }
  return "unknown";
}

export function getFileCategoryByName(fileName: string): FileCategory {
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
  if (previewType === "markdown") return "markdown";
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
    markdown: "markdown",
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

export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.min(Math.floor(Math.log(bytes) / Math.log(k)), sizes.length - 1);
  return `${Math.round((bytes / Math.pow(k, i)) * 100) / 100} ${sizes[i]}`;
}

export function validateFile(file: File): { valid: boolean; error?: string } {
  if (file.size > MAX_FILE_SIZE) {
    return {
      valid: false,
      error: `文件大小超过限制（最大 ${formatFileSize(MAX_FILE_SIZE)}）`,
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

function getWebPFileName(fileName: string): string {
  const normalizedName = fileName.trim() || "image";
  return /\.[^.]+$/.test(normalizedName)
    ? normalizedName.replace(/\.[^.]+$/, ".webp")
    : `${normalizedName}.webp`;
}

export async function prepareFilesForMessageUpload(
  files: File[],
): Promise<PreparedMessageUploadFiles> {
  const preparedFiles: File[] = [];
  const invalidFiles: { name: string; error: string }[] = [];
  const failedFiles: { name: string; error: string }[] = [];

  for (const file of files) {
    const validation = validateFile(file);
    if (!validation.valid) {
      invalidFiles.push({
        name: file.name,
        error: validation.error || "文件不可上传",
      });
      continue;
    }

    try {
      if (file.type.startsWith("image/")) {
        const webpFile = await convertToWebP(file, {
          quality: 1,
          output: "file",
          fileName: getWebPFileName(file.name),
        }) as File;
        preparedFiles.push(webpFile);
      } else {
        preparedFiles.push(file);
      }
    } catch (error) {
      console.error(`Failed to prepare file ${file.name}:`, error);
      failedFiles.push({
        name: file.name,
        error: error instanceof Error ? error.message : "处理失败",
      });
    }
  }

  return { files: preparedFiles, invalidFiles, failedFiles };
}

export async function uploadFileForMessage(
  conversationId: number,
  file: File,
  options: FileUploadProgressOptions = {},
): Promise<{ extra: FileExtra; messageType: MessageType }> {
  const validation = validateFile(file);
  if (!validation.valid) {
    throw new Error(validation.error);
  }

  const reportProgress = (progress: number) => {
    options.onProgress?.(Math.max(0, Math.min(100, progress)));
  };

  reportProgress(0);

  if (file.type.startsWith("video/")) {
    reportProgress(5);
    const coverFile = await extractVideoFrameToWebP(file, { quality: 1 });
    options.onThumbnailReady?.(URL.createObjectURL(coverFile));

    const totalBytes = Math.max(file.size + coverFile.size, 1);
    const uploadedBytes = {
      video: 0,
      cover: 0,
    };

    const updateAggregateProgress = () => {
      const loadedBytes = uploadedBytes.video + uploadedBytes.cover;
      const progress = Math.min(100, Math.round((loadedBytes / totalBytes) * 100));
      reportProgress(Math.max(progress, 6));
    };

    const createProgressHandler = (key: keyof typeof uploadedBytes, fallbackTotal: number) =>
      (event: AxiosProgressEvent) => {
        const total = event.total ?? fallbackTotal;
        if (!total) return;
        uploadedBytes[key] = Math.min(event.loaded, total);
        updateAggregateProgress();
      };

    const [videoInfo, coverInfo] = await Promise.all([
      uploadChatFile(conversationId, file, {
        onUploadProgress: createProgressHandler("video", file.size),
      }),
      uploadChatFile(conversationId, coverFile, {
        onUploadProgress: createProgressHandler("cover", coverFile.size),
      }),
    ]);

    reportProgress(100);

    const duration = await getMediaDuration(file);
    const extra: FileExtra = {
      fileName: file.name,
      fileSize: file.size,
      fileKey: videoInfo.data.fileKey,
      fileUrl: getChatFileUrl(videoInfo.data.fileKey),
      mimeType: file.type,
      thumbnailUrl: coverInfo.data.fileKey,
      duration: duration ?? undefined,
    };

    return { extra, messageType: "video" };
  }

  const response = await uploadChatFile(conversationId, file, {
    onUploadProgress: (event) => {
      const total = event.total ?? file.size;
      if (!total) return;
      reportProgress(Math.round((event.loaded / total) * 100));
    },
  });

  reportProgress(100);

  const fileInfo = response.data;
  const fileUrl = getChatFileUrl(fileInfo.fileKey);
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

  const fileExtra: FileExtra = {
    fileName: fileInfo.originalFileName,
    fileSize: fileInfo.fileSize,
    fileUrl,
    fileKey: fileInfo.fileKey,
    mimeType: fileInfo.contentType,
  };

  if (messageType === "image") {
    try {
      const dimensions = await getImageDimensions(file);
      if (dimensions) {
        fileExtra.width = dimensions.width;
        fileExtra.height = dimensions.height;
      }
    } catch (error) {
      console.warn("Failed to get image dimensions:", error);
    }
  }

  if (messageType === "video" || messageType === "audio") {
    try {
      const duration = await getMediaDuration(file);
      if (duration) {
        fileExtra.duration = duration;
      }
    } catch (error) {
      console.warn("Failed to get media duration:", error);
    }
  }

  return {
    extra: fileExtra,
    messageType,
  };
}

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

export function parseMessageExtra(extra?: string | null): FileExtra | null {
  if (!extra) return null;

  try {
    const raw = JSON.parse(extra) as Record<string, unknown>;

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
    if (raw.LocalPreviewUrl && !raw.localPreviewUrl) {
      raw.localPreviewUrl = raw.LocalPreviewUrl;
      delete raw.LocalPreviewUrl;
    }
    if (raw.LocalThumbnailUrl && !raw.localThumbnailUrl) {
      raw.localThumbnailUrl = raw.LocalThumbnailUrl;
      delete raw.LocalThumbnailUrl;
    }

    return raw as unknown as FileExtra;
  } catch {
    return null;
  }
}

export function getFileIcon(category: string): string {
  const icons: Record<string, string> = {
    image: "image",
    video: "video",
    audio: "audio",
    document: "doc",
    archive: "zip",
    unknown: "file",
  };
  return icons[category] || "file";
}

export async function fetchChatFile(fileKey: string): Promise<Blob> {
  if (!fileKey) {
    throw new Error("文件 Key 不能为空");
  }

  const lastDot = fileKey.lastIndexOf(".");
  const cleanFileKey = lastDot > 0 ? fileKey.substring(0, lastDot) : fileKey;
  const url = `${API_BASE_URL}/mm/files/chat/${cleanFileKey}`;

  const token = localStorage.getItem("accessToken");
  const headers = token ? { Authorization: `Bearer ${token}` } : {};

  try {
    const response = await http.get(url, {
      responseType: "blob",
      headers,
    });

    return response.data as Blob;
  } catch (error) {
    console.error("Failed to fetch chat file:", error);
    throw new Error("文件获取失败");
  }
}

export function createBlobUrl(blob: Blob): string {
  return URL.createObjectURL(blob);
}

export function revokeBlobUrl(url: string): void {
  if (url) {
    URL.revokeObjectURL(url);
  }
}
