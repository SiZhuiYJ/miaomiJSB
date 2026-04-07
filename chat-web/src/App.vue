<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useChatStore } from './stores/chat';

const store = useChatStore();
const { messages, loading } = storeToRefs(store);
const input = ref('');

onMounted(() => {
  store.loadHistory();
});

const handleSubmit = async () => {
  await store.submit(input.value);
  input.value = '';
};
</script>

<template>
  <main class="chat-page">
    <h1>聊天演示</h1>

    <section class="chat-panel">
      <p v-if="!messages.length" class="empty">暂无聊天记录，发一条消息开始吧。</p>
      <div v-for="(item, idx) in messages" :key="`${item.sentAt}-${idx}`" class="message" :class="item.role">
        <strong>{{ item.role === 'user' ? '我' : '助手' }}：</strong>{{ item.content }}
      </div>
    </section>

    <form class="input-row" @submit.prevent="handleSubmit">
      <input v-model="input" type="text" placeholder="请输入消息" :disabled="loading" />
      <button type="submit" :disabled="loading">发送</button>
    </form>
  </main>
</template>
