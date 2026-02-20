<script setup lang="ts">
interface Props {
    date: Date;
    statusClass: string;
    dayLabel: string;
}

interface Emits {
    (e: "click", date: Date): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

function handleClick(): void {
    emit("click", props.date);
}
</script>

<template>
    <div :class="statusClass" @click.stop="handleClick">
        <span class="day-label">{{ dayLabel }}</span>
    </div>
</template>

<style scoped lang="scss">
.day {
    width: 100%;
    height: 100%;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.day-label {
    font-size: 13px;
}
</style>