import { http } from './http';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  sentAt: string;
}

export interface SendMessageRequest {
  content: string;
}

export interface SendMessageResponse {
  reply: ChatMessage;
  history: ChatMessage[];
}

export async function fetchChatMessages() {
  const { data } = await http.get<ChatMessage[]>('/mm/chat/messages');
  return data;
}

export async function sendMessage(payload: SendMessageRequest) {
  const { data } = await http.post<SendMessageResponse>('/mm/chat/messages', payload);
  return data;
}
