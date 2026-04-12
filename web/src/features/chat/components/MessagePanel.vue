<!-- MessagePanel.vue -->
<script setup lang="ts">
import { nextTick, watch, useTemplateRef, computed, ref } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia';
import MessageItem from './MessageItem.vue';
import ConversationDetailDialog from './ConversationDetailDialog.vue';
import type { ConversationDetail, MessageSummary, MessageReadStatus } from '../types';
import {
  formatChatTime,
  getMemberAvatarBySender,
  getConversationDisplayTitle,
} from '../utils/chat';

const model = defineModel<string>({ default: '' });
const currentConversation = defineModel<ConversationDetail>('conversationDetail');

const props = defineProps<{
  messages: MessageSummary[];
  meUserId?: number;
  loading: boolean;
  showBackToList?: boolean;
  messageReadStatus?: Map<number, MessageReadStatus>;
  readInfoMap?: Map<number, { readText: string; readColor: string }>; // 由 useChatCore 提供
}>();

const emit = defineEmits<{
  loadMore: [];
  sendTextMessage: [];
  markRead: [];
  backToList: [];
  updateConversation: [];
  loadMessageReadStatus: [messageId: number];
}>();

const { user } = storeToRefs(useAuthStore());
const scrollbarRef = useTemplateRef('scrollbarRef');
const isChatDetail = ref(false);
const pendingAutoScroll = ref(true);

// 会话标题
const conversationHeaderTitle = computed(() =>
  currentConversation.value
    ? getConversationDisplayTitle(currentConversation.value, user.value?.userId)
    : ''
);

function getScrollWrap() {
  return (scrollbarRef.value?.wrapRef as HTMLElement | undefined) ?? null;
}

function isLatestMessageInView(threshold = 24) {
  const wrap = getScrollWrap();
  if (!wrap) return true;
  return wrap.scrollHeight - (wrap.scrollTop + wrap.clientHeight) <= threshold;
}

function scrollToBottom(behavior: ScrollBehavior = 'auto') {
  const wrap = getScrollWrap();
  if (!wrap) return;
  wrap.scrollTo({ top: wrap.scrollHeight, behavior });
}

// 监听消息变化，自动滚动
watch(
  () => props.messages.length,
  async (newLength, oldLength) => {
    if (newLength > oldLength) {
      pendingAutoScroll.value = isLatestMessageInView();
    }
    await nextTick();
    if (newLength > oldLength && pendingAutoScroll.value) {
      scrollToBottom('smooth');
    }
  }
);

watch(
  () => currentConversation.value?.id,
  async () => {
    await nextTick();
    scrollToBottom();
  }
);

// 消息进入视图时加载已读状态
function onMessageVisible(messageId: number) {
  if (!props.messageReadStatus?.has(messageId)) {
    emit('loadMessageReadStatus', messageId);
  }
}

// 获取消息的已读展示信息（优先使用预计算的 readInfoMap）
function getReadDisplay(messageId: number) {
  if (props.readInfoMap) {
    return props.readInfoMap.get(messageId) || { readText: '', readColor: '#909399' };
  }
  // 降级处理（兼容旧用法）
  const status = props.messageReadStatus?.get(messageId);
  if (!status || status.totalRecipients === 0) return { readText: '', readColor: '#909399' };
  const isAllRead = status.readCount >= status.totalRecipients;
  return {
    readText: `${status.readCount}/${status.totalRecipients} 已读`,
    readColor: isAllRead ? '#67C23A' : '#909399',
  };
}
</script>

<template>
  <main class="main-panel">
    <div v-if="currentConversation" class="conversation-detail">
      <el-scrollbar ref="scrollbarRef" view-class="message-list-container">
        <div class="message-list" v-if="props.meUserId">
          <MessageItem v-for="msg in props.messages" :key="msg.id" @vue:mounted="onMessageVisible(msg.id)"
            :message="msg" :src="getMemberAvatarBySender(currentConversation, msg.senderUserId)"
            :meUserId="props.meUserId" :createdAt="formatChatTime(msg.createdAt)"
            :is-mine="msg.senderUserId === props.meUserId" v-bind="getReadDisplay(msg.id)" />
        </div>
      </el-scrollbar>

      <div class="chat-header">
        <div class="chat-header-main">
          <el-button v-if="props.showBackToList" color="#111827" class="back-to-list" @click="emit('backToList')">
            会话列表
          </el-button>
          <h3>{{ conversationHeaderTitle }}</h3>
        </div>
        <el-button color="#111827" class="open-detail" icon="Document" @click="isChatDetail = true">
          详情
        </el-button>
      </div>

      <div class="composer">
        <el-input v-model="model" clearable placeholder="输入消息" @keyup.enter="emit('sendTextMessage')" />
        <el-button color="#111827" :disabled="props.loading" @click="emit('sendTextMessage')">
          发送
        </el-button>
        <el-button color="#111827" :disabled="props.loading" @click="emit('markRead')">
          标为已读
        </el-button>
      </div>

      <ConversationDetailDialog v-model="isChatDetail" :conversation="currentConversation"
        @update:conversation="emit('updateConversation')" @load-more="emit('loadMore')" />
    </div>

    <div v-else class="empty">请选择一个会话开始聊天。</div>
  </main>
</template>

<style scoped lang="scss">
/* 样式根据实际需求补充，此处仅保留结构 */
// .main-panel {
//   flex: 1;
//   display: flex;
//   flex-direction: column;
//   height: 100%;
// }

// .conversation-detail {
//   display: flex;
//   flex-direction: column;
//   height: 100%;
// }

// .message-list {
//   padding: 16px;
// }

// .chat-header {
//   display: flex;
//   justify-content: space-between;
//   align-items: center;
//   padding: 12px 16px;
//   border-bottom: 1px solid #ebeef5;
// }

// .chat-header-main {
//   display: flex;
//   align-items: center;
//   gap: 12px;
// }

// .composer {
//   display: flex;
//   gap: 12px;
//   padding: 16px;
//   border-top: 1px solid #ebeef5;
// }

// .empty {
//   display: flex;
//   justify-content: center;
//   align-items: center;
//   height: 100%;
//   color: #909399;
// }
</style>