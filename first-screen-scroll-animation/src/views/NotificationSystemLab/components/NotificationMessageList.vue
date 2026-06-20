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
  <Teleport to="body">
    <div class="notification-container">
      <MessageItem v-for="(msg, index) in activeMessages" :key="msg.id" :message="msg" :index="index"
        @remove="emit('remove', $event)" @set-item-ref="(...args) => emit('setItemRef', ...args)" />

      <div v-if="messages.length > 5" class="hidden-count-badge">
        + {{ messages.length - 5 }} 条未显示通知
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.notification-container {
  --item-height: 44px;
  --item-gap: 8px;
  --main-radius: 22px;
  --slate-50: #f8fafc;
  --slate-100: #f1f5f9;
  --slate-200: #e2e8f0;
  --slate-300: #cbd5e1;
  --slate-400: #94a3b8;
  --slate-500: #64748b;
  --slate-800: #1e293b;
  --slate-900: #0f172a;
  --blue-500: #3b82f6;
  --blue-600: #2563eb;
  --blue-700: #1d4ed8;
  --rose-50: #fff1f2;
  --rose-600: #e11d48;
  --emerald-50: #ecfdf5;
  --emerald-600: #059669;
  --purple-50: #f5f3ff;
  --purple-600: #7c3aed;

  position: absolute;
  top: 0;
  display: flex;
  justify-content: center;
  z-index: 9999;
  width: 100%;
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