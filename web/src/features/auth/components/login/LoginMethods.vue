<script setup lang="ts">
interface Props {
    modelValue: 'email' | 'account' | 'email-code'
}

interface Emits {
    (e: 'update:modelValue', value: 'email' | 'account' | 'email-code'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()
</script>

<template>
    <div class="login-methods">
        <label class="radio-label">
            <input type="radio" value="email" :checked="modelValue === 'email'"
                @change="$emit('update:modelValue', 'email')" />
            <span>邮箱密码</span>
        </label>
        <label class="radio-label">
            <input type="radio" value="account" :checked="modelValue === 'account'"
                @change="$emit('update:modelValue', 'account')" />
            <span>账号密码</span>
        </label>
        <label class="radio-label">
            <input type="radio" value="email-code" :checked="modelValue === 'email-code'"
                @change="$emit('update:modelValue', 'email-code')" />
            <span>验证码登录</span>
        </label>
    </div>
</template>

<style scoped lang="scss">
.login-methods {
    display: flex;
    gap: 12px;
    margin-bottom: 16px;
    padding: 8px;
    background: var(--bg-color);
    border-radius: 8px;
}

.radio-label {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    cursor: pointer;
    color: var(--text-muted);
    padding: 6px 12px;
    border-radius: 6px;
    transition: all 0.2s;
    flex: 1;
    justify-content: center;
    text-align: center;
}

.radio-label:hover {
    background: rgba(255, 255, 255, 0.05);
}

.radio-label input[type="radio"] {
    display: none;
}

.radio-label input[type="radio"]:checked + span {
    color: var(--accent-color);
    font-weight: 500;
}

@media (max-width: 480px) {
    .login-methods {
        gap: 8px;
        margin-bottom: 12px;
        padding: 6px;
    }
    
    .radio-label {
        font-size: 12px;
        padding: 5px 8px;
    }
}
</style>