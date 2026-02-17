<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { usePlansStore, type PlanSummary } from "../stores/plans";
import { useCheckinsStore } from "../stores/checkins";
import { notifyWarning } from '../utils/notification';
import Topbar from "../components/Topbar.vue";
import PlanSidebar from "../components/PlanSidebar.vue";
import CreatePlanDrawer from "../components/CreatePlanDrawer.vue";
import CheckinDrawer from "../components/CheckinDrawer.vue";
import CheckinDetailDrawer from "../components/CheckinDetailDrawer.vue";

const plansStore = usePlansStore();
const checkinsStore = useCheckinsStore();

const today = new Date();
const currentYear = ref(today.getFullYear());
const currentMonth = ref(today.getMonth() + 1);

const selectedPlanId = ref<number | null>(null);
const showPlanDrawer = ref(false);
const drawerPlan = ref<PlanSummary | null>(null);

const showCheckinDrawer = ref(false);
const showDetailDrawer = ref(false);
const checkinDate = ref<Date>(new Date());

const selectedPlan = computed(() => {
    if (selectedPlanId.value == null) return null;
    return plansStore.items.find(x => x.id === selectedPlanId.value) ?? null;
});

const mobileMode = ref<"card" | "calendar">("card");

function toLocalDateOnlyString(date: Date): string {
    const y = date.getFullYear();
    const m = `${date.getMonth() + 1}`.padStart(2, "0");
    const d = `${date.getDate()}`.padStart(2, "0");
    return `${y}-${m}-${d}`;
}

function parseDateOnly(input: string): Date {
    const parts = input.split("-");
    const y = Number(parts[0] ?? "0") || 0;
    const m = Number(parts[1] ?? "1") || 1;
    const d = Number(parts[2] ?? "1") || 1;
    return new Date(y, m - 1, d);
}

const monthDays = computed(() => {
    const days: Date[] = [];
    const year = currentYear.value;
    const month = currentMonth.value - 1;
    const cursor = new Date(year, month, 1);
    while (cursor.getMonth() === month) {
        days.push(
            new Date(cursor.getFullYear(), cursor.getMonth(), cursor.getDate())
        );
        cursor.setDate(cursor.getDate() + 1);
    }
    return days;
});

const miniCalendarCells = computed(() => {
    const source = monthDays.value;
    if (source.length === 0) return [] as Array<Date | null>;
    const cells: Array<Date | null> = [];
    const firstDay = source[0];
    const weekday = firstDay ? firstDay.getDay() : 1; // 0=Sun
    const offset = (weekday + 6) % 7; // Monday=0
    for (let i = 0; i < offset; i += 1) {
        cells.push(null);
    }
    for (const d of source) {
        cells.push(d);
    }
    return cells;
});

const monthStatsByPlan = computed(() => {
    return (planId: number) => {
        let totalCheckins = 0;
        let activeDays = 0;
        for (const day of monthDays.value) {
            const cls = getPlanDayStatusClass(planId, day);
            if (cls.includes("success") || cls.includes("retro")) {
                totalCheckins += 1;
            }
            if (
                cls.includes("success") ||
                cls.includes("retro") ||
                cls.includes("missed")
            ) {
                activeDays += 1;
            }
        }
        return { totalCheckins, activeDays };
    };
});

const progressPercentByPlan = computed(() => {
    return (planId: number) => {
        const stats = monthStatsByPlan.value(planId);
        if (stats.activeDays === 0) return 0;
        const ratio = (stats.totalCheckins / stats.activeDays) * 100;
        return Math.round(ratio);
    };
});

function isInPlanRangeForPlan(planId: number | null, date: Date): boolean {
    if (planId == null) return false;
    const plan = plansStore.items.find(x => x.id === planId);
    if (!plan) return false;
    const cellDate = parseDateOnly(toLocalDateOnlyString(date));
    const startDate = parseDateOnly(plan.startDate);
    const endDate = plan.endDate ? parseDateOnly(plan.endDate) : null;
    if (cellDate < startDate) return false;
    if (endDate && cellDate > endDate) return false;
    return true;
}

