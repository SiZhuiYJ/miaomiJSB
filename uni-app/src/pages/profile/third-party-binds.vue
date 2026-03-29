<template>
  <view class="page-container" :style="themeStore.themeStyle">
    <NotificationSystem />

    <view class="page-card">
      <view class="form-field">
        <text class="label">第三方账号绑定</text>
        <view class="desc">管理您绑定的第三方账号，用于便捷登录</view>
      </view>

      <view class="bind-list">
        <!-- 微信绑定 -->
        <view class="bind-item">
          <view class="bind-info">
            <image class="bind-icon" src="/static/images/wechat.png" mode="aspectFit" />
            <view class="bind-details">
              <text class="bind-name">微信</text>
              <text class="bind-desc">
                {{ hasProviderBind('wechat') ? '已绑定，' + formatDate(getBindingDate('wechat')!) : '未绑定' }}
              </text>
            </view>
          </view>
          <button class="bind-btn" :class="hasProviderBind('wechat') ? 'unbind-btn' : 'bind-btn-primary'"
            @click="toggleWechatBind()">
            {{ hasProviderBind('wechat') ? '解绑' : '绑定' }}
          </button>
        </view>

        <!-- 其他第三方账号可在此处扩展 -->
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { onShow } from '@dcloudio/uni-app';
import { useAuthStore } from '@/stores/auth';
import { useThemeStore } from '@/stores/theme';
import http from '@/libs/http/config';
import { notifySuccess, notifyError } from '@/utils/notification';

const authStore = useAuthStore();
const themeStore = useThemeStore();

onShow(async () => {
  themeStore.updateNavBarColor();
  // 获取最新的第三方绑定信息
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

// 检查特定提供商是否已绑定
function hasProviderBind(provider: string) {
  return authStore.user?.thirdPartyBindings?.some((binding: any) => binding.provider === provider) || false;
}

// 获取特定提供商的绑定日期
function getBindingDate(provider: string) {
  const binding = authStore.user?.thirdPartyBindings?.find((binding: any) => binding.provider === provider);
  return binding ? binding.boundAt : null;
}

// 格式化日期
function formatDate(dateString: string) {
  if (!dateString) return '';
  const date = new Date(dateString);
  return `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')}`;
}

// 处理微信绑定/解绑
async function toggleWechatBind() {
  if (hasProviderBind('wechat')) {
    // 解绑微信
    uni.showModal({
      title: '确认解绑',
      content: '解绑后将无法使用微信一键登录，确定要解绑吗？',
      confirmText: '确定解绑',
      cancelText: '取消',
      success: async (modalRes) => {
        if (modalRes.confirm) {
          try {
            await http.delete('/mm/Auth/wechat/unbind');
            notifySuccess('微信解绑成功');

            // 更新用户信息
            await authStore.fetchUserInfo();
          } catch (error: any) {
            console.error('解绑微信失败:', error);

            if (error.statusCode === 401) {
              notifyError('用户未登录或登录已过期');
            } else if (error.statusCode === 404) {
              notifyError('当前用户未绑定微信账号');
            } else {
              notifyError(error.data?.message || '解绑失败，请稍后重试');
            }
          }
        }
      }
    });
  } else {
    // 绑定微信
    if (hasWechatEnv()) {
      await bindWechat();
    } else {
      notifyError('请在微信小程序环境中使用此功能');
    }
  }
}

// 检查是否在微信环境中
function hasWechatEnv() {
  // #ifdef MP-WEIXIN
  return true;
  // #endif
  return false;
}

// 绑定微信
async function bindWechat() {
  try {
    // #ifdef MP-WEIXIN
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
  }
}
</script>

<style scoped lang="scss">
@use "@/styles/page-layouts.scss";

.bind-list {
  margin-top: 20px;
}

.bind-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 0;
  border-bottom: 0.5px solid rgba(0, 0, 0, 0.1);

  &:last-child {
    border-bottom: none;
  }
}

.bind-info {
  display: flex;
  align-items: center;
  flex: 1;
}

.bind-icon {
  width: 40px;
  height: 40px;
  margin-right: 12px;
  border-radius: 8px;
}

.bind-details {
  flex: 1;
}

.bind-name {
  display: block;
  font-size: 16px;
  color: var(--text-color);
  margin-bottom: 4px;
}

.bind-desc {
  display: block;
  font-size: 14px;
  color: var(--text-muted);
}

.bind-btn {
  padding: 8px 16px;
  border-radius: 8px;
  font-size: 14px;
  border: none;
  margin-left: 16px;
}

.bind-btn-primary {
  background-color: var(--theme-primary);
  color: white;
}

.unbind-btn {
  background-color: #f0f0f0;
  color: #ff4757;
}

.desc {
  font-size: 14px;
  color: var(--text-muted);
  margin-top: 8px;
}
</style>