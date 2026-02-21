<script setup lang="ts">
import type { PropType } from 'vue';

interface PlanItem {
  id: number;
  title: string;
  startDate: string;
  endDate: string | null;
}

const props = defineProps({
  items: {
    type: Array as PropType<PlanItem[]>,
    required: true,
  },
  selectedId: {
    type: Number as PropType<number | null>,
    default: null,
  },
});

const emit = defineEmits<{
  (e: 'update:selectedId', value: number | null): void;
  (e: 'create'): void;
}>();

function handleSelect(id: number): void {
  emit('update:selectedId', id);
}

function handleCreate(): void {
  emit('create');
}
</script>

<template>
  <section class="sidebar">
    <div class="sidebar-header">
      <h2>我的计划</h2>
      <button type="button" class="create" @click="handleCreate">＋ 新建</button>
    </div>
    <ul class="plan-list">
      <li v-for="plan in props.items" :key="plan.id"
        :class="['plan-item', plan.id === props.selectedId ? 'active' : '']" @click="handleSelect(plan.id)">
        <div class="plan-title">{{ plan.title }}</div>
        <div class="plan-dates">
          {{ plan.startDate }}
          <span v-if="plan.endDate">~ {{ plan.endDate }}</span>
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.sidebar {
  border-right: 1px solid var(--border-color);
  padding: 16px;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.create {
  border-radius: 999px;
  border: none;
  padding: 4px 10px;
  background: var(--accent-alt);
  color: var(--accent-on);
  font-size: 13px;
  cursor: pointer;
}

.plan-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.plan-item {
  padding: 8px 10px;
  border-radius: 8px;
  cursor: pointer;
}

.plan-item.active {
  background: rgba(34, 197, 94, 0.15);
}

.plan-title {
  font-size: 14px;
}

.plan-dates {
  font-size: 12px;
  color: var(--text-muted);
}
</style>
