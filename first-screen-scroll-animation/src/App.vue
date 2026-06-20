<script setup lang="ts">
import GsapView from './views/gsap/index.vue';
import LoadingView from './views/loading/index.vue';
import bubbles from './views/bubbles/index.vue';
import message from './views/message/index.vue';
import NotificationControlPanel from './views/notificationControlPanel/index.vue';
import { useTemplateRef, onMounted, reactive } from "vue";
import type { NotificationForm, NotificationDirectionOption } from "./views/NotificationSystemLab/types";
import {
  setNotificationInstance,
  notify,
  notifySuccess,
  notifyWarning,
  notifyError,
} from "./views/NotificationSystemLab/NotificationMessage.ts";

import NotificationSystemLab from "./views/NotificationSystemLab/index.vue";

const form = reactive<NotificationForm>({
  content: "新增：纵向双向收拢动画",
  color: "#f43f5e",
  duration: 5000,
  closable: true,
  direction: "vSplit",
});

const directions: NotificationDirectionOption[] = [
  { n: "左向右收", v: "ltr" },
  { n: "右向左收", v: "rtl" },
  { n: "上向下收", v: "ttb" },
  { n: "下向上收", v: "btt" },
  { n: "横向双收", v: "center" },
  { n: "纵向双收", v: "vSplit" },
  { n: "波纹推移", v: "ripple" },
  { n: "聚光灯", v: "spotlight" },
  { n: "柔和消融", v: "fade" },
];

const addMessage = () => {
  notify({
    content: form.content,
    color: form.color,
    duration: form.duration,
    closable: form.closable,
    direction: form.direction,
  });
}

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
  <NotificationControlPanel v-model="form" :directions="directions" @submit="addMessage" />
  <NotificationSystemLab ref="notificationSystemLabRef" />
</template>