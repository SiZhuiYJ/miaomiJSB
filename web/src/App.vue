<script setup lang="ts">
// 2495550774@qq.com
import { ref, onMounted } from "vue";
import { ElConfigProvider, dayjs } from "element-plus";
import { setNotificationInstance } from "@/utils/notification";
import NotificationSystem from "@/components/NotificationSystem/index.vue";
import { notify } from "@/utils/notification";
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
  for (let i = 1; i <= 10; i++)
    notify({ content: "测试", color: "#22c55e", duration: 60000 });
  for (let i = 1; i <= 10; i++)
    setTimeout(() => {
      notify({ content: "测试" + i, color: "#22c55e", duration: i * 1000 });
    }, 10000);
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
