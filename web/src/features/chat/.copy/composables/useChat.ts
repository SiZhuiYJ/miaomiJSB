import { computed, ref, watch } from "vue";
import { useAuthStore } from "@/stores";
import { storeToRefs } from "pinia";
import API from "../../api";
import type {
  ConversationDetail,
  ConversationMember,
  ConversationSummary,
  MessageReadStatus,
  MessageSummary,
} from "../types";

export function useChat() {
  const { user } = storeToRefs(useAuthStore());

  const meUserId = ref(useAuthStore().user?.userId);
  watch(
    () => user.value?.userId,
    (val) => {
      meUserId.value = val;
    },
  );

  const conversations = ref<ConversationSummary[]>([]);
  const currentConversation = ref<ConversationDetail | null>(null);
  const messages = ref<MessageSummary[]>([]);
  const loading = ref(false);
  const errorMessage = ref("");

  // 消息已读状态映射 { messageId: MessageReadStatus }
  const messageReadStatus = ref<Map<number, MessageReadStatus>>(new Map());

  const createConversationType = ref<"direct" | "group">("direct");
  const createTitle = ref("");
  const createMembersText = ref("");
  const composeText = ref("");

  const selectedConversationId = computed(
    () => currentConversation.value?.id || 0,
  );

  function getMemberDisplayName(member?: ConversationMember | null) {
    if (!member) return "";
    return (
      member.nickName?.trim() ||
      member.userAccount?.trim() ||
      String(member.userId)
    );
  }

  function getDirectPeerDisplayName(detail: ConversationDetail) {
    const currentUserId = user.value?.userId;
    const peer = detail.members.find(
      (member) => member.userId !== currentUserId,
    );
    return getMemberDisplayName(peer);
  }

  function updateConversationSummaryTitle(
    conversationId: number,
    title: string,
  ) {
    const target = conversations.value.find(
      (item) => item.id === conversationId,
    );
    if (target) target.title = title;
  }

  async function hydrateDirectConversationTitles(items: ConversationSummary[]) {
    const directItems = items.filter(
      (item) => item.conversationType === "direct",
    );
    if (directItems.length === 0) return;

    const detailResults = await Promise.allSettled(
      directItems.map((item) => API.getConversation(item.id)),
    );

    detailResults.forEach((result, index) => {
      if (result.status !== "fulfilled") return;

      const detail = result.value.data;
      const displayName = getDirectPeerDisplayName(detail);
      if (!displayName) return;

      const conversationId = directItems[index]?.id;
      if (!conversationId) return;
      updateConversationSummaryTitle(conversationId, displayName);
    });
  }

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

    const seen = new Set<number>();
    const deduped: MessageSummary[] = [];

    for (const message of merged) {
      if (seen.has(message.id)) continue;
      seen.add(message.id);
      deduped.push(message);
    }

    return deduped;
  }

  function setError(error: any, fallback: string) {
    const isTimeout = error?.code === "ECONNABORTED";
    const isNetworkError =
      error?.message === "Network Error" || !error?.response;

    if (isTimeout) {
      errorMessage.value = "请求超时，请检查网络后重试";
      return;
    }

    if (isNetworkError) {
      errorMessage.value = "无法连接服务器，请确认服务已启动并检查网络";
      return;
    }

    errorMessage.value =
      error?.response?.data?.message || error?.message || fallback;
  }

  async function loadConversations() {
    loading.value = true;
    errorMessage.value = "";
    try {
      const items = (await API.getConversations()).data;
      conversations.value = items;
      await hydrateDirectConversationTitles(items);
    } catch (error: any) {
      setError(error, "加载会话失败");
    } finally {
      loading.value = false;
    }
  }

  async function selectConversation(item: ConversationSummary) {
    loading.value = true;
    errorMessage.value = "";
    try {
      currentConversation.value = (await API.getConversation(item.id)).data;
      if (
        currentConversation.value?.conversationType === "direct" &&
        currentConversation.value
      ) {
        const directName = getDirectPeerDisplayName(currentConversation.value);
        if (directName) {
          currentConversation.value.title = directName;
          updateConversationSummaryTitle(
            currentConversation.value.id,
            directName,
          );
        }
      }
      messages.value = (await API.getMessages(item.id)).data;
      // 清除旧的已读状态
      messageReadStatus.value.clear();
      await markRead();
    } catch (error: any) {
      setError(error, "加载会话失败");
    } finally {
      loading.value = false;
    }
  }

  async function createConversation() {
    const memberUserIds = createMembersText.value
      .split(",")
      .map((x) => Number(x.trim()))
      .filter((x) => Number.isFinite(x) && x > 0);

    loading.value = true;
    errorMessage.value = "";
    try {
      const detail = (
        await API.createConversation({
          conversationType: createConversationType.value,
          title: createTitle.value || undefined,
          memberUserIds,
        })
      ).data;

      createTitle.value = "";
      createMembersText.value = "";
      await loadConversations();
      await selectConversation({
        id: detail.id,
        conversationType: detail.conversationType,
        title: detail.title,
        avatarKey: detail.avatarKey,
        avatarUserId: detail.avatarUserId,
        isActive: detail.isActive,
        isPinned: detail.isPinned,
        isMuted: detail.isMuted,
        updatedAt: detail.updatedAt,
        unreadCount: 0,
        lastMessage: null,
      });
    } catch (error: any) {
      setError(error, "创建会话失败");
    } finally {
      loading.value = false;
    }
  }
  // 更新会话信息
  async function updateConversation() {
    loading.value = true;
    errorMessage.value = "";
    try {
      if (currentConversation.value) {
        const detail = currentConversation.value;
        const { data } = await API.updateConversation(detail.id, {
          title: detail.title,
          avatarKey: detail.avatarKey,
          isActive: detail.isActive,
          isPinned: detail.isPinned,
          isMuted: detail.isMuted,
        });
        console.log(data);
        await loadConversations();
      }
    } catch (error: any) {
      setError(error, "更新会话失败");
    } finally {
      loading.value = false;
    }
  }
  async function sendTextMessage() {
    if (!selectedConversationId.value || !composeText.value.trim()) return;
    loading.value = true;
    errorMessage.value = "";
    try {
      const result = await API.sendMessage(selectedConversationId.value, {
        messageType: "text",
        content: composeText.value.trim(),
      });

      composeText.value = "";
      // 发送成功后直接追加新消息到列表，而不是拉取所有消息
      if (result.data) {
        messages.value = [...messages.value, result.data];
        // 自己发送的消息，立即加载已读状态（避免短暂显示"0/1 未读"）
        await loadMessageReadStatus(result.data.id);
      }
      await loadConversations();
    } catch (error: any) {
      setError(error, "发送消息失败");
    } finally {
      loading.value = false;
    }
  }

  async function pullLatestMessages() {
    if (!selectedConversationId.value) return;

    const lastMessage = messages.value[messages.value.length - 1];
    const lastMessageId = lastMessage?.id;

    // 如果没有消息或lastMessageId为0，重新加载完整消息列表
    if (!lastMessageId || lastMessageId === 0) {
      messages.value = (
        await API.getMessages(selectedConversationId.value)
      ).data;
      return;
    }

    const delta = (
      await API.getMessageDelta(selectedConversationId.value, lastMessageId, 50)
    ).data;

    if (delta.messages.length > 0) {
      // 严格过滤：只保留比最后消息ID更大的新消息
      const newMessages = delta.messages
        .filter((m) => m.id > lastMessageId)
        .sort((a, b) => a.id - b.id); // 确保按ID升序排列

      if (newMessages.length > 0) {
        // 简单追加到末尾，保持顺序
        messages.value = [...messages.value, ...newMessages];
      }
    }
  }

  async function loadMore() {
    if (!selectedConversationId.value || messages.value.length === 0) return;
    const firstMessage = messages.value[0];
    if (!firstMessage) return;

    const older = (
      await API.getMessages(selectedConversationId.value, firstMessage.id, 20)
    ).data;

    if (older.length > 0) {
      messages.value = mergeMessages(messages.value, older, "prepend");
    }
  }

  async function markRead() {
    if (!selectedConversationId.value) return;

    const lastId =
      messages.value.length > 0
        ? messages.value[messages.value.length - 1]?.id
        : undefined;

    await API.markRead(selectedConversationId.value, lastId);

    // 标记已读后，重新加载当前会话的已读状态
    if (lastId) {
      // 更新所有自己发送的消息的已读状态
      const myMessages = messages.value.filter(
        (m) => m.senderUserId === meUserId.value,
      );
      for (const msg of myMessages) {
        await loadMessageReadStatus(msg.id);
      }
    }

    await loadConversations();
  }

  // 处理消息已读回执（通过 SignalR 接收）
  async function handleMessageRead(data: {
    messageId: number;
    conversationId: number;
    readByUserId: number;
    readAt: string;
  }) {
    // 重新加载该消息的已读状态
    await loadMessageReadStatus(data.messageId);
  }

  // 加载单条消息的已读状态
  async function loadMessageReadStatus(messageId: number) {
    try {
      const status = (await API.getMessageReadStatus(messageId)).data;
      messageReadStatus.value.set(messageId, {
        messageId: status.messageId,
        totalRecipients: status.totalRecipients,
        readCount: status.readCount,
        readUsers: status.readUsers,
      });
      // 触发响应式更新
      messageReadStatus.value = new Map(messageReadStatus.value);
    } catch (error) {
      console.error("加载消息已读状态失败:", error);
    }
  }

  // 批量加载消息已读状态（性能优化）
  async function loadBatchMessageReadStatus(messageIds: number[]) {
    const promises = messageIds.map((id) => loadMessageReadStatus(id));
    await Promise.allSettled(promises);
  }

  return {
    meUserId,
    conversations,
    currentConversation,
    messages,
    loading,
    errorMessage,
    messageReadStatus,
    createConversationType,
    createTitle,
    createMembersText,
    composeText,
    selectedConversationId,
    loadConversations,
    selectConversation,
    createConversation,
    sendTextMessage,
    updateConversation,
    pullLatestMessages,
    loadMore,
    markRead,
    handleMessageRead,
    loadMessageReadStatus,
    loadBatchMessageReadStatus,
  };
}
