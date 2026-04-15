<!-- ConversationSidebar.vue -->
<script setup lang="ts">
import type { ConversationSummary } from '../types';
import {
  getConversationAvatarUrl,
  getConversationAvatarText,
  getConversationDisplayTitle,
  getMessagePreview,
} from '../utils/chat';

const createConversationType = defineModel<'direct' | 'group'>('createConversationType');
const createTitle = defineModel<string>('createTitle');
const createMembersText = defineModel<string>('createMembersText');

defineProps<{
  errorMessage: string;
  selectedConversationId: number;
  conversations: ConversationSummary[];
  isMobile: boolean;
}>();

const emit = defineEmits<{
  createConversation: [];
  selectConversation: [item: ConversationSummary];
}>();

const createConversationTypeOptions = [
  { value: 'direct', label: '单聊' },
  { value: 'group', label: '群聊' },
];
</script>

<template>
  <aside class="left-panel">
    <el-scrollbar :view-class="isMobile ? 'mobile' : ''">
      <ul class="conversation-list">
        <li v-for="item in conversations" :key="item.id" class="conversation-item"
          :class="{ active: item.id === selectedConversationId }" @click="emit('selectConversation', item)">
          <el-avatar class="conversation-avatar" :src="getConversationAvatarUrl(item)" :size="60" shape="square">
            {{ getConversationAvatarText(item) }}
          </el-avatar>
          <el-badge :value="item.unreadCount" :show-zero="false" :max="99" :offset="[-22, 5]"
            :color="item.isMuted ? '#f5f7fa' : ''" class="item">
            <div class="title-row">
              <strong>{{ getConversationDisplayTitle(item) }}</strong>
              <el-icon v-if="item.isPinned" class="pinned-icon">
                <Top />
              </el-icon>
              <el-icon v-if="item.isMuted" class="muted-icon">
                <MuteNotification />
              </el-icon>
            </div>
            <el-text line-clamp="1">
              {{ item.lastMessage?.senderNickName || item.lastMessage?.senderUserId || '系统' }}：
              {{ getMessagePreview(item) }}
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
        <el-button icon="Plus" color="#111827">添加好友</el-button>
      </div>
      <div class="create-box-item">
        <el-select v-model="createConversationType" placeholder="会话类型" class="conversation-type-select">
          <el-option v-for="item in createConversationTypeOptions" :key="item.value" :label="item.label"
            :value="item.value" />
        </el-select>
        <el-input v-model="createTitle" clearable placeholder="群聊标题，可选"
          :disabled="createConversationType === 'direct'" />
      </div>
      <div class="create-box-item">
        <el-input v-model="createMembersText" clearable placeholder="成员 ID，使用逗号分隔" />
        <el-button icon="ChatRound" color="#111827" @click="emit('createConversation')">创建</el-button>
      </div>
    </div>
  </aside>
</template>

<style scoped lang="scss">

</style>