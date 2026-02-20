<script setup lang="ts">
// 2495550774@qq.com
import { ref, onMounted } from "vue";
import { ElConfigProvider, dayjs } from "element-plus";
import { setNotificationInstance } from "./utils/notification";
// @ts-ignore
import zhCn from "element-plus/dist/locale/zh-cn.mjs";
// @ts-ignore
dayjs.en.weekStart = 1;

const notificationSystemRef = ref(null);

onMounted(() => {
  setNotificationInstance(notificationSystemRef.value);
});
</script>

<template>
  <!-- Element Plus全局配置组件 -->
  <el-config-provider :locale="zhCn">
    <!-- 路由视图容器 -->
    <router-view></router-view>
    <NotificationSystem ref="notificationSystemRef" />
  </el-config-provider>
</template>
