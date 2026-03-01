<script setup lang="ts">
import router from "@/routers/index";
import { ref } from 'vue'
import { ElScrollbar } from 'element-plus'
import SettingsMenu from "@/features/auth/components/Settings/menu.vue";
import { smoothScrollTo } from '@/utils/smoothScroll' // 引入上面的工具函数
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
    <SettingsMenu>
    </SettingsMenu>
    <!-- <SettingsMenuItem @click="router.push('/setting/profile')">个人资料</SettingsMenuItem>
    <SettingsMenuItem @click="router.push('/setting/avatar')">头像设置</SettingsMenuItem>
    <SettingsMenuItem @click="router.push('/setting/account')">账号设置</SettingsMenuItem>
    <SettingsMenuItem @click="router.push('/setting/password')">修改密码</SettingsMenuItem>
    <SettingsMenuItem @click="router.push('/setting/deactivate')" style="color: #ef4444">注销账号</SettingsMenuItem> -->

    <!-- <el-dropdown trigger="click" placement="bottom-end" popper-class="user-dropdown-popper" @command="handleCommand">
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
</el-dropdown> -->
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
  width: 100%;
  z-index: 1;
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid var(--border-color);
  background: rgba($color: #000000, $alpha: .0);
}

.open {
  height: 10000px;
  width: 100vw;
}
</style>