function getPlanStatusCode(
    planId: number | null,
    date: Date
): number | undefined {
    if (planId == null) return undefined;
    const list = checkinsStore.calendarByPlan[planId] ?? [];
    const key = toLocalDateOnlyString(date);
    const found = list.find(item => item.date === key);
    return found?.status;
}

function getPlanDayStatusClass(planId: number | null, date: Date): string {
    const status = getPlanStatusCode(planId, date);
    if (status === 1) return "day success";
    if (status === 2) return "day retro";

    const cellDate = parseDateOnly(toLocalDateOnlyString(date));
    const now = new Date();
    const todayOnly = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate()
    );

    const inPlanRange = isInPlanRangeForPlan(planId, date);
    const isPastDay = cellDate < todayOnly;

    if (inPlanRange && isPastDay && (status === undefined || status === null)) {
        return "day missed";
    }

    return "day";
}

function getMiniDayClassForPlan(planId: number, date: Date | null): string[] {
    if (!date || !isInPlanRangeForPlan(planId, date)) {
        return ["mini-day", "mini-day-empty"];
    }
    const cls = getPlanDayStatusClass(planId, date);
    const classes: string[] = ["mini-day", cls];

    if (cls === "day") {
        const now = new Date();
        const todayOnly = new Date(
            now.getFullYear(),
            now.getMonth(),
            now.getDate()
        );
        const targetOnly = new Date(
            date.getFullYear(),
            date.getMonth(),
            date.getDate()
        );
        if (targetOnly.getTime() === todayOnly.getTime()) {
            classes.push("mini-day-today");
        } else if (targetOnly > todayOnly) {
            classes.push("mini-day-future");
        }
    }

    return classes;
}

onMounted(async () => {
    await plansStore.fetchMyPlans();
    const first = plansStore.items[0];
    if (first) {
        selectedPlanId.value = first.id;
    }
    if (plansStore.items.length > 0) {
        const year = currentYear.value;
        const month = currentMonth.value;
        const tasks = plansStore.items.map(plan =>
            checkinsStore.loadCalendar(plan.id, year, month)
        );
        await Promise.all(tasks);
    }
});

watch(
    () => [selectedPlanId.value, currentYear.value, currentMonth.value],
    async vals => {
        const planId = vals[0];
        if (!planId) return;
        await checkinsStore.loadCalendar(
            planId,
            currentYear.value,
            currentMonth.value
        );
    },
    { immediate: true }
);

function formatDayLabel(value: string): string {
    const parts = value.split("-");
    return parts[2]?.replace(/^0/, "") ?? value;
}

function getDayStatusClass(date: Date): string {
    return getPlanDayStatusClass(selectedPlanId.value, date);
}

function handleDateClick(date: Date): void {
    console.log("打卡抽屉ling……")
    const now = new Date();
    const todayOnly = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate()
    );
    const targetOnly = new Date(
        date.getFullYear(),
        date.getMonth(),
        date.getDate()
    );

    if (targetOnly > todayOnly) {
        notifyWarning("未来的日期不能打卡");
        return;
    }

    console.log(selectedPlanId.value, date)
    const status = getPlanStatusCode(selectedPlanId.value, date);
    checkinDate.value = date;

    if (status === 1 || status === 2) {
        if (!selectedPlanId.value) return;
        showCheckinDrawer.value = false;
        showDetailDrawer.value = true;
        return;
    }

    showDetailDrawer.value = false;
    showCheckinDrawer.value = true;
}

function handleCreatePlan(): void {
    drawerPlan.value = null;
    showPlanDrawer.value = true;
}

function handleEditPlan(): void {
    if (!selectedPlan.value) return;
    drawerPlan.value = selectedPlan.value;
    showPlanDrawer.value = true;
}

function handlePlanCreated(id: number): void {
    selectedPlanId.value = id;
    mobileMode.value = "card";
}

function handlePlanUpdated(): void { }

function handlePlanDeleted(id: number): void {
    if (selectedPlanId.value === id) {
        const first = plansStore.items[0];
        selectedPlanId.value = first ? first.id : null;
        if (!first) {
            mobileMode.value = "card";
        }
    }
}

