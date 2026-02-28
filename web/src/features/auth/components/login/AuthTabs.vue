<script setup lang="ts">
interface Props {
    modelValue: 'login' | 'register'
}

interface Emits {
    (e: 'update:modelValue', value: 'login' | 'register'): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

function switchMode(mode: 'login' | 'register') {
    emit('update:modelValue', mode)
}
</script>

<template>
    <div class="tabs">
        <button type="button" :class="['tab', modelValue === 'login' ? 'active' : '']" @click="switchMode('login')">
            登录
        </button>
        <button type="button" :class="['tab', modelValue === 'register' ? 'active' : '']"
            @click="switchMode('register')">
            注册
        </button>
    </div>
</template>

<style scoped lang="scss">
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

@media (max-width: 480px) {
    .tab {
        padding: 10px 0;
        font-size: 15px;
    }
}
</style>