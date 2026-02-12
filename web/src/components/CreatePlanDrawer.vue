<script setup lang="ts">
import { computed, ref, watch, type PropType } from 'vue';
import { ElMessageBox } from 'element-plus';
import { usePlansStore, type PlanSummary } from '../stores/plans';
import { notifySuccess, notifyWarning } from '../utils/notification';

const props = defineProps({
  modelValue: {
    type: Boolean,
    required: true,
  },
  editPlan: {
    type: Object as PropType<PlanSummary | null>,
    default: null,
  },
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'created', id: number): void;
  (e: 'updated', id: number): void;
  (e: 'deleted', id: number): void;
}>();

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const plansStore = usePlansStore();

const title = ref('');
const description = ref('');
const startDate = ref<string | null>(null);
const endDate = ref<string | null>(null);
const isActive = ref(true);

function resetForm(): void {
  title.value = '';
  description.value = '';
  startDate.value = null;
  endDate.value = null;
  isActive.value = true;
}

watch(
  () => props.editPlan,
  (newVal) => {
    if (newVal) {
      title.value = newVal.title;
      description.value = newVal.description ?? '';
      startDate.value = newVal.startDate;
      endDate.value = newVal.endDate;
      isActive.value = newVal.isActive;
    } else {
      resetForm();
    }
  },
  { immediate: true }
);

async function handleSubmit(): Promise<void> {
  if (!title.value.trim()) {
    notifyWarning('请输入计划标题');
    return;
  }

  if (startDate.value && endDate.value && new Date(endDate.value) < new Date(startDate.value)) {
    notifyWarning('结束日期不能早于开始日期');
    return;
  }

  if (props.editPlan) {
    await plansStore.updatePlan({
      id: props.editPlan.id,
      title: title.value.trim(),
      description: description.value || undefined,
      startDate: startDate.value ?? null,
      endDate: endDate.value ?? null,
      isActive: isActive.value,
    });
    notifySuccess('修改计划成功');
    emit('updated', props.editPlan.id);
  } else {
    const created = await plansStore.createPlan({
      title: title.value.trim(),
      description: description.value || undefined,
      startDate: startDate.value ?? null,
      endDate: endDate.value ?? null,
    });
    notifySuccess('创建计划成功');
    emit('created', created.id);
  }

  visible.value = false;
}

async function handleDelete(): Promise<void> {
  if (!props.editPlan) return;
  try {
    await ElMessageBox.confirm('确定要删除这个计划吗？此操作无法撤销。', '删除确认', {
      confirmButtonText: '删除',
      cancelButtonText: '取消',
      type: 'warning',
    });
    await plansStore.deletePlan(props.editPlan.id);
    notifySuccess('删除计划成功');
    emit('deleted', props.editPlan.id);
    visible.value = false;
  } catch {
    // Cancelled
  }
}

function handleClosed(): void {
  resetForm();
}
</script>

<template>
  <el-drawer
    v-model="visible"
    direction="btt"
    size="auto"
    :title="props.editPlan ? '修改打卡计划' : '创建打卡计划'"
    @closed="handleClosed"
  >
    <div class="drawer-body">
      <label class="field">
        <span>计划标题</span>
        <input v-model="title" type="text" />
      </label>
      <label class="field">
        <span>计划描述</span>
        <textarea v-model="description" rows="3" />
      </label>
      <label class="field">
        <span>开始日期（可选，默认今天）</span>
        <el-date-picker
          v-model="startDate"
          type="date"
          placeholder="选择开始日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
        />
      </label>
      <label class="field">
        <span>结束日期（可选）</span>
        <el-date-picker
          v-model="endDate"
          type="date"
          placeholder="选择结束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
        />
      </label>
      <label v-if="props.editPlan" class="field-row">
        <span>是否启用</span>
        <el-switch v-model="isActive" />
      </label>

      <div class="actions">
        <button type="button" class="primary" @click="handleSubmit">
          {{ props.editPlan ? '保存修改' : '创建' }}
        </button>
        <button v-if="props.editPlan" type="button" class="danger" @click="handleDelete">
          删除计划
        </button>
      </div>
    </div>
  </el-drawer>
</template>

<style scoped>
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

.field-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
}

.field span, .field-row span {
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

.actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 4px;
}

.primary {
  border-radius: 999px;
  border: none;
  padding: 8px 0;
  background: linear-gradient(to right, var(--accent-color), var(--accent-alt));
  color: var(--accent-on);
  cursor: pointer;
}

.danger {
  border-radius: 999px;
  border: 1px solid #ef4444;
  padding: 8px 0;
  background: transparent;
  color: #ef4444;
  cursor: pointer;
}
</style>

