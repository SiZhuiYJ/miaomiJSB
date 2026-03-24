<script setup lang="ts">
import { notifySuccess, notifyError } from "@/utils/notification";
import { ElMessageBox } from "element-plus";
import { authApi } from "@/features/auth/api/index";
import { useAuthStore } from "@/features/auth/stores";
const authStore = useAuthStore();

import { useCode } from "@/features/auth/composables/useCode";
const { codeCountdown, sendingCode, handleSendVerificationCode } = useCode();

const deactivateCode = ref("");
const deactivateLoading = ref(false);

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
    <span class="footer">
        <button class="btn-confirm btn-danger" :disabled="deactivateLoading" @click="handleDeactivateConfirm">
            {{ deactivateLoading ? "处理中..." : "确认注销" }}
        </button>
    </span>
</template>

<style scoped lang="scss">
.profile-form {
    display: flex;
    flex-direction: column;
    gap: 12px;
    width: 80%;


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

.warning-text {
    font-size: 14px;
    color: #ef4444;
    margin: 0;
    line-height: 1.5;
}

.footer {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
}

.btn-danger {
    background: #ef4444;

    &:hover {
        background: #dc2626;
    }
}

.btn-confirm {
    padding: 8px 16px;
    border-radius: 6px;
    border: none;
    background: red;
    color: var(--accent-on);
    cursor: pointer;

    &:disabled {
        opacity: 0.7;
        cursor: not-allowed;
    }
}

.code-row {
    display: flex;
    gap: 8px;
    padding: 4px 0;
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

    &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }
}
</style>
