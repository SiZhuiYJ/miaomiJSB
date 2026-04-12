// utils/chat.ts
import { API_BASE_URL } from "@/config";
import type {
  ConversationSummary,
  ConversationDetail,
  ConversationMember,
} from "../types";

/**
 * 格式化聊天时间
 */
export function formatChatTime(value: string): string {
  return new Date(value).toLocaleString("zh-CN", {
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

/**
 * 获取成员显示名称
 */
export function getMemberDisplayName(
  member?: ConversationMember | null,
): string {
  if (!member) return "";
  return (
    member.nickName?.trim() ||
    member.userAccount?.trim() ||
    String(member.userId)
  );
}

/**
 * 获取单聊对方的成员信息
 */
export function getDirectPeer(
  detail: ConversationDetail | null,
  currentUserId?: number,
): ConversationMember | null {
  if (!detail || detail.conversationType !== "direct" || !currentUserId)
    return null;
  return detail.members.find((m) => m.userId !== currentUserId) ?? null;
}

/**
 * 获取会话显示标题
 */
export function getConversationDisplayTitle(
  item: ConversationSummary | ConversationDetail,
  currentUserId?: number,
): string {
  if (item.conversationType === "direct") {
    // 对于详情，优先使用已有 title 或计算对方昵称
    if ("members" in item) {
      const peer = getDirectPeer(item, currentUserId);
      return peer ? getMemberDisplayName(peer) : `单聊 #${item.id}`;
    }
    // 对于摘要，后端可能已设置 title
    return (
      item.title || item.userAccount || String(item.avatarUserId || item.id)
    );
  }
  return item.title || `群聊 #${item.id}`;
}

/**
 * 获取会话头像 URL
 */
export function getConversationAvatarUrl(
  item: ConversationSummary | ConversationDetail,
): string {
  const avatarUserId = item.avatarUserId;
  const avatarKey = item.avatarKey;
  if (!avatarKey || !avatarUserId) return "";
  return `${API_BASE_URL}/mm/Files/users/${avatarUserId}/${avatarKey}`;
}

/**
 * 获取会话头像占位文字（首字母）
 */
export function getConversationAvatarText(
  item: ConversationSummary | ConversationDetail,
): string {
  if (item.conversationType === "direct") {
    return getConversationDisplayTitle(item).slice(0, 1);
  }
  return (item.title || `群#${item.id}`).slice(0, 1);
}

/**
 * 获取成员头像 URL
 */
export function getMemberAvatarUrl(member: ConversationMember): string {
  if (!member.avatarKey) return "";
  return `${API_BASE_URL}/mm/Files/users/${member.userId}/${member.avatarKey}`;
}

/**
 * 根据发送者 ID 获取会话中对应成员的头像 URL
 */
export function getMemberAvatarBySender(
  detail: ConversationDetail,
  senderUserId: number,
): string {
  const member = detail.members.find((m) => m.userId === senderUserId);
  return member ? getMemberAvatarUrl(member) : "";
}

/**
 * 获取消息预览文本
 */
export function getMessagePreview(item: ConversationSummary): string {
  if (!item.lastMessage) return "暂无消息";
  if (item.lastMessage.content) return item.lastMessage.content;
  return `[${item.lastMessage.messageType}]`;
}
