<script setup lang="ts">
import { useAuthStore } from '../../stores/auth';
import { APP_TITLE } from '../../config';
import CustomTabBar from '../../components/CustomTabBar.vue';

const authStore = useAuthStore();
</script>

<template>
  <view class="container">
    <view class="header">
      <text class="title">{{ APP_TITLE }}</text>
      <text class="subtitle">欢迎回来，{{ authStore.user?.nickName || authStore.user?.userAccount || '用户' }}</text>
    </view>
    
    <view class="content">
      <text class="placeholder-text">这里是主页内容</text>
      <!-- You can add dashboard widgets or summary stats here later -->
    </view>

    <CustomTabBar :current="0" />
  </view>
</template>

<style scoped lang="scss">
.container {
  padding: var(--uni-container-padding);
  min-height: 100%;
  box-sizing: border-box;
  background-color: var(--bg-color);
}

.header {
  margin-bottom: 30px;
  margin-top: 20px;
}

.title {
  font-size: 28px;
  font-weight: bold;
  color: var(--text-color);
  display: block;
  margin-bottom: 8px;
}

.subtitle {
  font-size: 16px;
  color: var(--text-muted);
}

.content {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 300px;
  background-color: var(--bg-elevated);
  border-radius: var(--uni-card-border-radius);
}

.placeholder-text {
  color: var(--text-muted);
  font-size: 14px;
}
</style>
