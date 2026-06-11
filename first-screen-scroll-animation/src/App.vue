<script setup lang="ts">
import GsapView from './views/gsap/index.vue';
import LoadingView from './views/loading/index.vue';
import bubbles from './views/bubbles/index.vue';
import message from './views/message/index.vue';
import { useTemplateRef, onMounted } from "vue";
import {
  setNotificationInstance,
  notify,
  notifySuccess,
  notifyWarning,
  notifyError,
} from "./views/NotificationSystem/notification";

import NotificationSystem from "./views/NotificationSystem/index.vue";
import NotificationSystemLab from "./views/NotificationSystemLab/index.vue";

const notificationSystemRef = useTemplateRef("notificationSystemRef");
onMounted(async () => {

  setNotificationInstance(notificationSystemRef.value);

  // 测试通知系统（减少测试数量以避免性能问题）
  notify({
    content: "欢迎使用每日打卡系统",
    color: "#10b981",
    duration: 30000,
  });

  // 延迟测试不同类型的通知
  setTimeout(() => {
    notifySuccess("登录成功！");
  }, 2000);
  setTimeout(() => {
    notifyWarning("请注意打卡时间");
  }, 4000);
  setTimeout(() => {
    notifyError("网络连接异常");
  }, 6000);
});
</script>

<template>
  <!-- <message /> -->
  <!-- <GsapView /> -->
  <LoadingView />
  <!-- <bubbles /> -->
  <NotificationSystem ref="notificationSystemRef" />
  <NotificationSystemLab />
</template>