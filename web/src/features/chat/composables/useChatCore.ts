// composables/useChatCore.ts
import { ref, watch, onBeforeUnmount, computed } from "vue";
import { useAuthStore } from "@/stores";
import { storeToRefs } from "pinia";
import { useConversations } from "./useConversations";
import { useMessages } from "./useMessages";
import { useMessageRead } from "./useMessageRead";
import { useChatPush } from "./useChatPush";
import type { MessageSummary, SendMessagePayload } from "../types";

export function useChatCore() {
  const { user } = storeToRefs(useAuthStore());
  const meUserId = ref(useAuthStore().user?.userId);

  watch(
    () => user.value?.userId,
    (val) => {
      meUserId.value = val;
    },
  );

  const replyingMessage = ref<MessageSummary | null>(null);

  function setReplyingMessage(message: MessageSummary) {
    replyingMessage.value = message;
  }

  function clearReplyingMessage() {
    replyingMessage.value = null;
  }

  // 子模块
  const conversationsModule = useConversations();
  const messagesModule = useMessages(
    () => conversationsModule.selectedConversationId.value,
  );
  const readModule = useMessageRead(
    () => conversationsModule.selectedConversationId.value,
    () => meUserId.value,
  );

  // 组合 loading / error
  const loading = computed(
    () => conversationsModule.loading.value || messagesModule.loading.value,
  );
  const errorMessage = computed(
    () =>
      conversationsModule.errorMessage.value ||
      messagesModule.errorMessage.value,
  );

  // 推送模块（依赖以上模块的方法）
  const pushModule = useChatPush({
    fetchConversations: conversationsModule.loadConversations,
    syncCurrentMessages: messagesModule.refreshLoadedMessages,
    markRead: async () => {
      const lastMsg =
        messagesModule.messages.value[messagesModule.messages.value.length - 1];
      await readModule.markRead(lastMsg?.id);
    },
    hasConversation: () => !!conversationsModule.currentConversation.value,
    getConversationId: () => conversationsModule.selectedConversationId.value,
    getToken: () => useAuthStore().accessToken || "",
    getBaseUrl: () => import.meta.env.VITE_API_BASE_URL || "",
    upsertMessage: async (message) => {
      messagesModule.upsertMessage(message);
    },
    onMessageRead: async (data) => {
      await readModule.loadMessageReadStatus(data.messageId);
    },
    getAllConversationIds: () =>
      conversationsModule.conversations.value.map((c) => c.id),
  });

  // 选择会话时同步重置消息和已读状态
  const originalSelectConversation = conversationsModule.selectConversation;
  conversationsModule.selectConversation = async (item) => {
    clearReplyingMessage();
    messagesModule.resetMessages();
    readModule.clearReadStatus();
    const detail = await originalSelectConversation(item);
    if (detail) {
      await messagesModule.loadMessages();
      await readModule.markRead(
        messagesModule.messages.value[messagesModule.messages.value.length - 1]
          ?.id,
      );
      await pushModule.subscribeConversation(detail.id);
      // 加载自己发送的消息的已读状态
      const myMsgIds = messagesModule.messages.value
        .filter((m) => m.senderUserId === meUserId.value)
        .map((m) => m.id);
      if (myMsgIds.length)
        await readModule.loadBatchMessageReadStatus(myMsgIds);
    }
    return detail;
  };

  // 发送消息后自动刷新会话列表和已读状态
  const originalSendMessage = messagesModule.sendMessage;
  messagesModule.sendMessage = async (payload) => {
    const effectivePayload: SendMessagePayload = {
      ...payload,
      replyToMessageId: payload.replyToMessageId ?? replyingMessage.value?.id ?? null,
    };

    const newMsg = await originalSendMessage(effectivePayload);
    if (newMsg) {
      if (effectivePayload.replyToMessageId) {
        clearReplyingMessage();
      }

      void Promise.allSettled([
        conversationsModule.loadConversations(),
        readModule.loadMessageReadStatus(newMsg.id),
      ]);
    }
    return newMsg;
  };

  // 创建会话的表单状态
  const createConversationType = ref<"direct" | "group">("direct");
  const createTitle = ref("");
  const createMembersText = ref("");
  const composeText = ref("");

  watch(
    () => conversationsModule.selectedConversationId.value,
    (conversationId, previousConversationId) => {
      if (conversationId !== previousConversationId) {
        clearReplyingMessage();
      }
    },
  );

  watch(
    () => messagesModule.messages.value,
    (messages) => {
      if (!replyingMessage.value) return;
      const latestReplyTarget = messages.find(
        (message) => message.id === replyingMessage.value?.id,
      );
      if (!latestReplyTarget || latestReplyTarget.isRecalled) {
        clearReplyingMessage();
        return;
      }
      replyingMessage.value = latestReplyTarget;
    },
  );

  async function handleCreateConversation() {
    const memberUserIds = createMembersText.value
      .split(",")
      .map((x) => Number(x.trim()))
      .filter((x) => Number.isFinite(x) && x > 0);
    const detail = await conversationsModule.createConversation({
      conversationType: createConversationType.value,
      title: createTitle.value || undefined,
      memberUserIds,
    });
    if (detail) {
      createTitle.value = "";
      createMembersText.value = "";
    }
  }

  async function handleSendText(replyToMessageId?: number | null) {
    const content = composeText.value.trim();
    if (!content) return;

    const sentMessage = await messagesModule.sendMessage({
      messageType: "text",
      content,
      replyToMessageId: replyToMessageId ?? replyingMessage.value?.id ?? null,
    });

    if (sentMessage) {
      composeText.value = "";
    }
  }

  async function handleRecallMessage(message: MessageSummary) {
    const recalledMessage = await messagesModule.recallMessage(message.id);
    if (recalledMessage) {
      if (replyingMessage.value?.id === recalledMessage.id) {
        clearReplyingMessage();
      }
      await conversationsModule.loadConversations();
    }
    return recalledMessage;
  }

  // 在 useChatCore 中添加
  const togglePush = (force?: boolean) => {
    if (force === true) pushModule.startPush();
    else if (force === false) pushModule.stopPush();
    else pushModule.togglePush();
  };
  onBeforeUnmount(() => {
    pushModule.stopPush();
  });

  return {
    // 状态
    meUserId,
    loading,
    errorMessage,
    conversations: conversationsModule.conversations,
    currentConversation: conversationsModule.currentConversation,
    selectedConversationId: conversationsModule.selectedConversationId,
    messages: messagesModule.messages,
    messageReadStatus: readModule.messageReadStatus,
    replyingMessage,

    // 方法
    loadConversations: conversationsModule.loadConversations,
    selectConversation: conversationsModule.selectConversation,
    updateConversation: conversationsModule.updateConversation,
    loadMore: messagesModule.loadMore,
    sendTextMessage: handleSendText,
    sendMessage: messagesModule.sendMessage,
    recallMessage: handleRecallMessage,
    markRead: () =>
      readModule.markRead(
        messagesModule.messages.value[messagesModule.messages.value.length - 1]
          ?.id,
      ),
    pullLatestMessages: messagesModule.pullLatestMessages,
    setReplyingMessage,
    clearReplyingMessage,

    // 推送相关
    polling: pushModule.polling,
    realtimeConnected: pushModule.realtimeConnected,
    statusText: pushModule.statusText,
    togglePush,

    // 表单
    createConversationType,
    createTitle,
    createMembersText,
    composeText,
    handleCreateConversation,

    // 已读状态辅助方法
    getReadInfo: readModule.getReadInfo,
    loadMessageReadStatus: readModule.loadMessageReadStatus, // ✅ 添加
    loadBatchMessageReadStatus: readModule.loadBatchMessageReadStatus, // ✅ 添加
  };
}
