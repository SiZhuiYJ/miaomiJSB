<!-- ConversationDetailDialog.vue -->
<script setup lang="ts">
import { computed } from 'vue';
import IsPin from './IsPin.vue';
import type { ConversationDetail } from '../types';
import {
  groupRoleMap,
  formatChatTime,
  getConversationAvatarUrl,
  getConversationDisplayTitle,
} from '../utils/chat';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia';

const props = defineProps<{
  modelValue: boolean;
  conversation: ConversationDetail;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  'update:conversation': [];
  'loadMore': [];
}>();

const { user } = storeToRefs(useAuthStore());

const title = computed(() => getConversationDisplayTitle(props.conversation, user.value?.userId));
const avatarUrl = computed(() => getConversationAvatarUrl(props.conversation));

function toggleMuted() {
  props.conversation.isMuted = !props.conversation.isMuted;
  emit('update:conversation');
}

function togglePinned() {
  props.conversation.isPinned = !props.conversation.isPinned;
  emit('update:conversation');
}
</script>

<template>
  <el-dialog :model-value="modelValue" @update:model-value="emit('update:modelValue', $event)" title="会话详情"
    width="min(92vw, 520px)">
    <el-image v-if="conversation.conversationType === 'direct'" class="avatar-preview" :src="avatarUrl" fit="fill"
      :preview-src-list="[avatarUrl]" lazy />
    <p class="members">会话标题：{{ title }}</p>
    <p class="members">会话类型：{{ conversation.conversationType === 'group' ? '群聊' : '单聊' }}</p>
    <p class="members">会话ID：{{ conversation.id }}</p>
    <p class="members">创建时间：{{ formatChatTime(conversation.createdAt) }}</p>
    <p class="members">更新时间：{{ formatChatTime(conversation.updatedAt) }}</p>
    <p class="members">
      成员：
      {{
        conversation.members
          .map((member) => `${member.nickName || member.userId}(${groupRoleMap[member.memberRole] || member.memberRole})`)
          .join('，')
      }}
    </p>
    <div class="action-icons">
      <el-icon @click="toggleMuted">
        <MuteNotification v-if="conversation.isMuted" />
        <Bell v-else />
      </el-icon>
      <IsPin v-model:is-pinned="conversation.isPinned" @toggle-pinned="togglePinned" />
    </div>
    <el-button color="#111827" class="load-more" @click="emit('loadMore')">加载更早消息</el-button>
  </el-dialog>
</template>

<style scoped lang="scss"></style>