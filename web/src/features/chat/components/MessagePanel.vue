<script setup lang="ts">
import { nextTick, ref, watch, useTemplateRef } from 'vue';
import type { ConversationDetail, MessageSummary } from '../types';

const model = defineModel<string>({ default: '' });

const props = defineProps<{
  currentConversation: ConversationDetail | null;
  messages: MessageSummary[];
  meUserId?: number;
  loading: boolean;
  showBackToList?: boolean;
}>();

const emit = defineEmits<{
  loadMore: [];
  sendTextMessage: [];
  markRead: [];
  backToList: [];
}>();

const scrollbarRef = useTemplateRef('scrollbarRef');
const isChatDetail = ref(false);
const pendingAutoScroll = ref(true);

function getScrollWrap() {
  return (scrollbarRef.value?.wrapRef as HTMLElement | undefined) ?? null;
}

function isLatestMessageInView(threshold = 24) {
  const wrap = getScrollWrap();
  if (!wrap) return true;
  const distanceToBottom = wrap.scrollHeight - (wrap.scrollTop + wrap.clientHeight);
  return distanceToBottom <= threshold;
}

function scrollToBottom(behavior: ScrollBehavior = 'auto') {
  const wrap = getScrollWrap();
  if (!wrap) return;
  wrap.scrollTo({ top: wrap.scrollHeight, behavior });
}

watch(
  () => props.messages.length,
  async (newLength, oldLength) => {
    const hasNewMessage = newLength > oldLength;
    if (hasNewMessage) {
      pendingAutoScroll.value = isLatestMessageInView();
    }

    await nextTick();

    if (hasNewMessage && pendingAutoScroll.value) {
      scrollToBottom('smooth');
    }
  },
  { flush: 'pre' },
);

watch(
  () => props.currentConversation?.id,
  async () => {
    await nextTick();
    scrollToBottom();
  },
);
</script>

<template>
  <main class="main-panel">
    <div v-if="props.currentConversation" class="conversation-detail">

      <el-scrollbar ref="scrollbarRef" wrap-style="" view-class="">
        <div class="message-list">
          <div v-for="msg in props.messages" :key="msg.id"
            :class="['msg-item', { mine: props.meUserId && msg.senderUserId === props.meUserId }]">
            <div class="meta">
              #{{ msg.id }} · {{ msg.senderNickName || msg.senderUserId }} · {{ msg.messageType }}
            </div>
            <div class="bubble">{{ msg.content || msg.extra || '[空消息]' }}</div>
          </div>
        </div>
      </el-scrollbar>
      <div class="chat-header">
        <div class="chat-header-main">
          <el-button v-if="props.showBackToList" color="#111827" class="back-to-list" @click="emit('backToList')">
            会话列表
          </el-button>
          <h3>{{ props.currentConversation.title || `会话 #${props.currentConversation.id}` }}</h3>
        </div>

        <el-button color="#111827" class="open-detail" @click="isChatDetail = true">
          详情
        </el-button>
      </div>
      <div class="composer">
        <el-input v-model="model" clearable placeholder="输入消息" @keyup.enter="emit('sendTextMessage')" />
        <el-button color="#111827" :disabled="props.loading" @click="emit('sendTextMessage')">发送</el-button>
        <el-button color="#111827" :disabled="props.loading" @click="emit('markRead')">
          标为已读
        </el-button>
      </div>

      <el-dialog v-model="isChatDetail" title="会话详情" width="min(92vw, 520px)">
        <p class="members">
          成员：
          {{
            props.currentConversation.members
              .map((member) => `${member.nickName || member.userId}(${member.memberRole})`)
              .join('，')
          }}
        </p>
        <el-button color="#111827" class="load-more" @click="emit('loadMore')">加载更早消息</el-button>
      </el-dialog>
    </div>

    <div v-else class="empty">请选择一个会话开始聊天。</div>
  </main>
</template>
<style lang="scss" scoped></style>
