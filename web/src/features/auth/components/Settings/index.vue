<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useAuthStore } from '@/stores';
import { http } from '../../utils/http';
import { API_BASE_URL } from '../../config';
import { onLoad, onShow } from '@dcloudio/uni-app';
import { notifySuccess, notifyError } from '@/utils/notification';

const authStore = useAuthStore();

const loading = ref(false);

onShow(async () => {
  await authStore.fetchUserInfo();
});

function handleNavigate(url: string) {
  uni.navigateTo({ url });
}

function handleViewAvatar() {
  if (authStore.user?.avatarKey) {
    const url = `${API_BASE_URL}/mm/Files/users/${authStore.user.userId}/${authStore.user.avatarKey}`;
    uni.previewImage({
      urls: [url],
      current: 0
    });
  }
}

function handleEditAvatar() {
  uni.showActionSheet({
    itemList: ['使用微信头像', '选择照片'],
    success: async (res) => {
      if (res.tapIndex === 0) {
        // WeChat Avatar
        // Since uni.getUserProfile is limited, we try to guide the user or use uni.chooseImage with special handling if possible.
        // However, standard practice now is often just letting them pick.
        // We will try a simple approach: Just fetch it? No, can't.
        // We will use uni.getUserProfile if available (deprecated but works in some contexts) or warn.
        // Actually, let's just use chooseImage/chooseMedia but maybe the user expects the "WeChat Avatar" button behavior.
        // Since we can't invoke that programmatically from ActionSheet, we might just try to use uni.getUserProfile to get the URL.
        // Note: As of 2022, getUserProfile returns grey avatar. 
        // Best approach for "WeChat Avatar" option in a custom UI without a specific button is tricky.
        // We'll treat it as a "Select from Album" but maybe user has saved it?
        // Let's implement the 'Select Photo' flow primarily, and for 'WeChat Avatar' maybe show a toast explaining they should select it from album if they can't authorize.
        // OR: We try `uni.getUserProfile` and if it works (old base lib) great.

        // Actually, let's try to just use uni.chooseImage for both but maybe with different source?
        // No.
        // Let's just implement "Select Photo" fully first as that covers "File Upload".
        // For "WeChat Avatar", if we are in MP-WEIXIN:
        // We can't easily get it.
        // I will implement a workaround: Provide a "Use WeChat Avatar" button that calls getUserProfile.
        // If it fails or returns placeholder, I'll alert the user.

        // #ifdef MP-WEIXIN
        uni.getUserProfile({
          desc: '用于完善会员资料',
          success: (profileRes) => {
            // profileRes.userInfo.avatarUrl might be temporary or grey.
            // If valid, we download it.
            const avatarUrl = profileRes.userInfo.avatarUrl;
            downloadAndUpload(avatarUrl);
          },
          fail: () => {
            // If fail, maybe they cancelled or not supported.
            uni.showToast({ title: '无法获取微信头像，请尝试文件上传', icon: 'none' });
          }
        });
        // #endif

        // #ifndef MP-WEIXIN
        uni.showToast({ title: '仅微信小程序支持', icon: 'none' });
        // #endif

      } else if (res.tapIndex === 1) {
        // Select Photo
        uni.chooseImage({
          count: 1,
          sizeType: ['compressed'],
          sourceType: ['album', 'camera'],
          success: (chooseRes) => {
            const tempFilePaths = chooseRes.tempFilePaths;
            if (tempFilePaths.length > 0) {
              // Navigate to cropper
              uni.navigateTo({
                url: `/pages/common/cropper?src=${tempFilePaths[0]}`,
                events: {
                  cropSuccess: (data: any) => {
                    uploadFile(data.tempFilePath);
                  }
                }
              });
            }
          }
        });
      }
    }
  });
}

function downloadAndUpload(url: string) {
  uni.showLoading({ title: '上传中...' });
  uni.downloadFile({
    url: url,
    success: (res) => {
      if (res.statusCode === 200) {
        uploadFile(res.tempFilePath);
      } else {
        uni.hideLoading();
        notifyError('头像下载失败');
      }
    },
    fail: () => {
      uni.hideLoading();
      notifyError('头像下载失败');
    }
  });
}

