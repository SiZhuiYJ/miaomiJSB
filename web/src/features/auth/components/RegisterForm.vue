<script setup lang="ts">
import { ref } from 'vue'
import EmailInput from './EmailInput.vue'
import AccountInput from './AccountInput.vue'
import PasswordInput from './PasswordInput.vue'
import VerificationCodeInput from './VerificationCodeInput.vue'

interface RegisterFormProps {
  email: string
  userAccount: string
  password: string
  confirmPassword: string
  nickName: string
  code: string
  sendingCode: boolean
  countdown: number
  userAccountError: string
}

interface Emits {
  (e: 'update:email', value: string): void
  (e: 'update:userAccount', value: string): void
  (e: 'update:password', value: string): void
  (e: 'update:confirmPassword', value: string): void
  (e: 'update:nickName', value: string): void
  (e: 'update:code', value: string): void
  (e: 'update:userAccountError', value: string): void
  (e: 'sendCode'): void
  (e: 'generateRandomAccount'): void
  (e: 'validateAccount'): void
  (e: 'submit'): void
}

const props = defineProps<RegisterFormProps>()
const emit = defineEmits<Emits>()

const loading = ref(false)

function handleSubmit() {
  loading.value = true
  emit('submit')
  // 在父组件处理完后会重置loading状态
}

function handleGenerateRandomAccount() {
  emit('generateRandomAccount')
}

function handleValidateAccount() {
  emit('validateAccount')
}

function handleAccountInput() {
  emit('update:userAccountError', '')
}

defineExpose({
  setLoading: (value: boolean) => {
    loading.value = value
  }
})
</script>

<template>
  <form class="form" @submit.prevent="handleSubmit">
    <EmailInput 
      :model-value="email"
      @update:model-value="$emit('update:email', $event)"
    />
    
    <VerificationCodeInput
      :model-value="code"
      :sending="sendingCode"
      :countdown="countdown"
      @update:model-value="$emit('update:code', $event)"
      @send-code="$emit('sendCode')"
    />
    
    <AccountInput 
      :model-value="userAccount"
      :required="true"
      :error="userAccountError"
      @update:model-value="$emit('update:userAccount', $event)"
      @blur="handleValidateAccount"
      @input="handleAccountInput"
    >
      <template #suffix>
        <button
          type="button"
          class="btn-sm"
          @click="handleGenerateRandomAccount"
        >
          随机
        </button>
      </template>
    </AccountInput>
    
    <PasswordInput 
      :model-value="password"
      :required="true"
      label="密码"
      @update:model-value="$emit('update:password', $event)"
    />
    
    <PasswordInput 
      :model-value="confirmPassword"
      :required="true"
      label="确认密码"
      placeholder="请确认密码"
      @update:model-value="$emit('update:confirmPassword', $event)"
    />
    
    <label class="field">
      <span>昵称（可选）</span>
      <input 
        :value="nickName" 
        type="text" 
        placeholder="请输入昵称" 
        @input="$emit('update:nickName', ($event.target as HTMLInputElement).value)"
      />
    </label>
    
    <button class="submit" type="submit" :disabled="loading">
      注册并登录
    </button>
  </form>
</template>

<style scoped lang="scss">
.form {
  display: flex;
  flex-direction: column;
  gap: 16px;
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

input:focus {
  border-color: var(--accent-color);
  outline: none;
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