<script setup lang="ts">
// 2495550774@qq.com
import { computed, ref, onMounted } from 'vue';
import { useAuthStore } from './stores/auth';
import AuthView from './views/AuthView.vue';
import DashboardView from './views/DashboardView.vue';
import { ElConfigProvider, dayjs } from 'element-plus'
import { setNotificationInstance } from './utils/notification';
// @ts-ignore
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
// @ts-ignore
dayjs.en.weekStart = 1
const auth = useAuthStore();

const isAuthenticated = computed(() => auth.isAuthenticated);

const notificationSystemRef = ref(null);

onMounted(() => {
  setNotificationInstance(notificationSystemRef.value);
});
</script>

<template>
  <el-config-provider :locale="zhCn">
    <NotificationSystem ref="notificationSystemRef" />
    <AuthView v-if="!isAuthenticated" />
    <DashboardView v-else />
  </el-config-provider>
</template>
