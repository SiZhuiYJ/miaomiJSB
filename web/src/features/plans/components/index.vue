<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import { usePlansStore, useCheckinsStore } from "@/stores";
import type { PlanSummary } from "../types";
import { notifyWarning } from "@/utils/notification";
import CreatePlanDrawer from "@/features/plans/components/CreatePlanDrawer.vue";
import CheckinDetailDrawer from "@/features/checkin/components/CheckinDetailDrawer.vue";
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
  miniCalendarCells,
  monthStatsByPlan,
  progressPercentByPlan,
  isInPlanRangeForPlan,
  getMiniDayClassForPlan,
  getDayStatusClass,
} = usePlanCalendar();

const showPlanDrawer = ref(false);
const drawerPlan = ref<PlanSummary | null>(null);
const showDetailDrawer = ref(false);
const mobileMode = ref<"card" | "calendar">("card");

onMounted(async () => {
  await plansStore.fetchMyPlans();
  const first = plansStore.items[0];
  if (first) selectedPlanId.value = first.id;

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
  const now = new Date();
  const todayOnly = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const targetOnly = new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
  );
  if (selectedPlan.value) {
    const plan = selectedPlan.value;
    const startDate = new Date(plan.startDate);
    const startOnly = new Date(
      startDate.getFullYear(),
      startDate.getMonth(),
      startDate.getDate(),
    );
    if (targetOnly < startOnly) {
      notifyWarning("所选日期计划未开始");
      return;
    }
    if (plan.endDate) {
      const endDate = new Date(plan.endDate);
      const endOnly = new Date(
        endDate.getFullYear(),
        endDate.getMonth(),
        endDate.getDate(),
      );
      if (targetOnly > endOnly) {
        notifyWarning("所选日期计划已结束");
        return;
      }
    }
  }

  if (targetOnly > todayOnly) {
    notifyWarning("未来日期不能打卡");
    return;
  }

  checkinDate.value = date;
  showDetailDrawer.value = true;
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

function getTimeSlotMode(): "default" | "timeSlot" {
  // 根据选中的计划是否有时段来决定模式
  const plan = selectedPlan.value;
  return plan?.timeSlots && plan.timeSlots.length > 0 ? "timeSlot" : "default";
}

function handleOpenCheckinFromDetail(): void {
  // 从详情页打开打卡页面
  showDetailDrawer.value = true;
}
</script>

<template>
  <div class="dashboard">
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

    <CheckinDetailDrawer
      v-if="selectedPlanId"
      v-model="showDetailDrawer"
      :plan-id="selectedPlanId"
      :date="checkinDate"
      :time-slots="selectedPlan?.timeSlots"
      :mode="getTimeSlotMode()"
      @open-checkin="handleOpenCheckinFromDetail"
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
  background: var(--accent-alt);
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
