<script setup lang="ts">
defineProps<{
  statusText: string;
  polling: boolean;
  loading: boolean;
}>();

const emit = defineEmits<{
  refresh: [];
  togglePush: [];
  back: [];
}>();
</script>

<template>
  <header class="toolbar">
    <button @click="emit('back')">返回</button>
    <div class="push-status">{{ statusText }}</div>
    <button @click="emit('refresh')">刷新</button>
    <button :disabled="loading" @click="emit('togglePush')">
      {{ polling ? '停止推送' : '开启推送' }}
    </button>
  </header>
</template>
