<script setup lang="ts">
import { useAuthStore } from '../../stores/auth';
import { APP_TITLE } from '../../config';
import { useNavbar } from '../../utils/useNavbar';

const authStore = useAuthStore();
const { paddingTop, height, paddingLeft } = useNavbar();
</script>

<template>
  <view class="tab-content">
    <view class="header" :style="{ paddingTop: paddingTop + 'px', paddingLeft: paddingLeft + 'px' }">
      <text class="title" :style="{ lineHeight: height + 'px' }">{{ APP_TITLE }}</text>
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
  margin-bottom: 30px;
}

.content {
  margin-bottom: 80px;
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
