<script setup lang="ts">
import { ref, computed } from 'vue'
import EmailInput from './EmailInput.vue'
import VerificationCodeInput from './VerificationCodeInput.vue'
import LoginMethods from './LoginMethods.vue'

interface EmailCodeLoginProps {
    email: string
    code: string
    sendingCode: boolean
    countdown: number
    loginMethod: 'email' | 'account' | 'email-code'
}

interface Emits {
    (e: 'update:email', value: string): void
    (e: 'update:code', value: string): void
    (e: 'update:loginMethod', value: 'email' | 'account' | 'email-code'): void
    (e: 'sendCode'): void
    (e: 'submit'): void
}

const props = defineProps<EmailCodeLoginProps>()
const emit = defineEmits<Emits>()

const loading = ref(false)

const isSendDisabled = computed(() => {
    return props.sendingCode || props.countdown > 0 || !props.email
})

function handleSubmit() {
    if (!props.email || !props.code) {
        return
    }
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
    <form class="form" @submit.prevent="handleSubmit">
        <LoginMethods 
            :model-value="loginMethod" 
            @update:model-value="$emit('update:loginMethod', $event)" 
        />

        <EmailInput 
            :model-value="email"
            @update:model-value="$emit('update:email', $event)"
        />
        
        <VerificationCodeInput
            :model-value="code"
            :sending="sendingCode"
            :countdown="countdown"
            placeholder="请输入验证码"
            @update:model-value="$emit('update:code', $event)"
            @send-code="$emit('sendCode')"
        />

        <button class="submit" type="submit" :disabled="loading || !email || !code">
            登录
        </button>
    </form>
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