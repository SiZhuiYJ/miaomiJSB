<script setup lang="ts">
import type { ConversationSummary } from '../types';

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
</script>

<template>
  <aside class="left-panel">
    <el-scrollbar ref="scrollbarRef" wrap-style="" view-class="">
      <ul class="conversation-list">
        <li v-for="item in props.conversations" :key="item.id"
          :class="{ active: item.id === props.selectedConversationId }" @click="emit('selectConversation', item)">
          <div class="title-row">
            <strong>{{ item.title || `会话 #${item.id}` }}</strong>
            <span v-if="item.unreadCount" class="badge">{{ item.unreadCount }}</span>
          </div>
          <el-text line-clamp="1">{{ item.lastMessage?.content || item.lastMessage?.messageType || '暂无消息' }}</el-text>
        </li>
      </ul>
    </el-scrollbar>
    
    <div class="create-box">
      <div class="create-box-item">
        <h3>创建会话</h3>
        <el-text v-if="errorMessage" type="danger">{{ errorMessage }}</el-text>
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
        <el-button color="#111827" @click="emit('createConversation')">创建</el-button>
      </div>
    </div>
  </aside>
</template>