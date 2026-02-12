<script setup lang="ts">
import { computed, ref, watch, onMounted, onUnmounted } from 'vue';
import { usePlansStore } from '../../stores/plans';
import { useCheckinsStore } from '../../stores/checkins';
import { useNavbar } from '../../utils/useNavbar';
import ProgressRing from '../../components/ProgressRing.vue';

const props = defineProps<{
  isActive: boolean;
}>();

const plansStore = usePlansStore();
const checkinsStore = useCheckinsStore();
const { paddingTop, height, paddingLeft } = useNavbar();

const today = new Date();
const currentYear = ref(today.getFullYear());
const currentMonth = ref(today.getMonth() + 1);

watch(() => props.isActive, async (newVal) => {
  if (newVal) {
    await loadData();
  }
}, { immediate: true });

function handlePlanUpdated() {
  if (props.isActive) {
    loadData();
  }
}

onMounted(() => {
  uni.$on('plan-updated', handlePlanUpdated);
});

onUnmounted(() => {
  uni.$off('plan-updated', handlePlanUpdated);
});

async function loadData() {
  await plansStore.fetchMyPlans();
  if (plansStore.items.length > 0) {
    const year = currentYear.value;
    const month = currentMonth.value;
    const tasks = plansStore.items.map((plan) => {
      // Load current month calendar for display
      const t1 = checkinsStore.loadCalendar(plan.id, year, month);
      // Load total stats
      const t2 = checkinsStore.fetchPlanStats(plan);
      return Promise.all([t1, t2]);
    });
    await Promise.all(tasks);
  }
}

