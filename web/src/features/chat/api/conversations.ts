import http from "@/libs/http";
import type {
  ConversationDetail,
  ConversationSummary,
  CreateConversationPayload,
  UpdateConversation,
  UpdateConversationMemberRolePayload,
} from "../types";

export async function getConversations() {
  return await http.get<ConversationSummary[]>("/mm/chat/conversations");
}

export async function getConversation(id: number) {
  return await http.get<ConversationDetail>(`/mm/chat/conversations/${id}`);
}

export async function createConversation(payload: CreateConversationPayload) {
  return await http.post<ConversationDetail>("/mm/chat/conversations", payload);
}

export async function updateConversation(id: number, payload: UpdateConversation) {
  return await http.post<ConversationDetail>(`/mm/chat/conversations/${id}`, payload);
}

export async function updateConversationMemberRole(
  conversationId: number,
  memberUserId: number,
  payload: UpdateConversationMemberRolePayload,
) {
  return await http.post<ConversationDetail>(
    `/mm/chat/conversations/${conversationId}/members/${memberUserId}/role`,
    payload,
  );
}
