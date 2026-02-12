<script setup lang="ts">
import { ref } from 'vue';
import { onLoad } from '@dcloudio/uni-app';
import { usePlansStore } from '../../stores/plans';
import { useThemeStore } from '../../stores/theme';
import { notifyError, notifySuccess, notifyWarning } from '../../utils/notification';

const plansStore = usePlansStore();
const themeStore = useThemeStore();

const isEdit = ref(false);
const planId = ref<number | null>(null);
const title = ref('');
const description = ref('');
const startDate = ref('');
const endDate = ref('');
const isActive = ref(true);

const today = new Date().toISOString().split('T')[0];

onLoad((options: any) => {
  if (options && options.id) {
    isEdit.value = true;
    planId.value = Number(options.id);
    const plan = plansStore.items.find(x => x.id === planId.value);
    if (plan) {
      title.value = plan.title;
      description.value = plan.description;
      startDate.value = plan.startDate;
      endDate.value = plan.endDate || '';
      isActive.value = plan.isActive;
      uni.setNavigationBarTitle({ title: '编辑计划' });
    }
  }
});

function handleStartDateChange(e: any) {
  startDate.value = e.detail.value;
}

function handleEndDateChange(e: any) {
  endDate.value = e.detail.value;
}

function handleIsActiveChange(e: any) {
  isActive.value = e.detail.value;
}

async function handleDelete() {
  if (!planId.value) return;
  uni.showModal({
    title: '确认删除',
    content: '确定要删除这个计划吗？删除后无法恢复。',
    success: async (res) => {
      if (res.confirm) {
        try {
          await plansStore.deletePlan(planId.value!);
          notifySuccess('删除成功');
          uni.$emit('plan-updated');
          uni.$emit('switch-tab', 1); // Switch to "Today's Check-in"
          setTimeout(() => {
            const pages = getCurrentPages();
            if (pages.length > 1) {
              // If previous page is plan detail, we should skip it because plan is deleted
              const prevPage = pages[pages.length - 2];
              if (prevPage.route === 'pages/plan/detail') {
                // Check if we have enough history to go back 2 steps
                if (pages.length > 2) {
                  uni.navigateBack({ delta: 2 });
                } else {
                  // If detail was the first page (unlikely but possible with deep link), relaunch
                  uni.reLaunch({ url: '/pages/main/index?tab=1' });
                }
              } else {
                uni.navigateBack();
              }
            } else {
              uni.reLaunch({ url: '/pages/main/index?tab=1' });
            }
          }, 1500);
        } catch (e) {
          notifyError('删除失败');
        }
      }
    }
  });
}

async function handleSubmit() {
  if (!title.value.trim()) {
    notifyWarning('请输入计划标题');
    return;
  }

  if (startDate.value && endDate.value && new Date(endDate.value) < new Date(startDate.value)) {
    notifyWarning('结束日期不能早于开始日期');
    return;
  }

  try {
    if (isEdit.value && planId.value) {
      await plansStore.updatePlan({
        id: planId.value,
        title: title.value.trim(),
        description: description.value || undefined,
        startDate: startDate.value || null,
        endDate: endDate.value || null,
        isActive: isActive.value
      });
      notifySuccess('更新成功');
    } else {
      await plansStore.createPlan({
        title: title.value.trim(),
        description: description.value || undefined,
        startDate: startDate.value || null,
        endDate: endDate.value || null,
      });
      notifySuccess('创建成功');
    }

    uni.$emit('plan-updated');
    uni.$emit('switch-tab', 1); // Switch to "Today's Check-in"
    setTimeout(() => {
      // Switch to tabbar page index
      if (getCurrentPages().length > 1) {
        uni.navigateBack();
      } else {
        uni.reLaunch({ url: '/pages/main/index?tab=1' });
      }
    }, 1500);
  } catch (e) {
    notifyError(isEdit.value ? '更新失败' : '创建失败');
  }
}
</script>

<template>
  <view class="container" :style="themeStore.themeStyle">
    <NotificationSystem />
    <view class="card">
      <view class="form-group">
        <view class="label">计划标题</view>
        <input class="input" v-model="title" placeholder="请输入标题" />
      </view>

      <view class="form-group">
        <view class="label">计划描述</view>
        <textarea class="textarea" v-model="description" placeholder="请输入描述" auto-height />
      </view>

      <view class="form-group">
        <view class="label">开始日期（默认今天）</view>
        <picker mode="date" :value="startDate" :start="today" @change="handleStartDateChange">
          <view class="picker-view">
            <text v-if="startDate">{{ startDate }}</text>
            <text v-else class="placeholder">点击选择</text>
          </view>
        </picker>
      </view>

      <view class="form-group">
        <view class="label">结束日期（可选）</view>
        <picker mode="date" :value="endDate" :start="startDate || today" @change="handleEndDateChange">
          <view class="picker-view">
            <text v-if="endDate">{{ endDate }}</text>
            <text v-else class="placeholder">点击选择</text>
          </view>
        </picker>
      </view>

      <view class="form-group" v-if="isEdit">
        <view class="label">是否启用</view>
        <switch :checked="isActive" @change="handleIsActiveChange" color="var(--accent-color)" />
      </view>
    </view>

    <button class="submit-btn" @click="handleSubmit">{{ isEdit ? '保存修改' : '创建计划' }}</button>
    <button v-if="isEdit" class="delete-btn" @click="handleDelete">删除计划</button>
  </view>
</template>

<style scoped lang="scss">
.container {
  padding: var(--uni-container-padding);
  background-color: var(--bg-color);
  min-height: 100vh;
  box-sizing: border-box;
}

.card {
  background-color: var(--bg-elevated);
  border-radius: var(--uni-card-border-radius);
  padding: var(--uni-card-padding);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  margin-bottom: var(--uni-card-margin-bottom);
}

.form-group {
  margin-bottom: 20px;
}

.form-group:last-child {
  margin-bottom: 0;
}


.label {
  font-size: 14px;
  color: var(--text-muted);
  margin-bottom: 8px;
}

.input,
.textarea,
.picker-view {
  background-color: var(--bg-color);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 10px;
  color: var(--text-color);
  font-size: 14px;
  width: 100%;
  box-sizing: border-box;
  /* Ensure padding doesn't affect width */
}

.input {
  height: 44px;
  line-height: 24px;
  /* content height */
}

.textarea {
  min-height: 80px;
  line-height: 1.5;
}

/* 解决H5下uni-input选中问题 */
:deep(input),
:deep(textarea) {
  user-select: text !important;
  -webkit-user-select: text !important;
}

.picker-view {
  min-height: 40px;
  display: flex;
  align-items: center;
}

.placeholder {
  color: var(--text-muted);
}

.submit-btn {
  background-color: var(--theme-primary);
  color: #fff;
  border-radius: 999px;
  border: none;
  font-weight: 600;
  margin-bottom: var(--uni-card-margin-bottom);
}


.delete-btn {
  background-color: #ef4444;
  color: white;
  border-radius: 999px;
  border: none;
  font-weight: 600;
}
</style>
