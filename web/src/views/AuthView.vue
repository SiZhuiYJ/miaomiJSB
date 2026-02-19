<script setup lang="ts">
import { onUnmounted, ref, watch } from "vue";
import { useAuthStore } from "@/features/auth/stores";
import { notifySuccess, notifyError } from "../utils/notification";
import { authApi } from "@/features/auth/api";

const mode = ref<"login" | "register">("login");
const loginMethod = ref<"email" | "account">("email");
const loading = ref(false);

const email = ref("");
const password = ref("");
const nickName = ref("");
const userAccount = ref("");
const confirmPassword = ref("");
const code = ref("");
const sendingCode = ref(false);
const countdown = ref(0);
const userAccountError = ref("");
const lastAutouserAccount = ref("");

let countdownTimer: number | null = null;

watch(email, (newEmail) => {
  if (mode.value === "register") {
    const prefix = newEmail.split("@")[0] || "";
    // If userAccount is empty or matches the last auto-filled value, update it
    if (!userAccount.value || userAccount.value === lastAutouserAccount.value) {
      userAccount.value = prefix;
      lastAutouserAccount.value = prefix;
      userAccountError.value = "";
    }
  }
});

function generateRandomuserAccount(): void {
  userAccount.value = "user_" + Math.random().toString(36).slice(2, 10);
  userAccountError.value = ""; // Clear error when generating new
}

async function validateUserAccount(): Promise<void> {
  if (!userAccount.value || mode.value !== "register") return;

  try {
    await authApi.ValidateUserAccount(encodeURIComponent(userAccount.value));
    userAccountError.value = "";
  } catch (error: any) {
    if (error.response?.status === 409) {
      userAccountError.value = "账号名已存在";
    } else {
      userAccountError.value = "";
    }
  }
}

function startCountdown(seconds: number): void {
  countdown.value = seconds;
  if (countdownTimer !== null) {
    window.clearInterval(countdownTimer);
  }
  countdownTimer = window.setInterval(() => {
    if (countdown.value > 0) {
      countdown.value -= 1;
    } else {
      if (countdownTimer !== null) {
        window.clearInterval(countdownTimer);
        countdownTimer = null;
      }
    }
  }, 1000);
}

const auth = useAuthStore();

async function handleSubmit(): Promise<void> {
  if (mode.value === "register") {
    if (!userAccount.value) {
      notifyError("请输入账号名");
      return;
    }
    // Ensure userAccount is validated
    if (userAccountError.value) {
      notifyError(userAccountError.value);
      return;
    }
    // Optional: Double check if validation wasn't triggered (e.g. user typed and hit enter immediately)
    await validateUserAccount();
    if (userAccountError.value) {
      notifyError(userAccountError.value);
      return;
    }

    if (!code.value) {
      notifyError("请输入邮箱验证码");
      return;
    }
    if (password.value.length < 6) {
      notifyError("密码长度至少为 6 位");
      return;
    }
    if (password.value !== confirmPassword.value) {
      notifyError("两次输入的密码不一致");
      return;
    }
  }
  loading.value = true;
  try {
    if (mode.value === "login") {
      if (loginMethod.value === "email") {
        const response = await authApi.LoginWithEmail({
          email: email.value,
          password: password.value,
        });
        auth.setSession(response.data);
      } else {
        const response = await authApi.LoginWithAccount({
          userAccount: userAccount.value,
          password: password.value,
        });
        auth.setSession(response.data);
      }
      notifySuccess("登录成功");
    } else {
      const response = await authApi.Register({
        email: email.value,
        password: password.value,
        nickName: nickName.value || null,
        userAccount: userAccount.value || null,
        code: code.value,
      });
      auth.setSession(response.data);
      notifySuccess("注册并登录成功");
    }
  } catch (error: any) {
    if (error.response?.status === 401) {
      notifyError("登录失败：账号或密码错误");
    } else if (error.response?.status === 409) {
      notifyError("注册失败：邮箱或用户名已被占用");
    } else {
      notifyError("操作失败，请检查输入");
    }
  } finally {
    loading.value = false;
  }
}

async function handleSendCode(): Promise<void> {
  if (!email.value) {
    notifyError("请先填写邮箱");
    return;
  }
  if (sendingCode.value || countdown.value > 0) {
    return;
  }
  sendingCode.value = true;
  try {
    await authApi.SendEmilCode({
      email: email.value,
      actionType: "register",
    });
    notifySuccess("验证码已发送，请检查邮箱");
    startCountdown(60);
  } catch (error: any) {
    const status = error?.response?.status as number | undefined;
    if (status === 429) {
      notifyError("请求过于频繁，请稍后再试");
    } else if (status === 409) {
      notifyError("该邮箱已注册");
    } else if (status === 400) {
      notifyError("邮箱格式不正确或请求无效");
    } else {
      notifyError("发送验证码失败，请稍后重试");
    }
  } finally {
    sendingCode.value = false;
  }
}

function switchMode(next: "login" | "register"): void {
  mode.value = next;
}

onUnmounted(() => {
  if (countdownTimer !== null) {
    window.clearInterval(countdownTimer);
    countdownTimer = null;
  }
});
</script>

