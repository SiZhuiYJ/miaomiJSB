<script setup lang="ts">
import { ref } from 'vue'
import EmailInput from './EmailInput.vue'
import AccountInput from './AccountInput.vue'
import PasswordInput from './PasswordInput.vue'
import LoginMethods from './LoginMethods.vue'

interface LoginFormProps {
    email: string
    userAccount: string
    password: string
    loginMethod: 'email' | 'account'
}

interface Emits {
    (e: 'update:email', value: string): void
    (e: 'update:userAccount', value: string): void
    (e: 'update:password', value: string): void
    (e: 'update:loginMethod', value: 'email' | 'account'): void
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
    <form class="form" @submit.prevent="handleSubmit">
        <LoginMethods :model-value="loginMethod" @update:model-value="$emit('update:loginMethod', $event)" />

        <EmailInput v-if="loginMethod === 'email'" :model-value="email"
            @update:model-value="$emit('update:email', $event)" />

        <AccountInput v-if="loginMethod === 'account'" :model-value="userAccount" :required="true"
            @update:model-value="$emit('update:userAccount', $event)" />

        <PasswordInput :model-value="password" :required="true"
            @update:model-value="$emit('update:password', $event)" />

        <button class="submit" type="submit" :disabled="loading">
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