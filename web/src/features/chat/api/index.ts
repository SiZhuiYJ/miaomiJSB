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
};