<template>
  <div class="auth-container">
    <div class="auth-card">
      <h1 class="title">DailyCheck 每日打卡</h1>
      <div class="tabs">
        <button
          type="button"
          :class="['tab', mode === 'login' ? 'active' : '']"
          @click="switchMode('login')"
        >
          登录
        </button>
        <button
          type="button"
          :class="['tab', mode === 'register' ? 'active' : '']"
          @click="switchMode('register')"
        >
          注册
        </button>
      </div>

      <form class="form" @submit.prevent="handleSubmit">
        <div class="login-methods" v-if="mode === 'login'">
          <label class="radio-label">
            <input type="radio" value="email" v-model="loginMethod" /> 邮箱登录
          </label>
          <label class="radio-label">
            <input type="radio" value="account" v-model="loginMethod" />
            账号登录
          </label>
        </div>

        <label
          class="field"
          v-if="
            (mode === 'login' && loginMethod === 'email') || mode === 'register'
          "
        >
          <span>邮箱</span>
          <input
            v-model="email"
            type="email"
            :required="
              (mode === 'login' && loginMethod === 'email') ||
              mode === 'register'
            "
            placeholder="请输入邮箱"
          />
        </label>

        <div v-if="mode === 'register'" class="field code-field">
          <span>邮箱验证码</span>
          <div class="code-row">
            <input
              v-model="code"
              type="text"
              maxlength="6"
              inputmode="numeric"
              placeholder="请输入收到的验证码"
            />
            <button
              type="button"
              class="code-button"
              :disabled="sendingCode || countdown > 0"
              @click="handleSendCode"
            >
              <span v-if="countdown > 0">{{ countdown }}s</span>
              <span v-else-if="sendingCode">发送中...</span>
              <span v-else>获取验证码</span>
            </button>
          </div>
        </div>

        <label
          class="field"
          v-if="
            (mode === 'login' && loginMethod === 'account') ||
            mode === 'register'
          "
        >
          <span>账号</span>
          <div class="input-group">
            <input
              v-model="userAccount"
              type="text"
              :required="mode === 'login' && loginMethod === 'account'"
              placeholder="请输入账号"
              @blur="validateUserAccount"
              @input="userAccountError = ''"
              :class="{ 'input-error': userAccountError }"
            />
            <button
              v-if="mode === 'register'"
              type="button"
              class="btn-sm"
              @click="generateRandomuserAccount"
            >
              随机
            </button>
          </div>
          <span v-if="userAccountError" class="error-msg">{{
            userAccountError
          }}</span>
        </label>

        <label class="field">
          <span>密码</span>
          <input
            v-model="password"
            type="password"
            required
            placeholder="请输入密码"
          />
        </label>

        <label v-if="mode === 'register'" class="field">
          <span>确认密码</span>
          <input
            v-model="confirmPassword"
            type="password"
            required
            placeholder="请确认密码"
          />
        </label>

        <label v-if="mode === 'register'" class="field">
          <span>昵称（可选）</span>
          <input v-model="nickName" type="text" placeholder="请输入昵称" />
        </label>

        <button class="submit" type="submit" :disabled="loading">
          {{ mode === "login" ? "登录" : "注册并登录" }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped lang="scss">
.auth-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(
    circle at top,
    var(--surface-soft),
    var(--bg-color)
  );
}

.auth-card {
  width: 100%;
  max-width: 400px;
  background: var(--bg-elevated);
  padding: 32px;
  border-radius: 16px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.2);
  border: 1px solid var(--border-color);
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
  background: var(--bg-color);
  padding: 4px;
  border-radius: 8px;
}

.tab {
  flex: 1;
  border: none;
  background: transparent;
  padding: 8px;
  border-radius: 6px;
  color: var(--text-muted);
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
}

.tab.active {
  background: var(--bg-elevated);
  color: var(--text-color);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.login-methods {
  display: flex;
  gap: 16px;
  margin-bottom: 8px;
}

.radio-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  cursor: pointer;
  color: var(--text-color);
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;

  span {
    font-size: 14px;
    color: var(--text-muted);
  }

  input {
    width: 100%;
    padding: 10px;
    border-radius: 8px;
    border: 1px solid var(--border-color);
    background: var(--bg-color);
    color: var(--text-color);
    font-size: 14px;
    transition: border-color 0.2s;
    box-sizing: border-box;
  }
}

input:focus {
  border-color: var(--accent-color);
  outline: none;
}

.input-group {
  display: flex;
  gap: 8px;
}

.btn-sm {
  padding: 0 12px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: var(--bg-elevated);
  color: var(--text-color);
  cursor: pointer;
  font-size: 12px;
  white-space: nowrap;
}

.code-field .code-row {
  display: flex;
  gap: 8px;
}

.code-button {
  width: 100px;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  background: var(--bg-elevated);
  color: var(--text-color);
  cursor: pointer;
  font-size: 12px;
}

.code-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.submit {
  margin-top: 8px;
  padding: 12px;
  border-radius: 8px;
  border: none;
  background: linear-gradient(to right, var(--accent-color), var(--accent-alt));
  color: var(--accent-on);
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s;
}

.submit:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.input-error {
  border-color: #ef4444;
}

.error-msg {
  font-size: 12px;
  color: #ef4444;
}

@media (max-width: 480px) {
  .auth-card {
    margin: 0 16px;
    padding-inline: 16px;
    padding-top: 20px;
    padding-bottom: 24px;
  }

  .title {
    font-size: 20px;
  }

  .tab {
    padding: 10px 0;
    font-size: 15px;
  }

  .submit {
    padding: 12px 0;
    font-size: 16px;
  }
}
</style>
