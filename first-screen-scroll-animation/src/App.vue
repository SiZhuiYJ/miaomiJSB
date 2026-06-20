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
} from "./views/NotificationSystemLab/NotificationMessage.ts";

import NotificationSystemLab from "./views/NotificationSystemLab/index.vue";

const notificationSystemRef = useTemplateRef("notificationSystemLabRef");
onMounted(async () => {

  setNotificationInstance(notificationSystemRef.value);

  // 测试通知系统（减少测试数量以避免性能问题）
  notify({
    content: "欢迎使用每日打卡系统",
    color: "#10b981",
    duration: 300000,
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

  setTimeout(() => {
    notify({
      content: "欢迎使用每日打卡系统",
      color: "#10b981",
      duration: 300000,
    });
  }, 6000)
  setTimeout(() => {
    notify({
      content: "欢迎使用系统0",
      color: "#10b121",
      duration: 3000,
    });
    notify({
      content: "欢迎使用系统1",
      color: "#10b121",
      duration: 30000,
    });
    notify({
      content: "欢迎使用系统2",
      color: "#10b121",
      duration: 6000,
    });
    notify({
      content: "欢迎使用系统3",
      color: "#10b121",
      duration: 60000,
    });
  }, 8000);
});
</script>

<template>
  <message />
  <!-- <GsapView /> -->
  <LoadingView />
  <!-- <bubbles /> -->
  <NotificationSystemLab ref="notificationSystemLabRef" />
</template>