<script setup lang="ts">
import MessageItem from "./messageItem.vue";
import type { NotificationMessage } from "../types";

interface Props {
  messages: NotificationMessage[];
  activeMessages: NotificationMessage[];
}

defineProps<Props>();

const emit = defineEmits<{
  remove: [id: number];
  setItemRef: [el: HTMLElement | null, id: number];
}>();
</script>

<template>
  <div class="notification-container">
    <MessageItem
      v-for="(msg, index) in activeMessages"
      :key="msg.id"
      :message="msg"
      :index="index"
      @remove="emit('remove', $event)"
      @set-item-ref="(...args) => emit('setItemRef', ...args)"
    />

    <div v-if="messages.length > 5" class="hidden-count-badge">
      + {{ messages.length - 5 }} 条未显示通知
    </div>
  </div>
</template>

<style scoped lang="scss">
.notification-container {
  position: fixed;
  top: env(safe-area-inset-top, 20px);
  left: 50%;
  transform: translateX(-50%);
  z-index: 9999;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  pointer-events: none;
  padding: 12px;
  box-sizing: border-box;
}

.hidden-count-badge {
  font-size: 10px;
  color: var(--slate-400);
  margin-top: 0.5rem;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 9999px;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
}
</style>