async function handleCheckinSuccess(): Promise<void> {
    if (!selectedPlanId.value) return;
    await checkinsStore.loadCalendar(
        selectedPlanId.value,
        currentYear.value,
        currentMonth.value
    );
}

function handleMobileCardClick(): void {
    if (!selectedPlan.value) return;
    mobileMode.value = "calendar";
}

function handleMobileCalendarBack(): void {
    mobileMode.value = "card";
}
</script>

<template>
    <div class="dashboard">
        <Topbar />

        <main class="content desktop-only">
            <PlanSidebar :items="plansStore.items" :selected-id="selectedPlanId"
                @update:selected-id="v => (selectedPlanId = v)" @create="handleCreatePlan" />

            <section class="main">
                <div class="calendar-header">
                    <div class="header-row" v-if="selectedPlan">
                        <h2>{{ selectedPlan.title }}</h2>
                        <button class="icon-btn" @click="handleEditPlan">✎</button>
                    </div>
                    <h2 v-else>请选择一个打卡计划</h2>
                </div>

                <el-calendar v-model="checkinDate">
                    <template #date-cell="{ data }">
                        <div :class="getDayStatusClass(data.date)" @click.stop="handleDateClick(data.date)">
                            <span class="day-label">{{
                                formatDayLabel(data.day)
                                }}</span>
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

        <main class="mobile-main mobile-only" v-if="mobileMode === 'card'">
            <section v-for="plan in plansStore.items" :key="plan.id" class="mobile-card" @click="
                selectedPlanId = plan.id;
            handleMobileCardClick();
            ">
                <div class="mobile-card-header">
                    <div>
                        <div class="mobile-title">{{ plan.title }} <span class="mobile-description">{{ plan.description
                                }}</span></div>
                        <div class="mobile-subtitle">
                            {{ plan.startDate }}
                            {{ plan.endDate ? "到 " + plan.endDate : "开始" }}
                            <!--{{ currentYear }} 年 {{ currentMonth }} 月-->
                        </div>
                    </div>
                    <div class="mobile-progress">
                        <el-progress type="circle" :percentage="progressPercentByPlan(plan.id)" :stroke-width="6"
                            :width="52" color="#22c55e" :show-text="false" />
                        <div class="mobile-progress-text">
                            {{ monthStatsByPlan(plan.id).totalCheckins }}
                        </div>
                    </div>
                </div>

                <div class="mobile-card-body">
                    <div class="mini-calendar">
                        <div v-for="(cell, index) in miniCalendarCells" :key="index"
                            :class="getMiniDayClassForPlan(plan.id, cell)">
                            <span v-if="
                                cell && isInPlanRangeForPlan(plan.id, cell)
                            " class="mini-day-label">
                                {{ cell.getDate() }}
                            </span>
                        </div>
                    </div>

                    <div class="mobile-stats">
                        <div class="mobile-stat">
                            <div class="mobile-stat-value">
                                {{ monthStatsByPlan(plan.id).totalCheckins }}
                            </div>
                            <div class="mobile-stat-label">本月次数</div>
                        </div>
                        <div class="mobile-stat">
                            <div class="mobile-stat-value">
                                {{ monthStatsByPlan(plan.id).activeDays }}
                            </div>
                            <div class="mobile-stat-label">本月天数</div>
                        </div>
                    </div>
                </div>
            </section>

            <section v-if="!plansStore.items.length" class="mobile-empty">
                <p class="mobile-empty-text">暂无打卡计划</p>
            </section>
        </main>

        <section v-if="selectedPlan && mobileMode === 'calendar'" class="mobile-calendar-page mobile-only">
            <div class="mobile-calendar-header">
                <button type="button" class="mobile-back" @click="handleMobileCalendarBack">
                    ← 返回
                </button>
                <div class="mobile-calendar-title">
                    {{ selectedPlan.title }}
                    <span class="mobile-edit-icon" @click="handleEditPlan">✎</span>
                </div>
            </div>

            <el-calendar v-model="checkinDate">
                <template #date-cell="{ data }">
                    <div :class="getDayStatusClass(data.date)" @click.stop="handleDateClick(data.date)">
                        <span class="day-label">{{
                            formatDayLabel(data.day)
                            }}</span>
                    </div>
                </template>
            </el-calendar>

            <div class="legend">
                <span class="dot success"></span> 正常打卡
                <span class="dot retro"></span> 补签
                <span class="dot missed"></span> 错过
            </div>
        </section>

        <button type="button" class="mobile-create-fab mobile-only" @click="handleCreatePlan">
            ＋ 新建计划
        </button>

        <CreatePlanDrawer v-model="showPlanDrawer" :edit-plan="drawerPlan" @created="handlePlanCreated"
            @updated="handlePlanUpdated" @deleted="handlePlanDeleted" />

        <CheckinDrawer v-model="showCheckinDrawer" :plan-id="selectedPlanId ?? undefined"
            :date="checkinDate ?? undefined" @success="handleCheckinSuccess" />

        <CheckinDetailDrawer v-model="showDetailDrawer" :plan-id="selectedPlanId ?? undefined"
            :date="checkinDate ?? undefined" />
    </div>
