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
    <el-button color="#111827" @click="emit('back')">返回</el-button>
    <el-input :value="statusText" placeholder="连接状态" disabled />
    <el-button color="#111827" @click="emit('refresh')">刷新</el-button>
    <el-button color="#111827" :disabled="loading" @click="emit('togglePush')">
      {{ polling ? '停止推送' : '开启推送' }}
    </el-button>
  </header>
</template>