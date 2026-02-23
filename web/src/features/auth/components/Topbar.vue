<script setup lang="ts">
import { ref, reactive, h } from "vue";
import { useAuthStore } from "@/features/auth/stores";
import { authApi } from "@/features/auth/api/index";
import type { PasswordPayload } from "@/features/auth/types";
import { ElMessageBox } from "element-plus";
import { notifySuccess, notifyError } from "@/utils/notification";
import { APP_TITLE, API_BASE_URL } from "@/config";
import { storeToRefs } from "pinia";
import router from "@/routers/index";
const { user } = storeToRefs(useAuthStore());

// SVG Icons
const MoonIcon = {
  render: () =>
    h("svg", { viewBox: "0 0 24 24", width: "1.2em", height: "1.2em" }, [
      h("path", {
        fill: "currentColor",
        d: "M12 3a9 9 0 1 0 9 9c0-.46-.04-.92-.1-1.36a5.389 5.389 0 0 1-4.4 2.26 5.403 5.403 0 0 1-3.14-9.8c-.44-.06-.9-.1-1.36-.1z",
      }),
    ]),
};

const SunIcon = {
  render: () =>
    h("svg", { viewBox: "0 0 24 24", width: "1.2em", height: "1.2em" }, [
      h("path", {
        fill: "currentColor",
        d: "M6.76 4.84l-1.8-1.79-1.41 1.41 1.79 1.79 1.42-1.41zM4 10.5H1v2h3v-2zm9-9.95h-2V3.5h2V.55zm7.45 3.91l-1.41-1.41-1.79 1.79 1.41 1.41 1.79-1.79zm-3.21 13.7l1.79 1.79 1.41-1.41-1.79-1.79-1.41 1.41zM20 10.5v2h3v-2h-3zm-8-5c-3.31 0-6 2.69-6 6s2.69 6 6 6 6-2.69 6-6-2.69-6-6-6zm-1 16.95h2V19.5h-2v2.95zm-7.45-3.91l1.41 1.41 1.79-1.8-1.41-1.41-1.79 1.8z",
      }),
    ]),
};

const authStore = useAuthStore();

const initialTheme =
  document.documentElement.getAttribute("data-theme") === "dark"
    ? "dark"
    : "light";
const theme = ref<"dark" | "light">(initialTheme);

function applyTheme(next: "dark" | "light"): void {
  theme.value = next;
  document.documentElement.setAttribute(
    "data-theme",
    next === "dark" ? "dark" : "light",
  );
  if (next === "dark") {
    document.documentElement.classList.add("dark");
  } else {
    document.documentElement.classList.remove("dark");
  }
  localStorage.setItem("theme", next);
}

function toggleTheme(): void {
  applyTheme(theme.value === "dark" ? "light" : "dark");
}

function handleCommand(command: string): void {
  if (command === "logout") {
    authStore.clear();
    router.push("/login");
  } else if (command === "profile") {
    // openProfileDialog();
    router.push("/setting");
  } else if (command === "changePassword") {
    openChangePasswordDialog();
  } else if (command === "deactivate") {
    openDeactivateDialog();
  }
}

// Verification Code Logic
const codeCountdown = ref(0);
let codeTimer: number | null = null;
const sendingCode = ref(false);

