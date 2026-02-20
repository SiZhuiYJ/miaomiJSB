<script setup lang="ts">
interface Props {
  modelValue: 'email' | 'account'
}

interface Emits {
  (e: 'update:modelValue', value: 'email' | 'account'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()
</script>

<template>
  <div class="login-methods">
    <label class="radio-label">
      <input 
        type="radio" 
        value="email" 
        :checked="modelValue === 'email'"
        @change="$emit('update:modelValue', 'email')"
      /> 邮箱登录
    </label>
    <label class="radio-label">
      <input 
        type="radio" 
        value="account" 
        :checked="modelValue === 'account'"
        @change="$emit('update:modelValue', 'account')"
      /> 账号登录
    </label>
  </div>
</template>

<style scoped lang="scss">
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
</style>