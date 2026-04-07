import { defineStore } from 'pinia';
import { ref } from 'vue';
import { fetchChatMessages, sendMessage, type ChatMessage } from '../api/chat';

export const useChatStore = defineStore('chat', () => {
  const messages = ref<ChatMessage[]>([]);
  const loading = ref(false);

  async function loadHistory() {
    loading.value = true;
    try {
      messages.value = await fetchChatMessages();
    } finally {
      loading.value = false;
    }
  }

  async function submit(content: string) {
    const value = content.trim();
    if (!value) {
      return;
    }

    loading.value = true;
    try {
      const data = await sendMessage({ content: value });
      messages.value = data.history;
    } finally {
      loading.value = false;
    }
  }

  return {
    messages,
    loading,
    loadHistory,
    submit,
  };
});
