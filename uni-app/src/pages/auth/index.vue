<script setup lang="ts">
import { onUnmounted, ref } from 'vue';
import { useAuthStore } from '../../stores/auth';
import { http } from '../../utils/http';
import { notifyError, notifySuccess, notifyWarning } from '../../utils/notification';
import { APP_TITLE } from '../../config';


const mode = ref<'login' | 'register'>('login');
const loginMethod = ref<'email' | 'account'>('email');
const loading = ref(false);

const email = ref('');
const password = ref('');
const nickName = ref('');
const userAccount = ref('');
const userAccountError = ref('');
const lastAutouserAccount = ref('');
const confirmPassword = ref('');

watch(email, (newEmail) => {
  if (mode.value === 'register') {
    const prefix = newEmail.split('@')[0];
    if (!userAccount.value || userAccount.value === lastAutouserAccount.value) {
      userAccount.value = prefix;
      lastAutouserAccount.value = prefix;
      userAccountError.value = '';
    }
  }
});

const code = ref('');
const sendingCode = ref(false);
const countdown = ref(0);

let countdownTimer: number | null = null;

function generateRandomuserAccount() {
  userAccount.value = 'user_' + Math.random().toString(36).slice(2, 10);
  userAccountError.value = '';
}

async function validateuserAccount() {
  if (!userAccount.value || mode.value !== 'register') return;

  try {
    await http.post('/mm/Auth/validate-account?userAccount=' + encodeURIComponent(userAccount.value));
    userAccountError.value = '';
  } catch (error: any) {
    if (error.statusCode === 409) {
      userAccountError.value = '账号名已存在';
    } else {
      userAccountError.value = '';
    }
  }
}

function startCountdown(seconds: number): void {
  countdown.value = seconds;
  if (countdownTimer !== null) {
    clearInterval(countdownTimer);
  }
  countdownTimer = setInterval(() => {
    if (countdown.value > 0) {
      countdown.value -= 1;
    } else {
      if (countdownTimer !== null) {
        clearInterval(countdownTimer);
        countdownTimer = null;
      }
    }
  }, 1000);
}

const auth = useAuthStore();

async function handleSubmit() {
  if (mode.value === 'register') {
    if (!userAccount.value) {
      notifyError('请输入账号名');
      return;
    }
    // Ensure userAccount is validated
    if (userAccountError.value) {
      notifyError(userAccountError.value);
      return;
    }
    // Optional: Double check if validation wasn't triggered
    await validateuserAccount();
    if (userAccountError.value) {
      notifyError(userAccountError.value);
      return;
    }

    if (!code.value) {
      notifyError('请输入邮箱验证码');
      return;
    }
    if (password.value.length < 6) {
      notifyError('密码长度至少为 6 位');
      return;
    }
    if (password.value !== confirmPassword.value) {
      notifyError('两次输入的密码不一致');
      return;
    }
  }
  loading.value = true;
  try {
    if (mode.value === 'login') {
      let response;
      if (loginMethod.value === 'email') {
        response = await http.post<any>('/mm/Auth/login', {
          email: email.value,
          password: password.value,
        });
      } else {
        response = await http.post<any>('/mm/Auth/login-account', {
          userAccount: userAccount.value,
          password: password.value,
        });
      }
      // console.log(response.data);
      auth.setSession(response.data);
      notifySuccess('登录成功');
      setTimeout(() => {
        // Redirect to main page (which handles tabs manually)
        uni.reLaunch({ url: '/pages/main/index' });
      }, 1500);
    } else {
      const response = await http.post<any>('/mm/Auth/register', {
        email: email.value,
        password: password.value,
        nickName: nickName.value || null,
        userAccount: userAccount.value || null,
        code: code.value,
      });
      auth.setSession(response.data);
      notifySuccess('注册并登录成功');
      setTimeout(() => {
        uni.reLaunch({ url: '/pages/main/index' });
      }, 1500);
    }
  } catch (error: any) {
    if (error.statusCode === 401) {
      uni.showToast({ title: '账号或密码错误', icon: 'none' });
    } else if (error.statusCode === 409) {
      uni.showToast({ title: '用户名已被占用', icon: 'none' });
    } else {
      uni.showToast({ title: '操作失败', icon: 'none' });
    }
  } finally {
    loading.value = false;
  }
}

async function handleSendCode() {
  if (!email.value) {
    notifyWarning('请先填写邮箱');
    return;
  }
  if (sendingCode.value || countdown.value > 0) {
    return;
  }
  sendingCode.value = true;
  try {
    await http.post('/mm/Auth/email-code', {
      email: email.value,
      actionType: 'register'
    });
    notifySuccess('验证码已发送');
    startCountdown(60);
  } catch (error: any) {
    const status = error?.statusCode;
    if (status === 429) {
      notifyError('请求过于频繁');
    } else if (status === 409) {
      notifyError('该邮箱已注册');
    } else if (status === 400) {
      notifyError('邮箱格式不正确');
    } else {
      notifyError('发送失败');
    }
  } finally {
    sendingCode.value = false;
  }
}

function switchMode(next: 'login' | 'register') {
  mode.value = next;
}

onUnmounted(() => {
  if (countdownTimer !== null) {
    clearInterval(countdownTimer);
    countdownTimer = null;
  }
});
</script>

