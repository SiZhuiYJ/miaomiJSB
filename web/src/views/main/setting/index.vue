<script setup lang="ts">
import Setting from "@/features/setting/components/index.vue";
import { ref } from 'vue'
import { ElScrollbar } from 'element-plus'
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
</script>

<template>
  <el-scrollbar ref="scrollbarRef" wrap-style="max-height: calc(100vh - var(--header-h));" view-class="">
    <Setting />
    <div class="open"></div>
    <el-button @click="scrollToTop" type="primary" style="margin-top: 20px;">
      平滑滚动到顶部
    </el-button>
  </el-scrollbar>
</template>

<style scoped lang="scss">
.open {
  height: 10000px;
}
</style>
