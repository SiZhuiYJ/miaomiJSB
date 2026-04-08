<script setup lang="ts">
import type { ConversationSummary } from '../types';

const props = defineProps<{
  createConversationType: 'direct' | 'group';
  createTitle: string;
  createMembersText: string;
  selectedConversationId: number;
  conversations: ConversationSummary[];
}>();

const emit = defineEmits<{
  updateCreateConversationType: [value: 'direct' | 'group'];
  updateCreateTitle: [value: string];
  updateCreateMembersText: [value: string];
  createConversation: [];
  selectConversation: [item: ConversationSummary];
}>();
</script>

<template>
  <aside class="left-panel">
    <div class="create-box">
      <h3>创建会话</h3>
      <select
        :value="props.createConversationType"
        @change="emit('updateCreateConversationType', ($event.target as HTMLSelectElement).value as 'direct' | 'group')"
      >
        <option value="direct">direct 双人</option>
        <option value="group">group 多人</option>
      </select>
      <input
        :value="props.createTitle"
        placeholder="群聊标题（direct 可不填）"
        @input="emit('updateCreateTitle', ($event.target as HTMLInputElement).value)"
      />
      <input
        :value="props.createMembersText"
        placeholder="成员ID，逗号分隔：8,11"
        @input="emit('updateCreateMembersText', ($event.target as HTMLInputElement).value)"
      />
      <button @click="emit('createConversation')">创建</button>
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
