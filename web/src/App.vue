<script setup lang="ts">
// 2495550774@qq.com
import { ref, onMounted } from "vue";
import { ElConfigProvider, dayjs } from "element-plus";
import { setNotificationInstance } from "@/utils/notification";
import NotificationSystem from "@/components/NotificationSystem/index.vue";
import { notify, notifySuccess, notifyWarning, notifyError } from "@/utils/notification";
// @ts-ignore
import zhCn from "element-plus/dist/locale/zh-cn.mjs";
// @ts-ignore
dayjs.en.weekStart = 1;

const notificationSystemRef = ref(null);
// 初始化主题
function initTheme(): void {
  const saved = localStorage.getItem("theme");
  let next: "dark" | "light" = "light";
  if (saved === "light" || saved === "dark") {
    next = saved;
  }
  document.documentElement.setAttribute("data-theme", next);
  if (next === "dark") {
    document.documentElement.classList.add("dark");
  } else {
    document.documentElement.classList.remove("dark");
  }
}

onMounted(async () => {
  setNotificationInstance(notificationSystemRef.value);
  initTheme();
  
  // 测试通知系统（减少测试数量以避免性能问题）
  notify({ content: "欢迎使用每日打卡系统", color: "#10b981", duration: 30000 });
  
  // 延迟测试不同类型的通知
  setTimeout(() => {
    notifySuccess("登录成功！");
    notifyWarning("请注意打卡时间");
    notifyError("网络连接异常");
  }, 2000);
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