function uploadFile(filePath: string) {
  loading.value = true;
  uni.uploadFile({
    url: `${API_BASE_URL}/mm/Files/avatar`,
    filePath: filePath,
    name: 'file',
    header: {
      'Authorization': `Bearer ${authStore.accessToken}`
    },
    success: (uploadRes) => {
      loading.value = false;
      uni.hideLoading(); // Just in case
      if (uploadRes.statusCode === 200) {
        try {
          const data = JSON.parse(uploadRes.data);
          const key = data.key;
          if (key && authStore.user?.userId) {
            // Update store
            authStore.updateUser({ avatarKey: key });
            notifySuccess('头像上传成功');
          } else {
            notifyError('头像上传失败: 无效的响应');
          }
        } catch (e) {
          notifyError('头像上传失败: 数据解析错误');
        }
      } else {
        notifyError('头像上传失败');
      }
    },
    fail: (err) => {
      loading.value = false;
      uni.hideLoading();
      notifyError('头像上传失败: 网络错误');
    }
  });
}

// Deprecated old handler, kept just in case but we use separate handlers now
async function handleAvatarClick() {
  handleEditAvatar();
}

// Verification Code Logic
const codeCountdown = ref(0);
let codeTimer: number | null = null;
const sendingCode = ref(false);

function startCodeCountdown(seconds: number = 60) {
  codeCountdown.value = seconds;
  if (codeTimer) clearInterval(codeTimer);
  codeTimer = setInterval(() => {
    if (codeCountdown.value > 0) {
      codeCountdown.value--;
    } else {
      if (codeTimer) {
        clearInterval(codeTimer);
        codeTimer = null;
      }
    }
  }, 1000);
}

async function handleSendVerificationCode(actionType: 'change-password' | 'deactivate') {
  if (!authStore.user?.email) {
    notifyError('无法获取用户邮箱');
    return;
  }
  if (sendingCode.value || codeCountdown.value > 0) return;

  sendingCode.value = true;
  try {
    await http.post('/mm/Auth/email-code', {
      email: authStore.user.email,
      actionType: actionType
    });
    notifySuccess('验证码已发送');
    startCodeCountdown();
  } catch (error: any) {
    const status = error.statusCode;
    if (status === 429) notifyError('请求过于频繁');
    else notifyError('发送失败');
  } finally {
    sendingCode.value = false;
  }
}

// Change Password
const changePasswordVisible = ref(false);
const verificationMethod = ref<'password' | 'code'>('password');
const pwdForm = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: '',
  code: ''
});

function openChangePassword() {
  pwdForm.oldPassword = '';
  pwdForm.newPassword = '';
  pwdForm.confirmPassword = '';
  pwdForm.code = '';
  verificationMethod.value = 'password';
  changePasswordVisible.value = true;
}

async function handleChangePassword() {
  if (pwdForm.newPassword !== pwdForm.confirmPassword) {
    notifyError('两次输入的新密码不一致');
    return;
  }

  if (verificationMethod.value === 'code' && !pwdForm.code) {
    notifyError('请输入验证码');
    return;
  }

  if (verificationMethod.value === 'password' && !pwdForm.oldPassword) {
    notifyError('请输入旧密码');
    return;
  }

  loading.value = true;
  try {
    const payload: any = {
      newPassword: pwdForm.newPassword
    };

    if (verificationMethod.value === 'password') {
      payload.oldPassword = pwdForm.oldPassword;
    } else {
      payload.code = pwdForm.code;
    }

    await http.post('/mm/Auth/change-password', payload);
    notifySuccess('密码修改成功，请重新登录');
    changePasswordVisible.value = false;
    authStore.clear();
    uni.reLaunch({ url: '/pages/auth/index' });
  } catch (error: any) {
    if (error.statusCode === 401) notifyError('旧密码错误');
    else if (error.statusCode === 400) notifyError('请求无效');
    else notifyError('修改失败');
  } finally {
    loading.value = false;
  }
}

// Deactivate Account
const deactivateVisible = ref(false);
const deactivateCode = ref('');

function openDeactivate() {
  deactivateCode.value = '';
  deactivateVisible.value = true;
}

