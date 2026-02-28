<script setup lang="ts">
import router from "@/routers/index";
import { ref } from 'vue'
import { ElScrollbar } from 'element-plus'
import { smoothScrollTo } from '@/utils/smoothScroll' // 引入上面的工具函数
import ImageUploader from "@/features/file/components/ImageUploader.vue";
const scrollbarRef = ref<InstanceType<typeof ElScrollbar>>()

const scrollToTop = () => {
  if (scrollbarRef.value) {
    const wrap = scrollbarRef.value.wrapRef
    if (wrap) {
      smoothScrollTo(wrap, 0, 400) // 400ms 内平滑滚动到顶部
    }
  }
}
function handleCommand(command: string): void {
  if (command === "profile") {
    router.push("/setting");
  } else if (command === "avatar") {
    router.push("/setting/avatar");
  } else if (command === "account") {
    router.push("/setting/account");
  } else if (command === "password") {
    router.push("/setting/password");
  } else if (command === "deactivate") {
    router.push("/setting/deactivate");
  }
}
</script>

<template>
  <header class="topbar">
    <span @click="router.push('/home')">
      账号设置
    </span>
    <el-dropdown trigger="click" placement="bottom-end" popper-class="user-dropdown-popper" @command="handleCommand">
      <el-icon class="dropdown-arrow"><arrow-down /></el-icon>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item command="profile">主页</el-dropdown-item>
          <el-dropdown-item command="avatar" divided>头像</el-dropdown-item>
          <el-dropdown-item command="account">账号</el-dropdown-item>
          <el-dropdown-item command="password">修改密码</el-dropdown-item>
          <el-dropdown-item command="deactivate" divided style="color: #ef4444">注销账号</el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
  </header>
  <el-scrollbar ref="scrollbarRef" wrap-style="max-height: calc(100vh - var(--header-h));" view-class="">
    <div class="open">
      <router-view></router-view>
    </div>
    <el-button @click="scrollToTop" type="primary" style="margin-top: 20px;">
      平滑滚动到顶部
    </el-button>
  </el-scrollbar>
</template>

<style scoped lang="scss">
.topbar {
  height: var(--header-h);
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-elevated);
}

.open {
  height: 10000px;
}
</style>
