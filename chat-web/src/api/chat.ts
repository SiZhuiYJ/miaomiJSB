import axios from 'axios';
import type {
  ConversationDetail,
  ConversationSummary,
  CreateConversationPayload,
  MessageSummary,
  SendMessagePayload,
} from '../types';

export function createChatApi(baseURL: string, token: string) {
  const client = axios.create({ baseURL, timeout: 15000 });

  client.interceptors.request.use((config) => {
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  });

  return {
    async getConversations() {
      const { data } = await client.get<ConversationSummary[]>('/mm/chat/conversations');
      return data;
    },
    async getConversation(id: number) {
      const { data } = await client.get<ConversationDetail>(`/mm/chat/conversations/${id}`);
      return data;
    },
    async createConversation(payload: CreateConversationPayload) {
      const { data } = await client.post<ConversationDetail>('/mm/chat/conversations', payload);
      return data;
    },
    async getMessages(id: number, beforeMessageId?: number, pageSize = 20) {
      const { data } = await client.get<MessageSummary[]>(`/mm/chat/conversations/${id}/messages`, {
        params: { beforeMessageId, pageSize },
      });
      return data;
    },
    async sendMessage(id: number, payload: SendMessagePayload) {
      const { data } = await client.post<MessageSummary>(`/mm/chat/conversations/${id}/messages`, payload);
      return data;
    },
    async markRead(id: number, lastReadMessageId?: number) {
      const { data } = await client.post<{ lastReadMessageId?: number }>(`/mm/chat/conversations/${id}/read`, {
        lastReadMessageId,
      });
      return data;
    },
  };
}