async function handleDeactivateConfirm() {
  if (!deactivateCode.value) {
    notifyError('请输入验证码');
    return;
  }

  loading.value = true;
  try {
    await http.post('/mm/Auth/deactivate', {
      code: deactivateCode.value
    });
    notifySuccess('账号已注销');
    deactivateVisible.value = false;
    authStore.clear();
    uni.reLaunch({ url: '/pages/auth/index' });
  } catch (error: any) {
    if (error.statusCode === 400) notifyError('验证码错误');
    else notifyError('注销失败');
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <view class="container" :style="themeStore.themeStyle">
    <NotificationSystem />

    <view class="profile-header">
      <view class="avatar-large">
        <image v-if="authStore.user?.avatarKey"
          :src="`${API_BASE_URL}/mm/Files/users/${authStore.user.userId}/${authStore.user.avatarKey}`"
          class="avatar-img" mode="aspectFill" @click.stop="handleViewAvatar" />
        <text v-else class="avatar-text-large" @click.stop="handleViewAvatar">{{ (authStore.user?.nickName ||
          authStore.user?.userAccount ||
          authStore.user?.email ||
          'U')[0].toUpperCase() }}</text>
        <view class="avatar-edit-overlay" @click.stop="handleEditAvatar">
          <text class="edit-icon">✎</text>
        </view>
      </view>
      <text class="user-email">{{ authStore.user?.email }}</text>
    </view>

    <view class="card">
      <view class="list-group">
        <view class="list-item" @click="handleNavigate('/pages/profile/update-account')">
          <text class="item-label">账号</text>
          <view class="item-value">
            <text class="value-text">{{ authStore.user?.userAccount || '未设置' }}</text>
            <image class="arrow-icon" src="/static/svg/turn-right.svg" mode="aspectFit" />
          </view>
        </view>
        <view class="list-item" @click="handleNavigate('/pages/profile/update-nickname')">
          <text class="item-label">昵称</text>
          <view class="item-value">
            <text class="value-text">{{ authStore.user?.nickName || '未设置' }}</text>
            <image class="arrow-icon" src="/static/svg/turn-right.svg" mode="aspectFit" />
          </view>
        </view>
        <view class="list-item" @click="openChangePassword">
          <text class="item-label">修改密码</text>
          <view class="item-value">
            <image class="arrow-icon" src="/static/svg/turn-right.svg" mode="aspectFit" />
          </view>
        </view>
      </view>
    </view>

    <view class="actions">
      <button class="btn-deactivate" @click="openDeactivate">注销账号</button>
    </view>


    <!-- Change Password Modal -->
    <view class="modal-mask" v-if="changePasswordVisible">
      <view class="modal-content">
        <view class="modal-header">修改密码</view>
        <view class="modal-body">
          <view class="method-selector">
            <view :class="['method-tab', verificationMethod === 'password' ? 'active' : '']"
              @click="verificationMethod = 'password'">旧密码验证</view>
            <view :class="['method-tab', verificationMethod === 'code' ? 'active' : '']"
              @click="verificationMethod = 'code'">验证码验证</view>
          </view>

          <input v-if="verificationMethod === 'password'" class="input" v-model="pwdForm.oldPassword" password
            placeholder="旧密码" placeholder-class="input-placeholder" />

          <view v-if="verificationMethod === 'code'" class="code-row">
            <input class="input code-input" v-model="pwdForm.code" placeholder="验证码"
              placeholder-class="input-placeholder" />
            <button class="code-btn" :disabled="sendingCode || codeCountdown > 0"
              @click="handleSendVerificationCode('change-password')">
              {{ codeCountdown > 0 ? `${codeCountdown}s` : (sendingCode ? '发送中...' : '获取验证码') }}
            </button>
          </view>

          <input class="input" v-model="pwdForm.newPassword" password placeholder="新密码"
            placeholder-class="input-placeholder" />
          <input class="input" v-model="pwdForm.confirmPassword" password placeholder="确认新密码"
            placeholder-class="input-placeholder" />
        </view>
        <view class="modal-footer">
          <button class="modal-btn cancel" @click="changePasswordVisible = false">取消</button>
          <button class="modal-btn confirm" :loading="loading" @click="handleChangePassword">确定</button>
        </view>
      </view>
    </view>

    <!-- Deactivate Modal -->
    <view class="modal-mask" v-if="deactivateVisible">
      <view class="modal-content">
        <view class="modal-header">注销账号</view>
        <view class="modal-body">
          <text class="warning-text">注销后账号将无法登录，所有数据保留30天后彻底删除。</text>
          <view class="code-row">
            <input class="input code-input" v-model="deactivateCode" placeholder="验证码"
              placeholder-class="input-placeholder" />
            <button class="code-btn" :disabled="sendingCode || codeCountdown > 0"
              @click="handleSendVerificationCode('deactivate')">
              {{ codeCountdown > 0 ? `${codeCountdown}s` : (sendingCode ? '发送中...' : '获取验证码') }}
            </button>
          </view>
        </view>
        <view class="modal-footer">
          <button class="modal-btn cancel" @click="deactivateVisible = false">取消</button>
          <button class="modal-btn danger" :loading="loading" @click="handleDeactivateConfirm">确认注销</button>
        </view>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.container {
  min-height: 100%;
  box-sizing: border-box;
  background-color: var(--bg-color);
  padding: var(--uni-container-padding);
}

.profile-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 24px;
  padding: 20px 0;
}

.avatar-large {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background-color: var(--theme-primary);
  /* Changed to theme primary */
  border: 2px solid var(--border-color);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  overflow: hidden;
  /* Ensure image stays inside */
  position: relative;
}

.avatar-edit-overlay {
  position: absolute;
  bottom: 0;
  width: 100%;
  height: 24px;
  background-color: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
}

.edit-icon {
  color: white;
  font-size: 14px;
  margin-bottom: 2px;
}

.avatar-img {
  width: 100%;
  height: 100%;
}

.avatar-text-large {
  font-size: 32px;
  font-weight: bold;
  color: #ffffff;
  /* White text on colored background */
}

.user-name {
  font-size: 20px;
  font-weight: bold;
  color: var(--text-color);
  margin-bottom: 4px;
}

.user-email {
  font-size: 14px;
  color: var(--text-muted);
}

.card {
  background-color: var(--bg-elevated);
  border-radius: var(--uni-card-border-radius);
  padding: var(--uni-card-padding);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  margin-bottom: var(--uni-card-margin-bottom);
}



.field {
  margin-bottom: 16px;
}

.field:last-child {
  margin-bottom: 0;
}

.label {
  display: block;
  font-size: 14px;
  color: var(--text-muted);
  margin-bottom: 8px;
}

.hint-text {
  display: block;
  font-size: 12px;
  color: #fa5151;
  margin-top: 4px;
}

.input {
  width: 100%;
  height: 44px;
  background-color: #f9fafb;
  /* Keep light input background or use a slightly darker one if needed, but #f9fafb is fine */
  border-radius: 8px;
  padding: 0 12px;
  font-size: 14px;
  color: #1f2937;
  box-sizing: border-box;
  border: 1px solid var(--border-color);
}

.list-group {
  display: flex;
  flex-direction: column;
}

.list-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 0;
  border-bottom: 0.5px solid rgba(0, 0, 0, 0.1);
}

