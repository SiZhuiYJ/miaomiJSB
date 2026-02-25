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

<style scoped lang="scss"></style>