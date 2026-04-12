import http from '@/libs/http';
import type {
  ConversationDetail,
  ConversationSummary,
  CreateConversationPayload,
  MessageDeltaResponse,
  MessageReadStatus,
  MessageSummary,
  SendMessagePayload,
  UpdateConversation
} from '../types';

// 文件信息接口
export interface ChatFileInfo {
  fileKey: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
}

export async function getConversations() {
  return await http.get<ConversationSummary[]>('/mm/chat/conversations');
}

export async function getConversation(id: number) {
  return await http.get<ConversationDetail>(`/mm/chat/conversations/${id}`);
}

export async function createConversation(payload: CreateConversationPayload) {
  return await http.post<ConversationDetail>('/mm/chat/conversations', payload);
}
// 更新会话信息
export async function updateConversation(id: number, payload: UpdateConversation) {
  return await http.post<ConversationDetail>(`/mm/chat/conversations/${id}`, payload);
}
export async function getMessages(
  id: number,
  beforeMessageId?: number,
  pageSize = 20,
) {
  return await http.get<MessageSummary[]>(`/mm/chat/conversations/${id}/messages`, {
    params: { beforeMessageId, pageSize },
  });
}

export async function getMessageDelta(
  id: number,
  afterMessageId?: number,
  pageSize = 20,
) {
  return await http.get<MessageDeltaResponse>(
    `/mm/chat/conversations/${id}/messages/delta`,
    {
      params: { afterMessageId, pageSize },
    },
  );
}

export async function sendMessage(id: number, payload: SendMessagePayload) {
  return await http.post<MessageSummary>(`/mm/chat/conversations/${id}/messages`, payload);
}

export async function markRead(id: number, lastReadMessageId?: number) {
  return await http.post<{ lastReadMessageId?: number }>(
    `/mm/chat/conversations/${id}/read`,
    {
      lastReadMessageId,
    },
  );
}

export async function getMessageReadStatus(messageId: number) {
  return await http.get<MessageReadStatus>(`/mm/chat/messages/${messageId}/read-status`);
}

/**
 * 上传聊天文件
 * @param conversationId 会话ID
 * @param file 文件对象
 * @returns 文件信息
 */
export async function uploadChatFile(conversationId: number, file: File) {
  const formData = new FormData();
  formData.append('file', file);
  
  return await http.post<ChatFileInfo>(
    `/mm/files/chat/${conversationId}/upload`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    }
  );
}

/**
 * 获取聊天文件URL
 * @param fileKey 文件Key
 * @returns 文件访问URL
 */
export function getChatFileUrl(fileKey: string): string {
  if (!fileKey) return '';
  const baseUrl = import.meta.env.VITE_API_BASE_URL || '';
  // 确保baseUrl不以/结尾，避免双斜杠
  const normalizedBaseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
  return `${normalizedBaseUrl}/mm/files/chat/${fileKey}`;
}

/**
 * 上传会话头像
 * @param conversationId 会话ID
 * @param file 头像文件
 * @returns 文件Key
 */
export async function uploadConversationAvatar(conversationId: number, file: File) {
  const formData = new FormData();
  formData.append('file', file);
  
  return await http.post<{ key: string }>(
    `/mm/files/chat/${conversationId}/avatar`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    }
  );
}

/**
 * 获取会话头像URL
 * @param conversationId 会话ID
 * @param fileKey 头像文件Key
 * @returns 头像访问URL
 */
export function getConversationAvatarUrl(conversationId: number, fileKey: string): string {
  if (!fileKey) return '';
  const baseUrl = import.meta.env.VITE_API_BASE_URL || '';
  const normalizedBaseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
  return `${normalizedBaseUrl}/mm/files/chat/${conversationId}/avatars/${fileKey}`;
}

export default {
  getConversations,
  getConversation,
  sendMessage,
  getMessages,
  getMessageDelta,
  createConversation,
  updateConversation,
  markRead,
  getMessageReadStatus,
  uploadChatFile,
  getChatFileUrl,
  uploadConversationAvatar,
  getConversationAvatarUrl,
};
