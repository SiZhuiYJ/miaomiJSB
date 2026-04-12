// composables/useMessageRead.ts
import { ref } from "vue";
import API from "../api";
import type { MessageReadStatus, MessageSummary } from "../types";

export function useMessageRead(
  conversationId: () => number,
  meUserId: () => number | undefined,
) {
  const messageReadStatus = ref<Map<number, MessageReadStatus>>(new Map());

  async function loadMessageReadStatus(messageId: number) {
    try {
      const status = (await API.getMessageReadStatus(messageId)).data;
      messageReadStatus.value.set(messageId, status);
      // 触发响应式更新
      messageReadStatus.value = new Map(messageReadStatus.value);
    } catch (error) {
      console.error("加载消息已读状态失败:", error);
    }
  }

  async function loadBatchMessageReadStatus(messageIds: number[]) {
    const promises = messageIds.map((id) => loadMessageReadStatus(id));
    await Promise.allSettled(promises);
  }

  async function markRead(lastMessageId?: number) {
    const convId = conversationId();
    if (!convId) return;
    await API.markRead(convId, lastMessageId);
  }

  // 获取某条消息的已读显示文本（用于 MessageItem）
  function getReadInfo(message: MessageSummary) {
    const userId = meUserId();
    if (!userId || message.senderUserId !== userId) {
      return { readText: "", readColor: "" };
    }
    const status = messageReadStatus.value.get(message.id);
    if (!status) return { readText: "", readColor: "#909399" };

    const readCount = status.readCount;
    const total = status.totalRecipients;
    const isAllRead = readCount >= total;

    return {
      readText: `${readCount}/${total} 已读`,
      readColor: isAllRead ? "#67C23A" : "#909399",
    };
  }

  function clearReadStatus() {
    messageReadStatus.value.clear();
  }

  return {
    messageReadStatus,
    loadMessageReadStatus,
    loadBatchMessageReadStatus,
    markRead,
    getReadInfo,
    clearReadStatus,
  };
}
