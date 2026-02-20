<script setup lang="ts">
import { useRouter } from "vue-router";
const router = useRouter();
</script>

<template>
  <button @click="router.push('/home')">返回</button>
  个人信息
</template>

<style scoped lang="scss"></style>
