<script setup lang="ts">
import { watch } from 'vue';
import { API_BASE_URL } from '@/config';
import { useAuthStore } from '@/stores';
import router from '@/routers';
import ChatToolbar from './ChatToolbar.vue';
import ConversationSidebar from './ConversationSidebar.vue';
import MessagePanel from './MessagePanel.vue';
import { useChat } from '../composables/useChat';
import { useChatPush } from '../composables/useChatPush';

const chat = useChat();

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

chat.loadConversations();
void push.startPush();
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

    <div class="layout">
      <ConversationSidebar
        :create-conversation-type="chat.createConversationType.value"
        :create-title="chat.createTitle.value"
        :create-members-text="chat.createMembersText.value"
        :selected-conversation-id="chat.selectedConversationId.value"
        :conversations="chat.conversations.value"
        @update-create-conversation-type="(v) => (chat.createConversationType.value = v)"
        @update-create-title="(v) => (chat.createTitle.value = v)"
        @update-create-members-text="(v) => (chat.createMembersText.value = v)"
        @create-conversation="chat.createConversation"
        @select-conversation="chat.selectConversation"
      />

      <MessagePanel
        :current-conversation="chat.currentConversation.value"
        :messages="chat.messages.value"
        :me-user-id="chat.meUserId.value"
        :compose-text="chat.composeText.value"
        :loading="chat.loading.value"
        @update-compose-text="(v) => (chat.composeText.value = v)"
        @load-more="chat.loadMore"
        @send-text-message="chat.sendTextMessage"
        @mark-read="chat.markRead"
      />
    </div>
  </div>
</template>