<template>
  <view class="auth-container">
    <NotificationSystem />
    <view class="auth-card">
      <view class="title">{{ APP_TITLE }}</view>
      <view class="tabs">
        <view :class="['tab', mode === 'login' ? 'active' : '']" @click="switchMode('login')">
          登录
        </view>
        <view :class="['tab', mode === 'register' ? 'active' : '']" @click="switchMode('register')">
          注册
        </view>
      </view>

      <view class="form">
        <view class="login-methods" v-if="mode === 'login'">
          <view :class="['method-tab', loginMethod === 'email' ? 'active' : '']" @click="loginMethod = 'email'">邮箱登录
          </view>
          <view :class="['method-tab', loginMethod === 'account' ? 'active' : '']" @click="loginMethod = 'account'">账号登录
          </view>
        </view>

        <view class="field" v-if="mode === 'login' && loginMethod === 'email' || mode === 'register'">
          <text>邮箱</text>
          <input class="input" v-model="email" type="text" placeholder="请输入邮箱" placeholder-class="input-placeholder" />
        </view>
        <view v-if="mode === 'register'" class="field code-field">
          <text>邮箱验证码</text>
          <view class="code-row">
            <input class="input" v-model="code" type="number" maxlength="6" placeholder="验证码"
              placeholder-class="input-placeholder" />
            <button class="code-button" :disabled="sendingCode || countdown > 0" @click="handleSendCode">
              <text v-if="countdown > 0">{{ countdown }}s</text>
              <text v-else-if="sendingCode">发送中...</text>
              <text v-else>获取验证码</text>
            </button>
          </view>
        </view>

        <view class="field" v-if="mode === 'login' && loginMethod === 'account' || mode === 'register'">
          <text>账号名</text>
          <view class="input-row">
            <input class="input" :class="{ 'input-error': userAccountError }" v-model="userAccount" type="text"
              placeholder="账号名" placeholder-class="input-placeholder" @blur="validateuserAccount"
              @input="userAccountError = ''" />
            <button v-if="mode === 'register'" class="mini-btn" @click="generateRandomuserAccount">随机</button>
          </view>
          <text v-if="userAccountError" class="error-msg">{{ userAccountError }}</text>
        </view>

        <view class="field">
          <text>密码</text>
          <input class="input" v-model="password" type="password" password placeholder="请输入密码"
            placeholder-class="input-placeholder" />
        </view>

        <view v-if="mode === 'register'" class="field">
          <text>确认密码</text>
          <input class="input" v-model="confirmPassword" type="password" password placeholder="再次输入密码"
            placeholder-class="input-placeholder" />
        </view>

        <view v-if="mode === 'register'" class="field">
          <text>昵称（可选）</text>
          <input class="input" v-model="nickName" type="text" placeholder="您的昵称"
            placeholder-class="input-placeholder" />
        </view>

        <button class="submit" :loading="loading" @click="handleSubmit">
          {{ mode === 'login' ? '登录' : '注册并登录' }}
        </button>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.auth-container {
  min-height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: var(--bg-color);
  /* fallback */
  background: radial-gradient(circle at top, var(--surface-soft), var(--bg-color));
}

.auth-card {
  width: 80%;
  max-width: 400px;
  background-color: var(--bg-elevated);
  padding: 32px;
  border-radius: var(--uni-card-border-radius);
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.2);
}

.title {
  text-align: center;
  margin-bottom: 24px;
  font-size: 24px;
  font-weight: 600;
  color: var(--text-color);
}

.tabs {
  display: flex;
  margin-bottom: 24px;
  background-color: var(--bg-color);
  padding: 4px;
  border-radius: 8px;
}

.tab {
  flex: 1;
  text-align: center;
  padding: 8px;
  border-radius: 6px;
  color: var(--text-muted);
  font-size: 14px;
  transition: all 0.2s;
}

.tab.active {
  background-color: var(--bg-elevated);
  color: var(--text-color);
  font-weight: 500;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.login-methods {
  display: flex;
  gap: 16px;
  margin-bottom: 16px;
}

.method-tab {
  font-size: 14px;
  color: var(--text-muted);
  padding-bottom: 4px;
  border-bottom: 2px solid transparent;
}

.method-tab.active {
  color: var(--text-color);
  border-bottom-color: var(--accent-color);
  font-weight: 500;
}

.form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field text {
  font-size: 14px;
  color: var(--text-muted);
}

.input {
  width: 100%;
  height: 44px;
  padding: 0 10px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background-color: var(--bg-color);
  color: var(--text-color);
  font-size: 14px;
  box-sizing: border-box;
}

.input-placeholder {
  color: var(--text-muted);
  opacity: 0.5;
}

.input-row {
  display: flex;
  gap: 8px;
}

.mini-btn {
  width: 60px;
  height: 44px;
  line-height: 44px;
  text-align: center;
  padding: 0;
  font-size: 12px;
  border-radius: 8px;
  background-color: var(--bg-elevated);
  border: 1px solid var(--border-color);
  color: var(--text-color);
}

.mini-btn::after {
  border: none;
}

.code-field .code-row {
  display: flex;
  gap: 8px;
}

.code-button {
  width: 100px;
  height: 44px;
  line-height: 44px;
  text-align: center;
  padding: 0;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background-color: var(--bg-elevated);
  color: var(--text-color);
  font-size: 12px;
}

.code-button::after {
  border: none;
}

.code-button[disabled] {
  opacity: 0.5;
}

.submit {
  margin-top: 24px;
  width: 100%;
  height: 44px;
  line-height: 44px;
  border-radius: 8px;
  background-color: var(--accent-color);
  color: var(--accent-on);
  font-weight: 600;
  font-size: 16px;
  border: none;
}

.submit::after {
  border: none;
}

.input-error {
  border-color: #ef4444 !important;
}

.error-msg {
  font-size: 12px;
  color: #ef4444;
  margin-top: 4px;
}
</style>
