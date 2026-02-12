<script setup lang="ts">
import { computed } from 'vue';
import { useThemeStore } from '../stores/theme';

const props = defineProps<{
  current: number;
}>();

const emit = defineEmits<{
  (e: 'change', index: number): void;
}>();

const themeStore = useThemeStore();

const tabs = [
  { text: '主页' },
  { text: '今日打卡' },
  { text: '设置' },
];

function handleTabClick(index: number) {
  if (props.current === index) return;
  emit('change', index);
}

const pillStyle = computed(() => {
  const widthPercent = 100 / tabs.length;
  const translateX = props.current * 100;
  return {
    width: `${widthPercent}%`,
    transform: `translateX(${translateX}%)`,
  };
});
</script>

<template>
  <view class="tab-bar-placeholder"></view>
  <view class="tab-bar-container" :style="themeStore.themeStyle">
    <view class="tab-bar-capsule">
      <!-- Background Pill -->
      <view class="tab-pill-wrapper">
        <view class="tab-pill" :style="pillStyle"></view>
      </view>

      <!-- Tab Items -->
      <view
        v-for="(item, index) in tabs"
        :key="index"
        class="tab-item"
        :class="{ active: current === index }"
        hover-class="none"
        @click="handleTabClick(index)"
      >
        <text class="tab-text">{{ item.text }}</text>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.tab-bar-placeholder {
  height: 80px;
}

.tab-bar-container {
  position: fixed;
  bottom: 20px;
  left: 0;
  right: 0;
  display: flex;
  justify-content: center;
  z-index: 999;
}

.tab-bar-capsule {
  position: relative;
  display: flex;
  background-color: #fff;
  border-radius: 30px;
  padding: 5px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  border: 1px solid rgba(0, 0, 0, 0.05);
  /* Ensure width is fixed or flexible */
  min-width: 300px; 
}

.tab-pill-wrapper {
  position: absolute;
  top: 5px;
  left: 5px;
  right: 5px;
  bottom: 5px;
  pointer-events: none;
}

.tab-pill {
  height: 100%;
  background-color: var(--theme-primary);
  border-radius: 24px;
  transition: transform 0.3s cubic-bezier(0.4, 0.0, 0.2, 1);
  box-shadow: 0 2px 6px rgba(142, 168, 142, 0.4);
}

.tab-item {
  flex: 1;
  padding: 8px 16px;
  border-radius: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  // cursor: pointer;
  position: relative;
  z-index: 1;
}

.tab-text {
  font-size: 14px;
  font-weight: 500;
  color: #666;
  transition: color 0.3s;
}

.tab-item.active .tab-text {
  color: #fff;
  font-weight: 600;
}
</style>
