<script setup lang="ts">
import type { ConversationSummary } from '../types';

const createConversationType = defineModel<'direct' | 'group'>('createConversationType');
const createTitle = defineModel<string>('createTitle');
const createMembersText = defineModel<string>('createMembersText');

const props = defineProps<{
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
    <div class="create-box">
      <h3>创建会话</h3>
      <el-select v-model="createConversationType" placeholder="会话类型" class="conversation-type-select">
        <el-option
          v-for="item in createConversationTypeOptions"
          :key="item.value"
          :label="item.label"
          :value="item.value"
        />
      </el-select>
      <el-input v-model="createTitle" clearable placeholder="群聊标题，可选" />
      <el-input v-model="createMembersText" clearable placeholder="成员 ID，使用逗号分隔" />
      <el-button color="#111827" @click="emit('createConversation')">创建</el-button>
    </div>

    <ul class="conversation-list">
      <li
        v-for="item in props.conversations"
        :key="item.id"
        :class="{ active: item.id === props.selectedConversationId }"
        @click="emit('selectConversation', item)"
      >
        <div class="title-row">
          <strong>{{ item.title || `会话 #${item.id}` }}</strong>
          <span v-if="item.unreadCount" class="badge">{{ item.unreadCount }}</span>
        </div>
        <small>{{ item.lastMessage?.content || item.lastMessage?.messageType || '暂无消息' }}</small>
      </li>
    </ul>
  </aside>
</template>