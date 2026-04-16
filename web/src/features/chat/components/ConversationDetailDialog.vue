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
  getMemberAvatarUrl,
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
      <span v-for="item in conversation.members" class="members-item" :key="item.userId"
        :title="item.nickName || item.userAccount || ''">
        <el-avatar class="conversation-avatar" :src="getMemberAvatarUrl(item)" :size="48" shape="square">
          {{ item.nickName ? item.nickName.slice(0, 1) : '无' }}
        </el-avatar>
        <el-text :line-clamp="1"> {{ item.nickName || item.userId }}</el-text>
        <span>({{ groupRoleMap[item.memberRole] || item.memberRole }})</span>
      </span>
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