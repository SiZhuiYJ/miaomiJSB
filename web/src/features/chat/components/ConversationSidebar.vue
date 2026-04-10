<script setup lang="ts">
import type { ConversationSummary } from '../types';
import { API_BASE_URL } from '@/config';
const createConversationType = defineModel<'direct' | 'group'>('createConversationType');
const createTitle = defineModel<string>('createTitle');
const createMembersText = defineModel<string>('createMembersText');

const props = defineProps<{
  errorMessage: string;
  selectedConversationId: number;
  conversations: ConversationSummary[];
}>();

const emit = defineEmits<{
  createConversation: [];
  selectConversation: [item: ConversationSummary];
}>();

const createConversationTypeOptions = [
  { value: 'direct', label: '单聊' },
  { value: 'group', label: '群聊' },
];
function getConversationAvatar(item: ConversationSummary) {
  if (item.conversationType === 'direct') {
    if (!item.avatarKey || !item.avatarUserId) return '';
    return `${API_BASE_URL}/mm/Files/users/${item.avatarUserId}/${item.avatarKey}`;
  }
  if (!item.avatarKey || !item.avatarUserId) return '';
  return `${API_BASE_URL}/mm/Files/users/${item.avatarUserId}/${item.avatarKey}`;
}

function getConversationAvatarText(item: ConversationSummary) {
  if (item.conversationType === 'direct') return (item.title || `#${item.id}`).slice(0, 1);
  return (item.title || `群#${item.id}`).slice(0, 1);
}

function getConversationMessagePreview(item: ConversationSummary) {
  if (!item.lastMessage) return '暂无消息';
  if (item.lastMessage.content) return item.lastMessage.content;
  return `[${item.lastMessage.messageType}]`;
}
</script>

<template>
  <aside class="left-panel">
    <el-scrollbar ref="scrollbarRef" wrap-style="" view-class="">
      <ul class="conversation-list">
        <li v-for="item in props.conversations" :key="item.id" class="conversation-item"
          :class="{ active: item.id === props.selectedConversationId }" @click="emit('selectConversation', item)">
          <el-avatar class="conversation-avatar" :src="getConversationAvatar(item)">
            {{ getConversationAvatarText(item) }}
          </el-avatar>
          <el-badge :value="item.unreadCount" :show-zero="false" :max="99" :offset="[-22, 5]"
            :color="item.isMuted ? '#f5f7fa' : ''" class="item">
            <div class="title-row">
              <strong>{{ item.title || `会话 #${item.id}` }}</strong>
              <el-icon v-if="item.isMuted" class="muted-icon">
                <MuteNotification />
              </el-icon>
            </div>
            <el-text line-clamp="1">
              {{ item.lastMessage?.senderNickName || item.lastMessage?.senderUserId || '系统' }}：
              {{ getConversationMessagePreview(item) }}
            </el-text>
            <template #content="{ value }">
              <div class="custom-content">
                <el-icon>
                  <Message />
                </el-icon>
                <span>{{ value }}</span>
              </div>
            </template>
          </el-badge>
        </li>
      </ul>
    </el-scrollbar>

    <div class="create-box">
      <div class="create-box-item">
        <h3>创建会话</h3>
        <el-button icon="plus" color="#111827" @click="() => { }">添加好友</el-button>
      </div>
      <div class="create-box-item">
        <el-select v-model="createConversationType" placeholder="会话类型" class="conversation-type-select">
          <el-option v-for="item in createConversationTypeOptions" :key="item.value" :label="item.label"
            :value="item.value" />
        </el-select>
        <el-input v-model="createTitle" clearable placeholder="群聊标题，可选"
          :disabled="createConversationType == 'direct'" />
      </div>
      <div class="create-box-item">
        <el-input v-model="createMembersText" clearable placeholder="成员 ID，使用逗号分隔" />
        <el-button icon="ChatRound" color="#111827" @click="emit('createConversation')">创建</el-button>
      </div>
    </div>
  </aside>
</template>