</template>

<style scoped lang="scss">
.dashboard {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    background: var(--bg-color);
    color: var(--text-color);
}

.topbar {
    height: 56px;
    padding: 0 16px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    border-bottom: 1px solid #1f2937;
}

.brand {
    font-weight: 600;
}

.user-info {
    display: flex;
    align-items: center;
    gap: 8px;
}

.email {
    font-size: 14px;
    color: #9ca3af;
}

.logout {
    border: 1px solid #4b5563;
    border-radius: 999px;
    padding: 4px 10px;
    background: transparent;
    color: #e5e7eb;
    cursor: pointer;
}

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

.day {
    width: 100%;
    height: 32px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
}

.day-label {
    font-size: 13px;
}

.day.success {
    background: rgba(34, 197, 94, 0.2);
    color: #166534;
}

.day.retro {
    background: rgba(234, 179, 8, 0.2);
    color: #854d0e;
}

.day.missed {
    background: rgba(248, 113, 113, 0.2);
    color: #b91c1c;
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

.mobile-edit-icon {
    margin-left: 8px;
    font-size: 16px;
    color: var(--text-muted);
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

.drawer-body {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding-bottom: 12px;
}

.field {
    display: flex;
    flex-direction: column;
    gap: 4px;
    font-size: 14px;
}

.field span {
    color: var(--text-muted);
}

input,
textarea {
    border-radius: 8px;
    border: 1px solid var(--border-color);
    padding: 8px 10px;
    font-size: 14px;
    background: var(--bg-elevated);
    color: var(--text-color);
}

.primary {
    margin-top: 4px;
    border-radius: 999px;
    border: none;
    padding: 8px 0;
    background: linear-gradient(to right,
            var(--accent-color),
            var(--accent-alt));
    color: var(--accent-on);
    cursor: pointer;
}

.drawer-date {
    font-size: 14px;
    color: var(--text-muted);
}

.drawer-status {
    font-size: 14px;
}

.drawer-note {
    font-size: 14px;
}

.detail-loading {
    font-size: 14px;
    color: var(--text-muted);
}

.image-list {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

.image-item {
    position: relative;
}

.detail-image {
    width: 80px;
    height: 80px;
    object-fit: cover;
    border-radius: 8px;
    border: 1px solid var(--border-color);
}

.clickable-image {
    cursor: pointer;
}

.image-remove {
    position: absolute;
    top: -6px;
    right: -6px;
    width: 20px;
    height: 20px;
    border-radius: 999px;
    border: none;
    background: rgba(15, 23, 42, 0.9);
    color: #f9fafb;
    font-size: 14px;
    line-height: 20px;
    text-align: center;
    cursor: pointer;
}

.no-images {
    font-size: 13px;
    color: var(--text-muted);
}

.desktop-only {

    /* 手机端 */
    @media (max-width: 768px) {
        display: block;
    }
}

.mobile-only {
    display: none;
}

.mobile-main {
    padding: 12px;
}

.mobile-plan-tabs {
    display: flex;
    gap: 8px;
    margin-bottom: 10px;
    overflow-x: auto;
}

.mobile-plan-tab {
    flex-shrink: 0;
    border-radius: 999px;
    border: 1px solid var(--border-color);
    padding: 4px 10px;
    background: var(--surface-soft);
    color: var(--text-color);
    font-size: 13px;
    cursor: pointer;
}

.mobile-plan-tab.active {
    background: linear-gradient(to right,
            var(--accent-color),
            var(--accent-alt));
    color: var(--accent-on);
    border-color: transparent;
}

.mobile-card {
    border-radius: 16px;
    background: var(--bg-elevated);
    box-shadow: 0 16px 30px rgba(15, 23, 42, 0.5);
    padding: 16px;
    margin-bottom: 15px;
}

.mobile-card-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
}

.mobile-title {
    font-size: 16px;
    font-weight: 600;

    .mobile-description {
        font-size: 12px;
        color: var(--text-muted);
    }
}

.mobile-subtitle {
    margin-top: 4px;
    font-size: 12px;
    color: var(--text-muted);
}

.mobile-progress {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
}

.mobile-progress-text {
    position: absolute;
    font-size: 14px;
    font-weight: 600;
}

.mobile-card-body {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
}

.mini-calendar {
    display: grid;
    grid-template-columns: repeat(7, 18px);
    grid-auto-rows: 18px;
    gap: 4px;
}

.mini-day {
    position: relative;
    border-radius: 4px;
    background: transparent;
    width: 18px;
    height: 18px;
    cursor: default;
}

.mini-day.day.success {
    background: #22c55e;
    cursor: pointer;
}

.mini-day.day.retro {
    background: #eab308;
    cursor: pointer;
}

.mini-day.day.missed {
    background: #f97373;
    cursor: pointer;
}

.mini-day-empty {
    background: transparent;
}

.mini-day-today {
    border: 1px solid #22c55e;
}

.mini-day-future {
    border: 1px solid #38bdf8;
}

.mini-day-label {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 10px;
}

.mini-day.day.success .mini-day-label,
.mini-day.day.retro .mini-day-label,
.mini-day.day.missed .mini-day-label {
    color: #ffffff;
}

.mobile-stats {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.mobile-stat-value {
    font-size: 18px;
    font-weight: 600;
}

.mobile-stat-label {
    font-size: 12px;
    color: var(--text-muted);
}

.mobile-actions {
    margin-top: 12px;
    display: flex;
    justify-content: flex-end;
}

.mobile-create {
    border-radius: 999px;
    border: none;
    padding: 6px 14px;
    background: linear-gradient(to right,
            var(--accent-color),
            var(--accent-alt));
    color: var(--accent-on);
    font-size: 13px;
    cursor: pointer;
}

.mobile-empty {
    border-radius: 16px;
    background: var(--bg-elevated);
    box-shadow: 0 16px 30px rgba(15, 23, 42, 0.5);
    padding: 20px 16px;
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.mobile-empty-text {
    font-size: 14px;
}

.mobile-create-fab {
    position: fixed;
    right: 16px;
    bottom: 20px;
    border-radius: 999px;
    border: none;
    padding: 10px 16px;
    background: linear-gradient(to right,
            var(--accent-color),
            var(--accent-alt));
    color: var(--accent-on);
    font-size: 14px;
    cursor: pointer;
    box-shadow: 0 8px 20px rgba(15, 23, 42, 0.6);
    z-index: 20;
}

.mobile-calendar-page {
    padding: 12px;
}

.mobile-calendar-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 8px;
}

.mobile-back {
    border-radius: 999px;
    border: none;
    padding: 4px 10px;
    background: var(--surface-soft);
    color: var(--text-color);
    font-size: 13px;
    cursor: pointer;
}

.mobile-calendar-title {
    font-size: 15px;
    font-weight: 600;
}

@media (max-width: 768px) {
    .content {
        display: none;
    }

    .desktop-only {
        display: none;
    }

    .mobile-only {
        display: block;
    }
}
</style>
