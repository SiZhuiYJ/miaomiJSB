<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import { usePlansStore, useCheckinsStore } from "@/stores";
import type { PlanSummary } from "../types";
import { notifyWarning } from "@/utils/notification";
import Topbar from "@/components/Topbar.vue";
import CreatePlanDrawer from "@/components/CreatePlanDrawer.vue";
import CheckinDrawer from "@/components/CheckinDrawer.vue";
import CheckinDetailDrawer from "@/components/CheckinDetailDrawer.vue";
import DesktopMainView from "./DesktopMainView.vue";
import MobilePlanCards from "./MobilePlanCards.vue";
import MobileCalendarPage from "./MobileCalendarPage.vue";
import { usePlanCalendar } from "../composables/usePlanCalendar";

const plansStore = usePlansStore();
const checkinsStore = useCheckinsStore();

// 使用组合式函数管理日历逻辑
const {
  currentYear,
  currentMonth,
  selectedPlanId,
  checkinDate,
  selectedPlan,
  monthDays,
  miniCalendarCells,
  monthStatsByPlan,
  progressPercentByPlan,
  isInPlanRangeForPlan,
  getPlanStatusCode,
  getPlanDayStatusClass,
  getMiniDayClassForPlan,
  formatDayLabel,
  getDayStatusClass,
} = usePlanCalendar();

const showPlanDrawer = ref(false);
const drawerPlan = ref<PlanSummary | null>(null);
const showCheckinDrawer = ref(false);
const showDetailDrawer = ref(false);
const mobileMode = ref<"card" | "calendar">("card");

onMounted(async () => {
  await plansStore.fetchMyPlans();
  const first = plansStore.items[0];
  if (first) {
    selectedPlanId.value = first.id;
  }
  if (plansStore.items.length > 0) {
    const year = currentYear.value;
    const month = currentMonth.value;
    const tasks = plansStore.items.map((plan) =>
      checkinsStore.loadCalendar(plan.id, year, month),
    );
    await Promise.all(tasks);
  }
});

watch(
  () => [selectedPlanId.value, currentYear.value, currentMonth.value],
  async (vals) => {
    const planId = vals[0];
    if (!planId) return;
    await checkinsStore.loadCalendar(
      planId,
      currentYear.value,
      currentMonth.value,
    );
  },
  { immediate: true },
);

function handleDateClick(date: Date): void {
  console.log("打卡抽屉ling……");
  const now = new Date();
  const todayOnly = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const targetOnly = new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  );

  if (targetOnly > todayOnly) {
    notifyWarning("未来的日期不能打卡");
    return;
  }

  console.log("计划ID", selectedPlanId.value);
  console.log("时间", date);
  const status = getPlanStatusCode(selectedPlanId.value, date);
  checkinDate.value = date;

  if (status === 1 || status === 2) {
    if (!selectedPlanId.value) return;
    showCheckinDrawer.value = false;
    showDetailDrawer.value = true;
    console.log("打开打卡详情");
    return;
  }
  console.log("打开打卡");
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

function handlePlanUpdated(): void {}

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
    currentMonth.value,
  );
}

function handleMobileCardSelect(planId: number): void {
  selectedPlanId.value = planId;
  handleMobileCardClick();
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

    <DesktopMainView
      :selected-plan-id="selectedPlanId"
      :checkin-date="checkinDate"
      :get-day-status-class="getDayStatusClass"
      @update:selected-plan-id="(v) => (selectedPlanId = v)"
      @update:checkin-date="(v) => (checkinDate = v)"
      @create="handleCreatePlan"
      @edit="handleEditPlan"
      @date-click="handleDateClick"
    />

    <MobilePlanCards
      :mobile-mode="mobileMode"
      :progress-percent-by-plan="progressPercentByPlan"
      :month-stats-by-plan="monthStatsByPlan"
      :mini-calendar-cells="miniCalendarCells"
      :get-mini-day-class-for-plan="getMiniDayClassForPlan"
      :is-in-plan-range-for-plan="isInPlanRangeForPlan"
      @select-plan="handleMobileCardSelect"
      @create="handleCreatePlan"
    />

    <MobileCalendarPage
      :selected-plan-id="selectedPlanId"
      :checkin-date="checkinDate"
      :mobile-mode="mobileMode"
      :get-day-status-class="getDayStatusClass"
      @update:checkin-date="(v) => (checkinDate = v)"
      @back="handleMobileCalendarBack"
      @edit="handleEditPlan"
      @date-click="handleDateClick"
    />

    <button
      type="button"
      class="mobile-create-fab mobile-only"
      @click="handleCreatePlan"
    >
      ＋ 新建计划
    </button>

    <CreatePlanDrawer
      v-model="showPlanDrawer"
      :edit-plan="drawerPlan"
      @created="handlePlanCreated"
      @updated="handlePlanUpdated"
      @deleted="handlePlanDeleted"
    />

    <CheckinDrawer
      v-model="showCheckinDrawer"
      :plan-id="selectedPlanId ?? undefined"
      :date="checkinDate ?? undefined"
      @success="handleCheckinSuccess"
    />

    <CheckinDetailDrawer
      v-model="showDetailDrawer"
      :plan-id="selectedPlanId ?? undefined"
      :date="checkinDate ?? undefined"
    />
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

.mobile-create-fab {
  position: fixed;
  right: 16px;
  bottom: 20px;
  border-radius: 999px;
  border: none;
  padding: 10px 16px;
  background: linear-gradient(to right, var(--accent-color), var(--accent-alt));
  color: var(--accent-on);
  font-size: 14px;
  cursor: pointer;
  box-shadow: 0 8px 20px rgba(15, 23, 42, 0.6);
  z-index: 20;
}

@media (max-width: 768px) {
  .desktop-only {
    display: none;
  }

  .mobile-only {
    display: block;
  }
}
</style>
