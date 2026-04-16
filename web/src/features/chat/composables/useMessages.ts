// composables/useMessages.ts
import { ref } from "vue";
import API from "../api";
import { useErrorHandler } from "./useErrorHandler";
import type { MessageSummary, SendMessagePayload } from "../types";

export function useMessages(conversationId: () => number) {
  const messages = ref<MessageSummary[]>([]);
  const loading = ref(false);
  const { errorMessage, setError, clearError } = useErrorHandler();

  // 消息去重合并工具
  function mergeMessages(
    current: MessageSummary[],
    incoming: MessageSummary[],
    mode: "append" | "prepend" = "append",
  ) {
    if (incoming.length === 0) return current;
    const merged =
      mode === "prepend"
        ? [...incoming, ...current]
        : [...current, ...incoming];
    const deduped = new Map<number, MessageSummary>();
    for (const msg of merged) {
      deduped.set(msg.id, msg);
    }
    return Array.from(deduped.values()).sort((a, b) => a.id - b.id);
  }

  function upsertMessage(message: MessageSummary) {
    messages.value = mergeMessages(messages.value, [message]);
  }

  async function loadMessages(beforeId?: number, limit = 50) {
    const convId = conversationId();
    if (!convId) return;
    loading.value = true;
    clearError();
    try {
      const data = (await API.getMessages(convId, beforeId, limit)).data;
      if (!beforeId) {
        messages.value = data.sort((a, b) => a.id - b.id);
      }
      return data;
    } catch (error: any) {
      setError(error, "加载消息失败");
    } finally {
      loading.value = false;
    }
  }

  async function loadMore() {
    const convId = conversationId();
    if (!convId || messages.value.length === 0) return;
    const firstId = messages.value[0]?.id;
    if (!firstId) return;
    const older = await loadMessages(firstId, 20);
    if (older && older.length > 0) {
      messages.value = mergeMessages(messages.value, older, "prepend");
    }
  }

  async function pullLatestMessages() {
    const convId = conversationId();
    if (!convId) return;
    const lastMsg = messages.value[messages.value.length - 1];
    const lastId = lastMsg?.id;

    if (!lastId) {
      await loadMessages();
      return;
    }

    try {
      const delta = (await API.getMessageDelta(convId, lastId, 50)).data;
      if (delta.messages.length > 0 && lastId) {
        const newMessages = delta.messages
          .filter(m => m.id > lastId)
          .sort((a, b) => a.id - b.id);
        if (newMessages.length) {
          messages.value = mergeMessages(messages.value, newMessages);
        }
      }
    } catch (error: any) {
      setError(error, "获取新消息失败");
    }
  }

  async function refreshLoadedMessages() {
    const convId = conversationId();
    if (!convId) return;

    try {
      let remaining = Math.max(messages.value.length, 50);
      let beforeMessageId: number | undefined;
      let refreshedMessages: MessageSummary[] = [];

      while (remaining > 0) {
        const pageSize = Math.min(remaining, 100);
        const data = (await API.getMessages(convId, beforeMessageId, pageSize)).data;

        if (data.length === 0) {
          break;
        }

        refreshedMessages = [...data, ...refreshedMessages];
        beforeMessageId = data[0]?.id;
        remaining -= data.length;

        if (data.length < pageSize) {
          break;
        }
      }

      messages.value = refreshedMessages.sort((a, b) => a.id - b.id);
    } catch (error: any) {
      setError(error, "刷新消息失败");
    }
  }

  async function sendMessage(payload: SendMessagePayload) {
    const convId = conversationId();
    if (!convId) return null;
    loading.value = true;
    clearError();
    try {
      const result = await API.sendMessage(convId, payload);
      if (result.data) {
        messages.value = mergeMessages(messages.value, [result.data]);
      }
      return result.data;
    } catch (error: any) {
      setError(error, "发送消息失败");
      return null;
    } finally {
      loading.value = false;
    }
  }

  async function recallMessage(messageId: number) {
    loading.value = true;
    clearError();
    try {
      const result = await API.recallMessage(messageId);
      if (result.data) {
        upsertMessage(result.data);
      }
      return result.data;
    } catch (error: any) {
      setError(error, "撤回消息失败");
      return null;
    } finally {
      loading.value = false;
    }
  }

  // 清空消息（切换会话时调用）
  function resetMessages() {
    messages.value = [];
  }

  return {
    messages,
    loading,
    errorMessage,
    loadMessages,
    loadMore,
    pullLatestMessages,
    refreshLoadedMessages,
    sendMessage,
    recallMessage,
    upsertMessage,
    resetMessages,
  };
}
