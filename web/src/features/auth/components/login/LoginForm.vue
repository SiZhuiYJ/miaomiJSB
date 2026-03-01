<script setup lang="ts">
import { ref } from 'vue'
import EmailInput from './EmailInput.vue'
import AccountInput from './AccountInput.vue'
import PasswordInput from './PasswordInput.vue'
import LoginMethods from './LoginMethods.vue'
import EmailCodeLogin from './EmailCodeLogin.vue'

interface LoginFormProps {
    email: string
    userAccount: string
    password: string
    code: string
    sendingCode: boolean
    countdown: number
    loginMethod: 'email' | 'account' | 'email-code'
}

interface Emits {
    (e: 'update:email', value: string): void
    (e: 'update:userAccount', value: string): void
    (e: 'update:password', value: string): void
    (e: 'update:code', value: string): void
    (e: 'update:loginMethod', value: 'email' | 'account' | 'email-code'): void
    (e: 'sendCode'): void
    (e: 'submit'): void
}

const props = defineProps<LoginFormProps>()
const emit = defineEmits<Emits>()

const loading = ref(false)

function handleSubmit() {
    loading.value = true
    emit('submit')
    // 在父组件处理完后会重置loading状态
}

defineExpose({
    setLoading: (value: boolean) => {
        loading.value = value
    }
})
</script>

<template>
    <!-- 邮箱密码登录 -->
    <form v-if="loginMethod === 'email'" class="form" @submit.prevent="handleSubmit">
        <LoginMethods :model-value="loginMethod" @update:model-value="$emit('update:loginMethod', $event)" />

        <EmailInput :model-value="email" @update:model-value="$emit('update:email', $event)" />
        <PasswordInput :model-value="password" :required="true" @update:model-value="$emit('update:password', $event)" />

        <button class="submit" type="submit" :disabled="loading">
            登录
        </button>
    </form>

    <!-- 账号密码登录 -->
    <form v-else-if="loginMethod === 'account'" class="form" @submit.prevent="handleSubmit">
        <LoginMethods :model-value="loginMethod" @update:model-value="$emit('update:loginMethod', $event)" />

        <AccountInput :model-value="userAccount" :required="true" @update:model-value="$emit('update:userAccount', $event)" />
        <PasswordInput :model-value="password" :required="true" @update:model-value="$emit('update:password', $event)" />

        <button class="submit" type="submit" :disabled="loading">
            登录
        </button>
    </form>

    <!-- 邮箱验证码登录 -->
    <EmailCodeLogin 
        v-else-if="loginMethod === 'email-code'"
        :email="email"
        :code="code"
        :sending-code="sendingCode"
        :countdown="countdown"
        :login-method="loginMethod"
        @update:email="$emit('update:email', $event)"
        @update:code="$emit('update:code', $event)"
        @update:login-method="$emit('update:loginMethod', $event)"
        @send-code="$emit('sendCode')"
        @submit="handleSubmit"
    />
</template>

<style scoped lang="scss">
.form {
    display: flex;
    flex-direction: column;
    gap: 16px;
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

@media (max-width: 480px) {
    .submit {
        padding: 12px 0;
        font-size: 16px;
    }
}
</style>