<script setup lang="ts">
import { computed } from "vue";
import PlanSidebar from "@/components/PlanSidebar.vue";
import { usePlansStore } from "@/stores";
import type { PlanSummary } from "../types";

interface Props {
    selectedPlanId: number | null;
    checkinDate: Date;
    getDayStatusClass: (date: Date) => string;
}

interface Emits {
    (e: "update:checkinDate", value: Date): void;
    (e: "update:selectedPlanId", value: number | null): void;
    (e: "create"): void;
    (e: "edit"): void;
    (e: "dateClick", date: Date): void;
}

interface Emits {
    (e: "update:selectedPlanId", value: number | null): void;
    (e: "create"): void;
    (e: "edit"): void;
    (e: "dateClick", date: Date): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const localCheckinDate = computed({
    get: () => props.checkinDate,
    set: (value) => emit("update:checkinDate", value),
});

const plansStore = usePlansStore();

const selectedPlan = computed(() => {
    if (props.selectedPlanId == null) return null;
    return plansStore.items.find((x) => x.id === props.selectedPlanId) ?? null;
});

function handleDateClick(date: Date): void {
    emit("dateClick", date);
}

function formatDayLabel(value: string): string {
    const parts = value.split("-");
    return parts[2]?.replace(/^0/, "") ?? value;
}

function getDayStatusClass(date: Date): string {
    return props.getDayStatusClass(date);
}
</script>

<template>
    <main class="content desktop-only">
        <PlanSidebar :items="plansStore.items" :selected-id="selectedPlanId"
            @update:selected-id="(v) => emit('update:selectedPlanId', v)" @create="emit('create')" />

        <section class="main">
            <div class="calendar-header">
                <div class="header-row" v-if="selectedPlan">
                    <h2>{{ selectedPlan.title }}</h2>
                    <button class="icon-btn" @click="emit('edit')">✎</button>
                </div>
                <h2 v-else>请选择一个打卡计划</h2>
            </div>

            <el-calendar v-model="localCheckinDate">
                <template #date-cell="{ data }">
                    <div :class="getDayStatusClass(data.date)" @click.stop="handleDateClick(data.date)">
                        <span class="day-label">{{ formatDayLabel(data.day) }}</span>
                    </div>
                </template>
            </el-calendar>

            <div class="legend">
                <span class="dot success"></span> 正常打卡
                <span class="dot retro"></span> 补签
                <span class="dot missed"></span> 错过
            </div>
        </section>
    </main>
</template>

<style scoped lang="scss">
.content {
    flex: 1;
    display: grid;
    grid-template-columns: 260px minmax(0, 1fr);
    max-width: 1200px;
    margin: 0 auto;
}

.main {
    padding: 16px;
}

.calendar-header {
    margin-bottom: 8px;
}

:deep(.el-calendar-day) {
    height: 46px;
}

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

.legend {
    margin-top: 8px;
    display: flex;
    align-items: center;
    gap: 12px;
    font-size: 12px;
    color: var(--text-muted);
}

.header-row {
    display: flex;
    align-items: center;
    gap: 12px;
}

.icon-btn {
    border: none;
    background: transparent;
    color: var(--text-muted);
    font-size: 18px;
    cursor: pointer;
    padding: 4px;
    border-radius: 4px;

    &:hover {
        background: var(--bg-elevated);
        color: var(--text-color);
    }
}

.dot {
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 999px;
    margin-right: 4px;
}

.dot.success {
    background: #22c55e;
}

.dot.retro {
    background: #eab308;
}

.dot.missed {
    background: #f87171;
}

.desktop-only {

    /* 手机端 */
    @media (max-width: 768px) {
        display: block;
    }
}

:deep(.prev) div .day.success,
:deep(.next) div .day.success {
    background: rgba(202, 255, 222, 0.2);
    color: #89ffb6;
}

.day.success {
    background: rgba(34, 197, 94, 0.2);
    color: #22c55e;
}

:deep(.prev) div .day.retro,
:deep(.next) div .day.retro {
    background: rgba(255, 246, 217, 0.2);
    color: #ffc685;
}

.day.retro {
    background: rgba(234, 179, 8, 0.2);
    color: #eab308;
}

:deep(.prev) div .day.missed,
:deep(.next) div .day.missed {
    background: rgba(255, 211, 211, 0.2);
    color: #ff7f7f;
}

.day.missed {
    background: rgba(248, 113, 113, 0.2);
    color: #f87171;
}
</style>