.list-item:last-child {
  border-bottom: none;
}

.item-label {
  font-size: 16px;
  color: var(--text-color);
}

.item-value {
  display: flex;
  align-items: center;
  gap: 8px;
}

.value-text {
  font-size: 14px;
  color: var(--text-muted);
}

.arrow-icon {
  width: 16px;
  height: 16px;
  opacity: 0.3;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.method-selector {
  display: flex;
  margin-bottom: 16px;
  background-color: #f3f4f6;
  padding: 4px;
  border-radius: 8px;
}

.method-tab {
  flex: 1;
  text-align: center;
  padding: 8px 0;
  font-size: 14px;
  color: #6b7280;
  border-radius: 6px;
  transition: all 0.2s;
}

.method-tab.active {
  background-color: #ffffff;
  color: #1f2937;
  font-weight: 500;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.btn-save {
  background-color: var(--theme-primary);
  /* var(--accent-color) usually maps to green, making it explicit */
  color: var(--accent-on);
  border-radius: 8px;
  font-size: 16px;
  height: 44px;
  line-height: 44px;
  border: none;
  font-weight: 600;
}

/* Modal Styles */
.modal-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 999;
}

.modal-content {
  width: 80%;
  max-width: 320px;
  background-color: #ffffff;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.modal-header {
  padding: 16px;
  text-align: center;
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
  border-bottom: 1px solid #e5e7eb;
}

.modal-body {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  background-color: #ffffff;
}

.code-row {
  display: flex;
  gap: 8px;
}

.code-input {
  flex: 1;
}

.code-btn {
  width: 100px;
  height: 44px;
  line-height: 44px;
  text-align: center;
  padding: 0;
  font-size: 12px;
  background-color: #f9fafb;
  border: 1px solid #e5e7eb;
  color: #1f2937;
  border-radius: 8px;
}

.code-btn[disabled] {
  opacity: 0.6;
}

.code-btn::after {
  border: none;
}

.warning-text {
  font-size: 14px;
  color: #ef4444;
  line-height: 1.5;
}

.modal-footer {
  display: flex;
  border-top: 1px solid #e5e7eb;
  background-color: #ffffff;
}

.modal-btn {
  flex: 1;
  height: 48px;
  line-height: 48px;
  text-align: center;
  background-color: transparent;
  font-size: 16px;
  border-radius: 0;
}

.modal-btn::after {
  border: none;
}

.modal-btn.cancel {
  color: #6b7280;
  border-right: 1px solid #e5e7eb;
}

.modal-btn.confirm {
  color: var(--accent-color);
}

.modal-btn.danger {
  color: #ef4444;
}

.btn-deactivate {
  background-color: transparent;
  color: #fa5151;
  font-size: 14px;
  height: 40px;
  line-height: 40px;
  border: none;
  font-weight: 400;
  margin-top: 20px;
}

.btn-deactivate::after {
  border: none;
}
</style>
