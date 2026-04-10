<script setup lang="ts">
import { nextTick, ref, watch, useTemplateRef, computed } from 'vue';
import type { ConversationDetail, MessageSummary, ConversationMember } from '../types';
import { useDebounceFn } from '@/utils/debounce'
import { API_BASE_URL } from '@/config';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia'
const { user } = storeToRefs(useAuthStore());
import SvgIcon from '@/components/SvgIcon/index.vue'
import IsPin from './IsPin.vue'

const model = defineModel<string>({ default: '' });
const currentConversation = defineModel<ConversationDetail>('conversationDetail')

const props = defineProps<{
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
  updateConversation: [];
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
function toggleMuted() {
  console.log('切换状态')
  if (currentConversation.value) {
    currentConversation.value.isMuted = !currentConversation.value.isMuted;
    updateMuted(currentConversation.value.isMuted)
  }
}
const { debounced: updateMuted } = useDebounceFn(
  async (val: boolean) => {
    console.log('调用函数', val)
    emit('updateConversation');
  },
  500
);
function togglePinned() {
  console.log('切换状态')
  if (currentConversation.value) {
    currentConversation.value.isPinned = !currentConversation.value.isPinned;
    updatePinned(currentConversation.value.isPinned)
  }
}
const { debounced: updatePinned } = useDebounceFn(
  async (val: boolean) => {
    console.log('调用函数', val)
    emit('updateConversation');
  },
  500
);

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
  () => currentConversation.value?.id,
  async () => {
    await nextTick();
    scrollToBottom();
  },
);

/**
* 从双元素数组中排除指定 id 的用户，返回另一个用户
* @param users 长度为 2 的用户数组
* @param targetId 要剔除的用户 id
* @returns 另一个用户对象，若不存在则返回 null
*/
// export 
function getOtherUser(users: ConversationMember[], targetId: number | string): ConversationMember | null {
  const other = users.find(user => user.userId !== targetId);
  return other ?? null;
}

const directPeer = computed(() => {
  if (!currentConversation.value || currentConversation.value.conversationType !== 'direct' || !user.value) return null;
  return getOtherUser(currentConversation.value.members, user.value.userId);
});

const url = computed(() => {
  if (!directPeer.value?.avatarKey) return '';
  return `${API_BASE_URL}/mm/Files/users/${directPeer.value.userId}/${directPeer.value.avatarKey}`;
});

function formatChatTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getMemberAvatarBySender(senderUserId: number) {
  const member = currentConversation.value?.members.find((item) => item.userId === senderUserId);
  if (!member?.avatarKey) return '';
  return `${API_BASE_URL}/mm/Files/users/${member.userId}/${member.avatarKey}`;
}


</script>

<template>
  <main class="main-panel">
    <div v-if="currentConversation" class="conversation-detail" >
      <el-scrollbar ref="scrollbarRef" wrap-style="" view-class="">
        <div class="message-list">
          <div v-for="msg in props.messages" :key="msg.id"
            :class="['msg-item', { mine: props.meUserId && msg.senderUserId === props.meUserId }]">
            <div class="meta">
              <el-avatar class="message-avatar" :src="getMemberAvatarBySender(msg.senderUserId)">
                {{ (msg.senderNickName || String(msg.senderUserId)).slice(0, 1) }}
              </el-avatar>
              <span>#{{ msg.id }} · {{ msg.senderNickName || msg.senderUserId }} · {{ msg.messageType }}</span>
              <span>{{ formatChatTime(msg.createdAt) }}</span>
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
          <h3>{{ currentConversation.title || `会话 #${currentConversation.id}` }}</h3>
        </div>

        <el-button color="#111827" class="open-detail" icon="Document" @click="isChatDetail = true">
          详情
        </el-button>
      </div>
      <div class="composer">
        <el-input v-model="model" clearable placeholder="输入消息" @keyup.enter="emit('sendTextMessage')" />
        <el-button color="#111827" :disabled="props.loading" @click="emit('sendTextMessage')">
          <svg-icon class="bind-icon" icon-class="general-enter" color="red" size="1rem" />发送
        </el-button>
        <el-button color="#111827" :disabled="props.loading" @click="emit('markRead')">
          标为已读
        </el-button>
      </div>

      <el-dialog v-model="isChatDetail" title="会话详情" width="min(92vw, 520px)">
        <el-image v-if="currentConversation.conversationType == 'direct'" class="avatar-preview" :src="url" fit="fill"
          :preview-src-list="[url]" lazy />
        <p class="members">会话类型：{{ currentConversation.conversationType === 'group' ? '群聊' : '单聊' }}</p>
        <p class="members">会话ID：{{ currentConversation.id }}</p>
        <p class="members">创建时间：{{ formatChatTime(currentConversation.createdAt) }}</p>
        <p class="members">更新时间：{{ formatChatTime(currentConversation.updatedAt) }}</p>
        <p class="members">
          成员：
          {{
            currentConversation.members
              .map((member) => `${member.nickName || member.userId}(${member.memberRole})`)
              .join('，')
          }}
        </p>
        <el-icon @click="toggleMuted">
          <MuteNotification v-if="currentConversation.isMuted" />
          <Bell v-else />
        </el-icon>

        <is-pin :is-pinned="currentConversation.isPinned" @toggle-pinned="togglePinned" />

        <!-- <el-switch v-model="currentConversation.isMuted" inline-prompt active-text="置顶" inactive-text="取消" /> -->

        <el-button color="#111827" class="load-more" @click="emit('loadMore')">加载更早消息</el-button>
      </el-dialog>
    </div>

    <div v-else class="empty">请选择一个会话开始聊天。</div>
  </main>
</template>
<style lang="scss" scoped></style>
