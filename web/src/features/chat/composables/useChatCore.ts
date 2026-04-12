// composables/useChatCore.ts
import { ref, watch, onBeforeUnmount, computed } from "vue";
import { useAuthStore } from "@/stores";
import { storeToRefs } from "pinia";
import { useConversations } from "./useConversations";
import { useMessages } from "./useMessages";
import { useMessageRead } from "./useMessageRead";
import { useChatPush } from "./useChatPush";
import type { MessageSummary } from "../types";

export function useChatCore() {
  const { user } = storeToRefs(useAuthStore());
  const meUserId = ref(useAuthStore().user?.userId);

  watch(
    () => user.value?.userId,
    (val) => {
      meUserId.value = val;
    },
  );

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
    pullLatestMessages: messagesModule.pullLatestMessages,
    markRead: async () => {
      const lastMsg =
        messagesModule.messages.value[messagesModule.messages.value.length - 1];
      await readModule.markRead(lastMsg?.id);
    },
    hasConversation: () => !!conversationsModule.currentConversation.value,
    getConversationId: () => conversationsModule.selectedConversationId.value,
    getToken: () => useAuthStore().accessToken || "",
    getBaseUrl: () => import.meta.env.VITE_API_BASE_URL || "",
    onMessageRead: async (data) => {
      await readModule.loadMessageReadStatus(data.messageId);
    },
  });

  // 选择会话时同步重置消息和已读状态
  const originalSelectConversation = conversationsModule.selectConversation;
  conversationsModule.selectConversation = async (item) => {
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
    const newMsg = await originalSendMessage(payload);
    if (newMsg) {
      await conversationsModule.loadConversations();
      await readModule.loadMessageReadStatus(newMsg.id);
    }
    return newMsg;
  };

  // 创建会话的表单状态
  const createConversationType = ref<"direct" | "group">("direct");
  const createTitle = ref("");
  const createMembersText = ref("");
  const composeText = ref("");

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

  async function handleSendText() {
    if (!composeText.value.trim()) return;
    await messagesModule.sendMessage({
      messageType: "text",
      content: composeText.value.trim(),
    });
    composeText.value = "";
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

    // 方法
    loadConversations: conversationsModule.loadConversations,
    selectConversation: conversationsModule.selectConversation,
    updateConversation: conversationsModule.updateConversation,
    loadMore: messagesModule.loadMore,
    sendTextMessage: handleSendText,
    markRead: () =>
      readModule.markRead(
        messagesModule.messages.value[messagesModule.messages.value.length - 1]
          ?.id,
      ),
    pullLatestMessages: messagesModule.pullLatestMessages,

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
