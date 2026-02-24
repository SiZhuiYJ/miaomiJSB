<script setup lang="ts">
import { computed } from "vue";
import { usePlansStore } from "@/stores";

interface Props {
  selectedPlanId: number | null;
  checkinDate: Date;
  mobileMode: "card" | "calendar";
  getDayStatusClass: (date: Date) => string;
}

interface Emits {
  (e: "update:checkinDate", value: Date): void;
}

interface Emits {
  (e: "back"): void;
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
  <section
    v-if="selectedPlan && mobileMode === 'calendar'"
    class="mobile-calendar-page mobile-only"
  >
    <div class="mobile-calendar-header">
      <button type="button" class="mobile-back" @click="emit('back')">
        ← 返回
      </button>
      <div class="mobile-calendar-title">
        {{ selectedPlan.title }}
        <span class="mobile-edit-icon" @click="emit('edit')">✎</span>
      </div>
    </div>

    <el-calendar v-model="localCheckinDate">
      <template #date-cell="{ data }">
        <div
          :class="getDayStatusClass(data.date)"
          @click.stop="handleDateClick(data.date)"
        >
          <span class="day-label">{{ formatDayLabel(data.day) }}</span>
        </div>
      </template>
    </el-calendar>

    <div class="legend">
      <span class="dot success"></span> 正常打卡
      <span class="dot retro"></span> 补签 <span class="dot missed"></span> 错过
    </div>
  </section>
</template>

<style scoped lang="scss">
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
:deep(.is-today) div .day {
  box-shadow: 0px 19px 0 -13px #f87171;
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

.mobile-edit-icon {
  margin-left: 8px;
  font-size: 16px;
  color: var(--text-muted);
}

.mobile-only {
  display: none;
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

@media (max-width: 768px) {
  .mobile-only {
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
