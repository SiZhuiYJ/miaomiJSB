<script setup lang="ts">
import { nextTick, ref, watch } from 'vue';
import type { ConversationDetail, MessageSummary } from '../types';

const props = defineProps<{
  currentConversation: ConversationDetail | null;
  messages: MessageSummary[];
  meUserId?: number;
  composeText: string;
  loading: boolean;
}>();

const emit = defineEmits<{
  loadMore: [];
  sendTextMessage: [];
  markRead: [];
  updateComposeText: [value: string];
}>();

const scrollBodyRef = ref<HTMLElement | null>(null);
const isChatDetail = ref<boolean>(false)

watch(
  () => props.messages.length,
  async () => {
    await nextTick();
    if (!scrollBodyRef.value) return;
    scrollBodyRef.value.scrollTop = scrollBodyRef.value.scrollHeight;
  },
);
</script>

<template>
  <main class="main-panel">
    <div v-if="props.currentConversation" class="conversation-detail">
      <div class="chat-header">
        <h3>{{ props.currentConversation.title || `会话 #${props.currentConversation.id}` }}</h3>

        <el-button class="open-detail" @click="() => { isChatDetail = true }" icon="more" link />
      </div>

      <div ref="scrollBodyRef" class="message-scroll-wrap">
        <div class="message-list">
          <div v-for="msg in props.messages" :key="msg.id"
            :class="['msg-item', { mine: props.meUserId && msg.senderUserId === props.meUserId }]">
            <div class="meta">#{{ msg.id }} · {{ msg.senderNickName || msg.senderUserId }} · {{ msg.messageType }}</div>
            <div class="bubble">{{ msg.content || msg.extra || '[空消息]' }}</div>
          </div>
        </div>
      </div>

      <div class="composer">
        <input :value="props.composeText" placeholder="输入文本消息" @keyup.enter="emit('sendTextMessage')"
          @input="emit('updateComposeText', ($event.target as HTMLInputElement).value)" />
        <button :disabled="props.loading" @click="emit('sendTextMessage')">发送</button>
        <button :disabled="props.loading" @click="emit('markRead')">标记已读</button>
      </div>
      <el-dialog v-model="isChatDetail">
        <p class="members">
          成员：{{
            props.currentConversation.members
              .map((m) => `${m.nickName || m.userId}(${m.memberRole})`)
              .join('、')
          }}
        </p>
        <button class="load-more" @click="emit('loadMore')">加载更早消息</button>
      </el-dialog>
    </div>

    <div v-else class="empty">请选择会话或新建会话</div>
  </main>

</template>
