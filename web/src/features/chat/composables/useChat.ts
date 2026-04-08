import { computed, ref, watch } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia';
import API from '../api';
import type {
  ConversationDetail,
  ConversationSummary,
  MessageSummary,
} from '../types';

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
  const errorMessage = ref('');

  const createConversationType = ref<'direct' | 'group'>('direct');
  const createTitle = ref('');
  const createMembersText = ref('');
  const composeText = ref('');

  const selectedConversationId = computed(() => currentConversation.value?.id || 0);

  function setError(error: any, fallback: string) {
    errorMessage.value =
      error?.response?.data?.message || error?.message || fallback;
  }

  async function loadConversations() {
    loading.value = true;
    errorMessage.value = '';
    try {
      conversations.value = (await API.getConversations()).data;
    } catch (error: any) {
      setError(error, '加载会话失败');
    } finally {
      loading.value = false;
    }
  }

  async function selectConversation(item: ConversationSummary) {
    loading.value = true;
    errorMessage.value = '';
    try {
      currentConversation.value = (await API.getConversation(item.id)).data;
      messages.value = (await API.getMessages(item.id)).data;
      await markRead();
    } catch (error: any) {
      setError(error, '加载会话失败');
    } finally {
      loading.value = false;
    }
  }

  async function createConversation() {
    const memberUserIds = createMembersText.value
      .split(',')
      .map((x) => Number(x.trim()))
      .filter((x) => Number.isFinite(x) && x > 0);

    loading.value = true;
    errorMessage.value = '';
    try {
      const detail = (
        await API.createConversation({
          conversationType: createConversationType.value,
          title: createTitle.value || undefined,
          memberUserIds,
        })
      ).data;

      createTitle.value = '';
      createMembersText.value = '';
      await loadConversations();
      await selectConversation({
        id: detail.id,
        conversationType: detail.conversationType,
        title: detail.title,
        avatarKey: detail.avatarKey,
        isActive: detail.isActive,
        updatedAt: detail.updatedAt,
        unreadCount: 0,
        lastMessage: null,
      });
    } catch (error: any) {
      setError(error, '创建会话失败');
    } finally {
      loading.value = false;
    }
  }

  async function sendTextMessage() {
    if (!selectedConversationId.value || !composeText.value.trim()) return;
    loading.value = true;
    errorMessage.value = '';
    try {
      await API.sendMessage(selectedConversationId.value, {
        messageType: 'text',
        content: composeText.value.trim(),
      });

      composeText.value = '';
      await pullLatestMessages();
      await loadConversations();
    } catch (error: any) {
      setError(error, '发送消息失败');
    } finally {
      loading.value = false;
    }
  }

  async function pullLatestMessages() {
    if (!selectedConversationId.value) return;

    const lastMessage = messages.value[messages.value.length - 1];
    const lastMessageId = lastMessage?.id;

    const delta = (
      await API.getMessageDelta(selectedConversationId.value, lastMessageId, 50)
    ).data;

    if (delta.messages.length > 0) {
      messages.value = [...messages.value, ...delta.messages];
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
      messages.value = [...older, ...messages.value];
    }
  }

  async function markRead() {
    if (!selectedConversationId.value) return;

    const lastId =
      messages.value.length > 0
        ? messages.value[messages.value.length - 1]?.id
        : undefined;

    await API.markRead(selectedConversationId.value, lastId);
    await loadConversations();
  }

  return {
    meUserId,
    conversations,
    currentConversation,
    messages,
    loading,
    errorMessage,
    createConversationType,
    createTitle,
    createMembersText,
    composeText,
    selectedConversationId,
    loadConversations,
    selectConversation,
    createConversation,
    sendTextMessage,
    pullLatestMessages,
    loadMore,
    markRead,
  };
}
