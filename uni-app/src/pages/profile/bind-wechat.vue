<template>
  <view class="page-container" :style="themeStore.themeStyle">
    <NotificationSystem />

    <view class="page-card">
      <view class="form-field">
        <text class="label">微信绑定</text>
        <view class="wechat-bind-info">
          <text class="desc">
            {{
              hasWechatBind
                ? '您已绑定微信账号，可以使用微信一键登录'
                : '绑定微信后，您可以使用微信一键登录，方便快捷'
            }}
          </text>
        </view>

        <view class="wechat-bind-status">
          <text class="status-text" :class="hasWechatBind ? 'bound' : 'unbound'">
            {{
              hasWechatBind
                ? '已绑定微信'
                : '未绑定微信'
            }}
          </text>
        </view>
      </view>
    </view>

    <view class="page-actions">
      <button class="btn-primary" :loading="loading" :disabled="loading || hasWechatBind" @click="handleBindWechat">
        {{
          hasWechatBind
            ? '已绑定微信'
            : '点击绑定微信'
        }}
      </button>

      <button v-if="hasWechatBind" class="btn-secondary" :loading="loading" @click="handleUnbindWechat">
        解绑微信
      </button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { onShow } from '@dcloudio/uni-app';
import { useAuthStore } from '@/stores/auth';
import { useThemeStore } from '@/stores/theme';
import http from '@/libs/http/config';
import { notifySuccess, notifyError } from '@/utils/notification';

const authStore = useAuthStore();
const themeStore = useThemeStore();

onShow(async () => {
  themeStore.updateNavBarColor();
  // 获取最新的用户信息和第三方绑定信息
  await authStore.fetchUserInfo();
  await fetchThirdPartyBindings();
});

// 获取第三方绑定信息
async function fetchThirdPartyBindings() {
  try {
    const res = await http.get('/mm/Auth/bindings');
    if (res.statusCode === 200 && res.data) {
      // 更新用户信息中的第三方绑定数据
      authStore.updateUser({ 
        thirdPartyBindings: res.data.bindings || []
      });
    }
  } catch (error) {
    console.error('Failed to fetch third party bindings', error);
  }
}

const loading = ref(false);

// 计算属性：判断是否已绑定微信
const hasWechatBind = computed(() => {
  // 检查用户是否已经有微信绑定信息
  // 使用thirdPartyBindings数组来判断是否绑定了微信
  return authStore.user?.thirdPartyBindings?.some((binding: any) => binding.provider === 'wechat') || false;
});

// 处理微信绑定
async function handleBindWechat() {
  if (hasWechatBind.value) {
    notifySuccess('您已经绑定过微信了');
    return;
  }

  loading.value = true;
  try {
    // #ifdef MP-WEIXIN
    // 微信小程序环境
    const loginRes: any = await new Promise((resolve, reject) => {
      uni.login({
        provider: 'weixin',
        success: resolve,
        fail: reject
      });
    });

    if (!loginRes.code) {
      throw new Error('获取微信登录凭证失败');
    }

    await http.post('/mm/Auth/wechat/bind', {
      code: loginRes.code
    });

    notifySuccess('微信绑定成功');

    // 更新用户信息
    await authStore.fetchUserInfo();

    setTimeout(() => {
      uni.navigateBack();
    }, 1500);
    // #endif

    // #ifndef MP-WEIXIN
    notifyError('请在微信小程序环境中使用此功能');
    // #endif
  } catch (error: any) {
    console.error('绑定微信失败:', error);

    if (error.statusCode === 409) {
      notifyError(error.data?.message || '绑定失败：微信账号已被绑定');
    } else if (error.statusCode === 401) {
      notifyError('用户未登录或登录已过期，请重新登录');
    } else {
      notifyError(error.data?.message || '绑定失败，请稍后重试');
    }
  } finally {
    loading.value = false;
  }
}

// 处理微信解绑
async function handleUnbindWechat() {
  uni.showModal({
    title: '确认解绑',
    content: '解绑后将无法使用微信一键登录，确定要解绑吗？',
    confirmText: '确定解绑',
    cancelText: '取消',
    success: async (modalRes) => {
      if (modalRes.confirm) {
        try {
          loading.value = true;

          // 调用后端解绑接口
          await http.delete('/mm/Auth/wechat/unbind');

          notifySuccess('微信解绑成功');

          // 更新用户信息
          await authStore.fetchUserInfo();

          // 重新加载页面
          setTimeout(() => {
            uni.redirectTo({
              url: '/pages/profile/index'
            });
          }, 1000);
        } catch (error: any) {
          console.error('解绑微信失败:', error);

          if (error.statusCode === 401) {
            notifyError('用户未登录或登录已过期');
          } else if (error.statusCode === 404) {
            notifyError('当前用户未绑定微信账号');
          } else {
            notifyError(error.data?.message || '解绑失败，请稍后重试');
          }
        } finally {
          loading.value = false;
        }
      }
    }
  });
}
</script>

<style scoped lang="scss">
@use "@/styles/page-layouts.scss";

.wechat-bind-info {
  margin: 20rpx 0;
}

.desc {
  font-size: 28rpx;
  color: #666;
  line-height: 1.6;
}

.wechat-bind-status {
  margin-top: 20rpx;

  .status-text {
    font-size: 28rpx;
    padding: 8rpx 16rpx;
    border-radius: 20rpx;

    &.bound {
      background-color: #e8f5e9;
      color: #4caf50;
    }

    &.unbound {
      background-color: #fff3e0;
      color: #ff9800;
    }
  }
}

.btn-secondary {
  margin-top: 20rpx;
  height: 80rpx;
  line-height: 80rpx;
  background-color: #f5f5f5;
  color: #333;
  border: 1rpx solid #ddd;
  border-radius: 8rpx;
  font-size: 32rpx;
}
</style>