import http from "@/libs/http";
import type {
  ConversationDetail,
  ConversationSummary,
  CreateConversationPayload,
  InviteConversationMembersPayload,
  MuteConversationMemberPayload,
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

export async function inviteConversationMembers(
  conversationId: number,
  payload: InviteConversationMembersPayload,
) {
  return await http.post<ConversationDetail>(
    `/mm/chat/conversations/${conversationId}/members/invite`,
    payload,
  );
}

export async function kickConversationMember(
  conversationId: number,
  memberUserId: number,
) {
  return await http.post<ConversationDetail>(
    `/mm/chat/conversations/${conversationId}/members/${memberUserId}/kick`,
  );
}

export async function muteConversationMember(
  conversationId: number,
  memberUserId: number,
  payload: MuteConversationMemberPayload,
) {
  return await http.post<ConversationDetail>(
    `/mm/chat/conversations/${conversationId}/members/${memberUserId}/mute`,
    payload,
  );
}

export async function unmuteConversationMember(
  conversationId: number,
  memberUserId: number,
) {
  return await http.post<ConversationDetail>(
    `/mm/chat/conversations/${conversationId}/members/${memberUserId}/unmute`,
  );
}

export async function disbandConversation(conversationId: number) {
  return await http.post<ConversationDetail>(
    `/mm/chat/conversations/${conversationId}/disband`,
  );
}
