<script setup lang="ts">
import Setting from "@/features/setting/components/index.vue";
</script>

<template>
  <el-scrollbar
    wrap-style="max-height: calc(100vh - var(--header-h));"
    view-class="dashboard"
  >
    <Setting />
    <div class="open"></div>
  </el-scrollbar>
</template>

<style scoped lang="scss">
.open {
  height: 1000px;
}
</style>