function startCodeCountdown(seconds: number = 60) {
  codeCountdown.value = seconds;
  if (codeTimer) clearInterval(codeTimer);
  codeTimer = window.setInterval(() => {
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

async function handleSendVerificationCode(
  actionType: "change-password" | "deactivate",
) {
  if (!authStore.user?.email) {
    notifyError("无法获取用户邮箱");
    return;
  }
  if (sendingCode.value || codeCountdown.value > 0) return;

  sendingCode.value = true;
  try {
    if (user.value) {
      authApi.sendEmailCode({ email: user.value.email, actionType });
      notifySuccess("验证码已发送");
      startCodeCountdown();
    } else {
      notifyError("邮箱为空");
    }
  } catch (error: any) {
    const status = error.response?.status;
    if (status === 429) notifyError("请求过于频繁");
    else notifyError("发送失败");
  } finally {
    sendingCode.value = false;
  }
}

// Profile Dialog
const profileVisible = ref(false);
const profileForm = reactive({
  userAccount: "",
  nickName: "",
  avatarKey: "",
});
const profileLoading = ref(false);

async function handleUpdateProfile(): Promise<void> {
  profileLoading.value = true;
  try {
    const response = await authApi.updateProfile({
      nickName: profileForm.nickName || null,
      avatarKey: profileForm.avatarKey || null,
    });
    authStore.setSession(response.data);
    notifySuccess("用户信息更新成功");
    profileVisible.value = false;
  } catch (error: any) {
    if (error.response?.status === 409) {
      notifyError("用户名已被占用");
    } else {
      notifyError("更新失败");
    }
  } finally {
    profileLoading.value = false;
  }
}

// Change Password Dialog
const changePasswordVisible = ref(false);
const verificationMethod = ref<"password" | "code">("password");
const changePasswordForm = reactive({
  oldPassword: "",
  newPassword: "",
  confirmPassword: "",
  code: "",
});
const changePasswordLoading = ref(false);

function openChangePasswordDialog() {
  changePasswordForm.oldPassword = "";
  changePasswordForm.newPassword = "";
  changePasswordForm.confirmPassword = "";
  changePasswordForm.code = "";
  verificationMethod.value = "password";
  changePasswordVisible.value = true;
}

async function handleChangePassword() {
  if (changePasswordForm.newPassword !== changePasswordForm.confirmPassword) {
    notifyError("两次输入的新密码不一致");
    return;
  }

  if (verificationMethod.value === "code" && !changePasswordForm.code) {
    notifyError("请输入验证码");
    return;
  }

  if (
    verificationMethod.value === "password" &&
    !changePasswordForm.oldPassword
  ) {
    notifyError("请输入旧密码");
    return;
  }

  changePasswordLoading.value = true;
  try {
    const payload: PasswordPayload = {
      oldPassword: null,
      newPassword: changePasswordForm.newPassword,
      code: null,
    };

    if (verificationMethod.value === "password") {
      payload.oldPassword = changePasswordForm.oldPassword;
    } else {
      payload.code = changePasswordForm.code;
    }

    await authApi.updatePassword(payload);
    notifySuccess("密码修改成功");
    changePasswordVisible.value = false;
    authStore.clear();
  } catch (error: any) {
    if (error.response?.status === 401) notifyError("旧密码错误");
    else if (error.response?.status === 400)
      notifyError(error.response.data || "请求无效");
    else notifyError("修改失败");
  } finally {
    changePasswordLoading.value = false;
  }
}

// Deactivate Account Dialog
const deactivateVisible = ref(false);
const deactivateCode = ref("");
const deactivateLoading = ref(false);

function openDeactivateDialog() {
  deactivateCode.value = "";
  deactivateVisible.value = true;
}

async function handleDeactivateConfirm(): Promise<void> {
  if (!deactivateCode.value) {
    notifyError("请输入验证码");
    return;
  }

  try {
    await ElMessageBox.confirm(
      "确定要注销账号吗？注销后将无法登录，数据将保留30天。",
      "注销确认",
      {
        confirmButtonText: "确认注销",
        cancelButtonText: "取消",
        type: "warning",
      },
    );

    deactivateLoading.value = true;
    await authApi.deactivateConfirm({ code: deactivateCode.value });
    notifySuccess("账号已注销");
    deactivateVisible.value = false;
    authStore.clear();
  } catch (error: any) {
    if (error === "cancel") return;
    if (error.response?.status === 400)
      notifyError(error.response.data || "验证码错误");
    else notifyError("注销失败");
  } finally {
    deactivateLoading.value = false;
  }
}
</script>

<template>
  <header class="topbar">
    <div class="brand">{{ APP_TITLE }}</div>
    <div class="right">
      <button type="button" class="theme-toggle-icon" @click="toggleTheme"
        :title="theme === 'dark' ? '切换到白昼模式' : '切换到暗夜模式'">
        <component :is="theme === 'dark' ? SunIcon : MoonIcon" />
      </button>
      <!-- 头像 -->

      <el-dropdown v-if="user" trigger="click" placement="bottom-end" popper-class="user-dropdown-popper"
        @command="handleCommand">
        <div class="user-info-trigger">
          <el-image fit="cover" :src="`${API_BASE_URL}mm/Files/users/${user.userId}/${user.avatarKey}`"
            class="avatar-img" mode="aspectFill" />
          <span class="email">
            {{ user.email }}
            <span v-if="user.nickName">({{ user.nickName }})</span>
          </span>
          <span class="dropdown-arrow">▼</span>
        </div>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item command="profile">个人设置</el-dropdown-item>
            <el-dropdown-item command="changePassword">修改密码</el-dropdown-item>
            <el-dropdown-item command="deactivate" divided style="color: #ef4444">注销账号</el-dropdown-item>
            <el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>

    <el-dialog v-model="profileVisible" title="个人设置" width="400px">
      <div class="profile-form">
        <label class="field">
          <span>账号名 (全局唯一)</span>
          <input v-model="profileForm.userAccount" type="text" placeholder="设置账号名" />
        </label>
        <label class="field">
          <span>昵称</span>
          <input v-model="profileForm.nickName" type="text" placeholder="设置昵称" />
        </label>
        <label class="field">
          <span>头像 URL</span>
          <input v-model="profileForm.avatarKey" type="text" placeholder="https://..." />
        </label>
      </div>
      <template #footer>
        <span class="dialog-footer">
          <button class="btn-cancel" @click="profileVisible = false">
            取消
          </button>
          <button class="btn-confirm" :disabled="profileLoading" @click="handleUpdateProfile">
            {{ profileLoading ? "保存中..." : "保存" }}
          </button>
        </span>
      </template>
    </el-dialog>

    <!-- Change Password Dialog -->
    <el-dialog v-model="changePasswordVisible" title="修改密码" width="400px">
      <div class="profile-form">
        <div class="method-selector">
          <label class="radio-label">
            <input type="radio" value="password" v-model="verificationMethod" />
            旧密码验证
          </label>
          <label class="radio-label">
            <input type="radio" value="code" v-model="verificationMethod" />
            验证码验证
          </label>
        </div>

        <label class="field" v-if="verificationMethod === 'password'">
          <span>旧密码</span>
          <input v-model="changePasswordForm.oldPassword" type="password" placeholder="请输入旧密码" />
        </label>

        <div class="field code-field" v-if="verificationMethod === 'code'">
          <span>邮箱验证码</span>
          <div class="code-row">
            <input v-model="changePasswordForm.code" type="text" maxlength="6" placeholder="验证码" />
            <button type="button" class="code-button" :disabled="sendingCode || codeCountdown > 0"
              @click="handleSendVerificationCode('change-password')">
              <span v-if="codeCountdown > 0">{{ codeCountdown }}s</span>
              <span v-else-if="sendingCode">发送中...</span>
              <span v-else>获取验证码</span>
            </button>
          </div>
        </div>

        <label class="field">
          <span>新密码</span>
          <input v-model="changePasswordForm.newPassword" type="password" placeholder="请输入新密码" />
        </label>
        <label class="field">
          <span>确认新密码</span>
          <input v-model="changePasswordForm.confirmPassword" type="password" placeholder="请再次输入新密码" />
        </label>
      </div>
      <template #footer>
        <span class="dialog-footer">
          <button class="btn-cancel" @click="changePasswordVisible = false">
            取消
          </button>
          <button class="btn-confirm" :disabled="changePasswordLoading" @click="handleChangePassword">
            {{ changePasswordLoading ? "提交中..." : "确认修改" }}
          </button>
        </span>
      </template>
    </el-dialog>

    <!-- Deactivate Account Dialog -->
    <el-dialog v-model="deactivateVisible" title="注销账号" width="400px">
      <div class="profile-form">
        <p class="warning-text">
          注销后账号将无法登录，所有数据保留30天后彻底删除。请输入验证码确认注销。
        </p>
        <div class="field code-field">
          <span>邮箱验证码</span>
          <div class="code-row">
            <input v-model="deactivateCode" type="text" maxlength="6" placeholder="验证码" />
            <button type="button" class="code-button" :disabled="sendingCode || codeCountdown > 0"
              @click="handleSendVerificationCode('deactivate')">
              <span v-if="codeCountdown > 0">{{ codeCountdown }}s</span>
              <span v-else-if="sendingCode">发送中...</span>
              <span v-else>获取验证码</span>
            </button>
          </div>
        </div>
      </div>
      <template #footer>
        <span class="dialog-footer">
          <button class="btn-cancel" @click="deactivateVisible = false">
            取消
          </button>
          <button class="btn-confirm btn-danger" :disabled="deactivateLoading" @click="handleDeactivateConfirm">
            {{ deactivateLoading ? "处理中..." : "确认注销" }}
          </button>
        </span>
      </template>
    </el-dialog>
  </header>
</template>

<style scoped lang="scss">
.topbar {
  height: 56px;
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-elevated);
}

.brand {
  font-weight: 600;
}

.right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.avatar-img {
  width: 45px;
  height: 45px;
  border-radius: 10px;
}

.user-info-trigger {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: background-color 0.2s;
}

.user-info-trigger:hover {
  background-color: var(--bg-color);
}

.email {
  font-size: 14px;
  color: var(--text-muted);
}

.dropdown-arrow {
  font-size: 10px;
  color: var(--text-muted);
}

.theme-toggle-icon {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1px solid var(--border-color);
  background: transparent;
  color: var(--text-color);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.theme-toggle-icon:hover {
  background: var(--bg-color);
  color: var(--accent-color);
}

.profile-form {
  display: flex;
  flex-direction: column;
  gap: 12px;

  .field {
    display: flex;
    flex-direction: column;
    gap: 4px;

    input {
      width: 100%;
      padding: 8px 10px;
      border-radius: 6px;
      border: 1px solid var(--border-color);
      background: var(--bg-color);
      color: var(--text-color);
      font-size: 14px;
      box-sizing: border-box;
    }

    span {
      font-size: 14px;
      color: var(--text-muted);
    }
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.btn-cancel {
  padding: 8px 16px;
  border-radius: 6px;
  border: 1px solid var(--border-color);
  background: transparent;
  color: var(--text-color);
  cursor: pointer;
}

.btn-danger {
  background: #ef4444;
}

.btn-danger:hover {
  background: #dc2626;
}

.code-row {
  display: flex;
  gap: 8px;
}

.code-button {
  width: 100px;
  border-radius: 6px;
  border: 1px solid var(--border-color);
  background: var(--bg-elevated);
  color: var(--text-color);
  cursor: pointer;
  font-size: 12px;
  white-space: nowrap;
  display: flex;
  align-items: center;
  justify-content: center;
}

.code-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.warning-text {
  font-size: 14px;
  color: #ef4444;
  margin: 0;
  line-height: 1.5;
}

.btn-confirm {
  padding: 8px 16px;
  border-radius: 6px;
  border: none;
  background: linear-gradient(to right, var(--accent-color), var(--accent-alt));
  color: var(--accent-on);
  cursor: pointer;
}

.btn-confirm:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.method-selector {
  display: flex;
  gap: 16px;
  margin-bottom: 12px;
}

.radio-label {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  cursor: pointer;
  color: var(--text-color);
}

.theme-toggle-icon {
  --primary-color: royalblue;
  padding: 5px 5px;
  color: #000000d9;
  border: 1px solid #d9d9d9;
  border-radius: 50%;
  line-height: 1.4;
  box-shadow: 0 2px #00000004;
  cursor: pointer;
  transition: .3s;
  transform: scale(1);
  border-style: dashed;

}

.theme-toggle-icon:focus-visible {
  outline: 0;
}

.theme-toggle-icon::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
  box-shadow: 0 0 0 6px var(--primary-color);
  opacity: 0;
  transition: .3s;
}


.theme-toggle-icon:hover,
.theme-toggle-icon:focus {
  color: var(--primary-color);
  border-color: currentColor;
}

.theme-toggle-icon:active {
  filter: brightness(.9);
}

.theme-toggle-icon:active::after {
  transition: 0s;
  box-shadow: none;
  opacity: 0.4;
}
</style>

<style>
/* Global override for the user dropdown arrow */
.user-dropdown-popper .el-popper__arrow {
  left: 50% !important;
  transform: translateX(-50%) !important;
}
</style>
