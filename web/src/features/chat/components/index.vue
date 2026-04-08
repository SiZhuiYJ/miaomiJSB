<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { API_BASE_URL } from '@/config';
import { useAuthStore } from '@/stores';
import router from '@/routers';
import ChatToolbar from './ChatToolbar.vue';
import ConversationSidebar from './ConversationSidebar.vue';
import MessagePanel from './MessagePanel.vue';
import { useChat } from '../composables/useChat';
import { useChatPush } from '../composables/useChatPush';
import type { ConversationSummary } from '../types';

const chat = useChat();
const isMobile = ref(false);

function syncViewport() {
  isMobile.value = window.innerWidth <= 768;
}

const showConversationList = computed(() => {
  if (!isMobile.value) return true;
  return !chat.selectedConversationId.value;
});

const push = useChatPush({
  fetchConversations: chat.loadConversations,
  pullLatestMessages: chat.pullLatestMessages,
  markRead: chat.markRead,
  hasConversation: () => !!chat.selectedConversationId.value,
  getConversationId: () => chat.selectedConversationId.value,
  getToken: () => useAuthStore().accessToken || '',
  getBaseUrl: () => API_BASE_URL,
});

watch(
  () => chat.selectedConversationId.value,
  (conversationId) => {
    void push.subscribeConversation(conversationId);
  },
);

async function handleSelectConversation(item: ConversationSummary) {
  await chat.selectConversation(item);
}

function handleBackToList() {
  chat.currentConversation.value = null;
  chat.messages.value = [];
}

onMounted(() => {
  syncViewport();
  window.addEventListener('resize', syncViewport);
  void chat.loadConversations();
  void push.startPush();
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', syncViewport);
});
</script>

<template>
  <div class="chat-page">
    <ChatToolbar
      :status-text="push.statusText.value"
      :polling="push.polling.value || push.realtimeConnected.value"
      :loading="chat.loading.value"
      @back="router.push('/home')"
      @refresh="chat.loadConversations"
      @toggle-push="push.togglePush"
    />

    <p v-if="chat.errorMessage.value" class="error">{{ chat.errorMessage.value }}</p>

    <div class="layout" :class="{ mobile: isMobile }">
      <ConversationSidebar
        v-if="showConversationList"
        v-model:create-conversation-type="chat.createConversationType.value"
        v-model:create-title="chat.createTitle.value"
        v-model:create-members-text="chat.createMembersText.value"
        :selected-conversation-id="chat.selectedConversationId.value"
        :conversations="chat.conversations.value"
        @create-conversation="chat.createConversation"
        @select-conversation="handleSelectConversation"
      />

      <MessagePanel
        v-if="!isMobile || !showConversationList"
        v-model="chat.composeText.value"
        :current-conversation="chat.currentConversation.value"
        :messages="chat.messages.value"
        :me-user-id="chat.meUserId.value"
        :loading="chat.loading.value"
        :show-back-to-list="isMobile"
        @load-more="chat.loadMore"
        @send-text-message="chat.sendTextMessage"
        @mark-read="chat.markRead"
        @back-to-list="handleBackToList"
      />
    </div>
  </div>
</template>
