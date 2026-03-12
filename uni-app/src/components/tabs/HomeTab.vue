<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { APP_TITLE } from '@/config';
import { useNavbar } from '@/utils/useNavbar';

const authStore = useAuthStore();
const { paddingTop, height, paddingLeft, navbarHeight } = useNavbar();
</script>

<template>
  <view class="tab-content">
    <view class="tab-header-fixed"
      :style="{ paddingTop: paddingTop + 'px', paddingLeft: paddingLeft + 'px', paddingRight: paddingLeft + 'px' }">
      <view class="header-info-inner" :style="{ height: height + 'px' }">
        <text class="title">{{ APP_TITLE }}</text>
      </view>
    </view>

    <!-- 撑开固定头部的内容区域 -->
    <view :style="{ height: navbarHeight + 'px' }"></view>

    <!-- 固定头部 -->
    <view class="header" :style="{ paddingLeft: paddingLeft + 'px', paddingRight: paddingLeft + 'px' }">
      <text class="subtitle">欢迎回来，{{ authStore.user?.nickName || authStore.user?.userAccount || '用户' }}</text>
    </view>

    <view class="content">
      <text class="placeholder-text">这里是主页内容</text>
      <!-- You can add dashboard widgets or summary stats here later -->
    </view>
  </view>
</template>

<style scoped lang="scss">
.tab-content {
  padding: var(--uni-container-padding);
  box-sizing: border-box;
  background-color: var(--bg-color);
  min-height: 100vh;
}

.header {
  margin-bottom: 20px;
}

.tab-header-fixed {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
  background-color: rgba(var(--bg-color), 0.01);
}

.header-info-inner {
  display: flex;
  align-items: center;
  justify-content: flex-start;
}

.content {
  margin-bottom: 80px;
}

.title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-color);
}

.subtitle {
  font-size: 16px;
  color: var(--text-muted);
  margin-top: 10px;
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