function toLocalDateOnlyString(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function parseDateOnly(input: string): Date {
  const parts = input.split('-');
  const y = Number(parts[0] ?? '0') || 0;
  const m = Number(parts[1] ?? '1') || 1;
  const d = Number(parts[2] ?? '1') || 1;
  return new Date(y, m - 1, d);
}

const monthDays = computed(() => {
  const days: Date[] = [];
  const year = currentYear.value;
  const month = currentMonth.value - 1;
  const cursor = new Date(year, month, 1);
  while (cursor.getMonth() === month) {
    days.push(new Date(cursor.getFullYear(), cursor.getMonth(), cursor.getDate()));
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
  const offset = weekday === 0 ? 6 : weekday - 1; // Mon=0, Sun=6 for grid starting Monday
  for (let i = 0; i < offset; i += 1) {
    cells.push(null);
  }
  for (const d of source) {
    cells.push(d);
  }
  return cells;
});

function isInPlanRangeForPlan(planId: number | null, date: Date): boolean {
  if (planId == null) return false;
  const plan = plansStore.items.find((x) => x.id === planId);
  if (!plan) return false;
  const cellDate = parseDateOnly(toLocalDateOnlyString(date));
  const startDate = parseDateOnly(plan.startDate);
  const endDate = plan.endDate ? parseDateOnly(plan.endDate) : null;
  if (cellDate < startDate) return false;
  if (endDate && cellDate > endDate) return false;
  return true;
}

function getPlanStatusCode(planId: number | null, date: Date): number | undefined {
  if (planId == null) return undefined;
  const list = checkinsStore.calendarByPlan[planId] ?? [];
  const key = toLocalDateOnlyString(date);
  const found = list.find((item) => item.date === key);
  return found?.status;
}

function getPlanDayStatusClass(planId: number | null, date: Date): string {
  const status = getPlanStatusCode(planId, date);
  if (status === 1) return 'day success';
  if (status === 2) return 'day retro';

  const cellDate = parseDateOnly(toLocalDateOnlyString(date));
  const now = new Date();
  const todayOnly = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  const inPlanRange = isInPlanRangeForPlan(planId, date);
  const isPastDay = cellDate < todayOnly;

  if (inPlanRange && isPastDay && (status === undefined || status === null)) {
    return 'day missed';
  }

  return 'day';
}

function getMiniDayClassForPlan(planId: number, date: Date | null): string[] {
  if (!date || !isInPlanRangeForPlan(planId, date)) {
    return ['mini-day', 'mini-day-empty'];
  }
  const cls = getPlanDayStatusClass(planId, date);
  const classes: string[] = ['mini-day', cls];

  const now = new Date();
  const todayOnly = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const targetOnly = new Date(date.getFullYear(), date.getMonth(), date.getDate());

  if (targetOnly.getTime() === todayOnly.getTime()) {
    classes.push('mini-day-today');
  }

  return classes;
}

const monthStatsByPlan = computed((): ((planId: number) => { totalCheckins: number; activeDays: number; percentage: number }) => {
  return (planId: number) => {
    let totalCheckins = 0;
    let activeDays = 0;

    for (const day of monthDays.value) {
      if (isInPlanRangeForPlan(planId, day)) {
        activeDays += 1;
        const cls = getPlanDayStatusClass(planId, day);
        if (cls.includes('success') || cls.includes('retro')) {
          totalCheckins += 1;
        }
      }
    }

    const percentage = activeDays > 0 ? (totalCheckins / activeDays) * 100 : 0;
    return { totalCheckins, activeDays, percentage };
  };
});

function getTotalStatsDisplay(planId: number) {
  const stats = checkinsStore.stats[planId];
  if (!stats || stats.loading) return '∞';

  const { totalCheckins, totalDays } = stats;
  if (totalDays === null) {
    return `${totalCheckins}/∞`;
  }
  return `${totalCheckins}/${totalDays}`;
}

function handlePlanClick(planId: number) {
  uni.navigateTo({
    url: `/pages/plan/detail?id=${planId}`
  });
}

function handleCreatePlan() {
  uni.navigateTo({
    url: '/pages/plan/create'
  });
}
</script>

<template>
  <view class="tab-content">
    <NotificationSystem />

    <view class="header" :style="{ paddingTop: paddingTop + 'px', paddingLeft: paddingLeft + 'px' }">
      <text class="page-title" :style="{ lineHeight: height + 'px' }">今日打卡</text>
    </view>

    <view class="plan-list">
      <view v-for="plan in plansStore.items" :key="plan.id" class="plan-card" @click="handlePlanClick(plan.id)">
        <view class="card-header-row">
          <view class="card-title-group">
            <text class="card-title">{{ plan.title }}</text>
            <text class="card-desc">{{ plan.description || '无描述' }}</text>
          </view>
        </view>
        <text class="card-date-range">{{ plan.startDate }} 到 {{ plan.endDate || '永久' }}</text>

        <view class="card-body">
          <view class="calendar-wrapper">
            <view class="mini-calendar">
              <view v-for="(cell, index) in miniCalendarCells" :key="index"
                :class="getMiniDayClassForPlan(plan.id, cell)">
                <text v-if="cell && isInPlanRangeForPlan(plan.id, cell)" class="mini-day-text">
                  {{ cell.getDate() }}
                </text>
              </view>
            </view>
          </view>

          <view class="stats-wrapper">
            <view class="progress-ring">
              <ProgressRing :progress="monthStatsByPlan(plan.id).percentage / 100" :size="50" :radius="65"
                color="#8EA88E" :strokeWidth="12">
                <text class="ring-text">{{ monthStatsByPlan(plan.id).totalCheckins }}</text>
              </ProgressRing>
            </view>

            <view class="stat-item">
              <text class="stat-val">{{ monthStatsByPlan(plan.id).percentage.toFixed(2) }}%</text>
              <text class="stat-lbl">本月进度</text>
            </view>

            <view class="stat-item">
              <text class="stat-val">{{ monthStatsByPlan(plan.id).totalCheckins }}/{{
                monthStatsByPlan(plan.id).activeDays }}</text>
              <text class="stat-lbl">本月天数</text>
            </view>
            <view class="stat-item">
              <text class="stat-val">{{ getTotalStatsDisplay(plan.id) }}</text>
              <text class="stat-lbl">总进度</text>
            </view>
          </view>
        </view>
      </view>

      <view class="create-plan-card" @click="handleCreatePlan">
        <text class="create-icon">+</text>
      </view>

      <view v-if="plansStore.items.length === 0" class="empty-state">
        <text>暂无计划，点击上方按钮创建</text>
      </view>
    </view>
  </view>
</template>

<style scoped lang="scss">
.tab-content {
  padding: var(--uni-container-padding);
  box-sizing: border-box;
  padding-bottom: 10px;
  background-color: var(--bg-color);
  min-height: 100vh;
}

.header {
  margin-bottom: 20px;
}

.plan-list {
  margin-bottom: 80px;
}

.page-title {
  font-size: 24px;
  font-weight: 800;
  color: var(--text-color);
}

/* Plan Card */
.plan-card {
  background-color: var(--bg-elevated);
  border-radius: var(--uni-card-border-radius);
  padding: var(--uni-card-padding);
  margin-bottom: var(--uni-card-margin-bottom);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
}

.card-header-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-bottom: 4px;
}

.card-title-group {
  display: flex;
  align-items: baseline;
  gap: 8px;
  overflow: hidden;
}

.card-title {
  font-size: 18px;
  font-weight: bold;
  color: var(--text-color);
  white-space: nowrap;
  flex-shrink: 0;
}

.card-desc {
  font-size: 13px;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  min-width: 0;
}

.card-date-range {
  display: block;
  font-size: 12px;
  color: var(--text-muted);
  margin-bottom: 16px;
}

.card-body {
  display: flex;
  gap: 16px;
  align-items: center;
}

.calendar-wrapper {
  flex: 1;
}

.mini-calendar {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 6px;
}

.mini-day {
  aspect-ratio: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  background-color: transparent;
  border: 1px solid transparent;
}

.mini-day-text {
  font-size: 12px;
  color: var(--text-color);
  font-weight: 500;
}

/* Day Status Colors */
.day.success {
  border: 1px solid #10b981;
  background-color: rgba(16, 185, 129, 0.1);
  color: #10b981;
}

.day.success .mini-day-text {
  color: #10b981;
}

.day.retro {
  border: 1px solid #eab308;
  background-color: rgba(234, 179, 8, 0.2);
  color: #a16207;
}

.day.retro .mini-day-text {
  color: #a16207;
}

.day.missed {
  border: 1px solid #ef4444;
  background-color: rgba(239, 68, 68, 0.1);
  color: #b91c1c;
}

.day.missed .mini-day-text {
  color: #b91c1c;
}

.mini-day-today {
  border: 1px solid var(--accent-color) !important;
  font-weight: bold;
}

/* Stats Wrapper */
.stats-wrapper {
  width: 80px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.ring-text {
  font-size: 16px;
  font-weight: bold;
  color: var(--text-color);
}

.stat-item {
  text-align: center;
  line-height: 1.2;
}

.stat-val {
  display: block;
  font-size: 16px;
  font-weight: bold;
  color: var(--text-color);
}

.stat-lbl {
  font-size: 11px;
  color: var(--text-muted);
}

.create-plan-card {
  background-color: var(--bg-elevated);
  border-radius: var(--uni-card-border-radius);
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--uni-card-margin-bottom);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  transition: all 0.2s;
}

.create-plan-card:active {
  transform: scale(0.98);
  opacity: 0.8;
}

.create-icon {
  font-size: 32px;
  color: var(--text-muted);
  font-weight: 300;
  line-height: 1;
}

.empty-state {
  text-align: center;
  color: var(--text-muted);
  margin-top: 20px;
}
</style>
