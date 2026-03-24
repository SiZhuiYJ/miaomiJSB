<script setup lang="ts">
import router from "@/routers";
import { useTemplateRef } from "vue";
import { ElScrollbar } from "element-plus";
import SettingsMenu from "@/features/auth/components/Settings/menu.vue";
import { smoothScrollTo } from "@/utils/smoothScroll"; // 引入上面的工具函数
const scrollbarRef =
  useTemplateRef<InstanceType<typeof ElScrollbar>>("scrollbarRef");

const scrollToTop = () => {
  if (scrollbarRef.value) {
    const wrap = scrollbarRef.value.wrapRef;
    if (wrap) {
      smoothScrollTo(wrap, 0, 400); // 400ms 内平滑滚动到顶部
    }
  }
};
</script>

<template>
  <header class="topbar">
    <el-icon @click="router.push('/home')">
      <CaretLeft />
    </el-icon>
    <SettingsMenu />
  </header>
  <el-scrollbar ref="scrollbarRef" wrap-style="max-height: calc(100vh - var(--header-h));" view-class="">
    <div class="open">
      <router-view></router-view>
    </div>
    <el-button @click="scrollToTop" type="primary" style="margin-top: 20px">
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
  background: rgba($color: #000000, $alpha: 0);
}

.open {
  height: 10000px;
  width: 100vw;
  display: flex;
  flex-direction: column;
  align-items: center;
}
</style>
