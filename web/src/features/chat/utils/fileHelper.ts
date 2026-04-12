import { uploadChatFile, getChatFileUrl } from '../api';
import type { FileExtra } from '../types';

// 文件大小限制 (100MB)
const MAX_FILE_SIZE = 100 * 1024 * 1024;

// 支持的文件类型
export const SUPPORTED_FILE_TYPES = {
  image: ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/bmp', 'image/svg+xml'],
  video: ['video/mp4', 'video/webm', 'video/ogg', 'video/quicktime', 'video/x-msvideo', 'video/x-ms-wmv'],
  audio: ['audio/mpeg', 'audio/wav', 'audio/ogg', 'audio/mp4', 'audio/flac', 'audio/aac'],
  document: [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    'application/vnd.ms-powerpoint',
    'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    'text/plain',
    'text/markdown',
    'application/rtf',
    'text/csv'
  ],
  archive: [
    'application/zip',
    'application/x-rar-compressed',
    'application/x-7z-compressed',
    'application/x-tar',
    'application/gzip',
    'application/x-bzip2'
  ]
};

/**
 * 获取文件类型分类
 */
export function getFileCategory(mimeType: string): 'image' | 'video' | 'audio' | 'document' | 'archive' | 'unknown' {
  for (const [category, types] of Object.entries(SUPPORTED_FILE_TYPES)) {
    if (types.includes(mimeType)) {
      return category as any;
    }
  }
  return 'unknown';
}

/**
 * 根据文件名获取文件类型
 */
export function getFileCategoryByName(fileName: string): 'image' | 'video' | 'audio' | 'document' | 'archive' | 'unknown' {
  const ext = fileName.split('.').pop()?.toLowerCase();
  
  const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'svg', 'ico'];
  const videoExts = ['mp4', 'avi', 'mov', 'wmv', 'flv', 'mkv', 'webm', 'm4v'];
  const audioExts = ['mp3', 'wav', 'ogg', 'm4a', 'flac', 'aac', 'wma', 'opus'];
  const docExts = ['pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt', 'rtf', 'csv', 'md'];
  const archiveExts = ['zip', 'rar', '7z', 'tar', 'gz', 'bz2'];

  if (!ext) return 'unknown';
  if (imageExts.includes(ext)) return 'image';
  if (videoExts.includes(ext)) return 'video';
  if (audioExts.includes(ext)) return 'audio';
  if (docExts.includes(ext)) return 'document';
  if (archiveExts.includes(ext)) return 'archive';
  
  return 'unknown';
}

/**
 * 格式化文件大小
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
}

/**
 * 验证文件
 */
export function validateFile(file: File): { valid: boolean; error?: string } {
  if (file.size > MAX_FILE_SIZE) {
    return { valid: false, error: `文件大小超过限制（最大${formatFileSize(MAX_FILE_SIZE)}）` };
  }
  
  if (file.size === 0) {
    return { valid: false, error: '文件不能为空' };
  }

  const category = getFileCategory(file.type);
  if (category === 'unknown') {
    return { valid: false, error: '不支持的文件类型' };
  }

  return { valid: true };
}

/**
 * 上传图片/视频/音频等文件并生成消息extra
 */
export async function uploadFileForMessage(
  conversationId: number,
  file: File
): Promise<{ extra: FileExtra; messageType: string }> {
  // 验证文件
  const validation = validateFile(file);
  if (!validation.valid) {
    throw new Error(validation.error);
  }

  // 上传文件
  const response = await uploadChatFile(conversationId, file);
  const fileInfo = response.data;

  // 构建文件URL
  const fileUrl = getChatFileUrl(fileInfo.fileKey);
  
  // 确定消息类型
  const category = getFileCategory(fileInfo.contentType);
  let messageType: string;
  
  switch (category) {
    case 'image':
      messageType = 'image';
      break;
    case 'video':
      messageType = 'video';
      break;
    case 'audio':
      messageType = 'audio';
      break;
    default:
      messageType = 'file';
  }

  // 构建FileExtra对象
  const fileExtra: FileExtra = {
    fileName: fileInfo.originalFileName,
    fileSize: fileInfo.fileSize,
    fileUrl: fileUrl,
    fileKey: fileInfo.fileKey,
    mimeType: fileInfo.contentType
  };

  // 对于图片，尝试获取尺寸
  if (messageType === 'image') {
    try {
      const dimensions = await getImageDimensions(file);
      if (dimensions) {
        fileExtra.width = dimensions.width;
        fileExtra.height = dimensions.height;
      }
    } catch (e) {
      console.warn('Failed to get image dimensions:', e);
    }
  }

  // 对于视频/音频，可以尝试获取时长
  if (messageType === 'video' || messageType === 'audio') {
    try {
      const duration = await getMediaDuration(file);
      if (duration) {
        fileExtra.duration = duration;
      }
    } catch (e) {
      console.warn('Failed to get media duration:', e);
    }
  }

  return {
    extra: fileExtra,
    messageType
  };
}

/**
 * 获取图片尺寸
 */
function getImageDimensions(file: File): Promise<{ width: number; height: number } | null> {
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
    const video = document.createElement('video');
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
    
    video.preload = 'metadata';
    video.src = url;
  });
}

/**
 * 解析消息的extra字段
 */
export function parseMessageExtra(extra?: string | null): FileExtra | null {
  if (!extra) return null;
  try {
    return JSON.parse(extra) as FileExtra;
  } catch {
    return null;
  }
}

/**
 * 获取文件图标（用于文档和压缩包）
 */
/**
 * 获取文件图标（用于文档和压缩包）
 */
export function getFileIcon(category: string): string {
  const icons: Record<string, string> = {
    image: '🖼️',
    video: '🎬',
    audio: '🎵',
    document: '📄',
    archive: '📦',
    unknown: '📎'
  };
  // Use type assertion to avoid TS error
  return (icons as any)[category] || '📎';
}
