<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { http } from '../api/http';
import { useCheckinsStore, type CheckinDetail } from '../stores/checkins';
import ImagePreviewList from './ImagePreviewList.vue';
import { notifyError } from '../utils/notification';

const props = defineProps({
  modelValue: {
    type: Boolean,
    required: true,
  },
  planId: {
    type: Number,
    default: null,
  },
  date: {
    type: Date,
    default: null,
  },
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
}>();

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const checkinsStore = useCheckinsStore();

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

const detailLoading = ref(false);
const detail = ref<CheckinDetail | null>(null);
const imageSrcs = ref<string[]>([]);

function clearImages(): void {
  for (const src of imageSrcs.value) {
    URL.revokeObjectURL(src);
  }
  imageSrcs.value = [];
}

async function loadImages(urls: string[]): Promise<void> {
  clearImages();
  for (const url of urls) {
    try {
      const response = await http.get<Blob>(url, { responseType: 'blob' });
      const objectUrl = URL.createObjectURL(response.data);
      imageSrcs.value.push(objectUrl);
    } catch {
    }
  }
}

async function fetchDetail(): Promise<void> {
  if (!props.planId || !props.date) return;

  const iso = formatDateOnly(props.date);
  detailLoading.value = true;
  try {
    const result = await checkinsStore.getCheckinDetail(props.planId, iso);
    detail.value = result;
    await loadImages(result.imageUrls);
  } catch {
    notifyError('加载打卡详情失败');
    visible.value = false;
  } finally {
    detailLoading.value = false;
  }
}

function handleClosed(): void {
  clearImages();
  detail.value = null;
}

watch(
  () => visible.value,
  (open) => {
    if (!open) {
      clearImages();
      detail.value = null;
    }
  },
);

watch(
  () => props.date,
  async (value, oldValue) => {
    if (visible.value && value && value !== oldValue) {
      await fetchDetail();
    }
  },
);

onBeforeUnmount(() => {
  clearImages();
});
</script>

<template>
  <el-drawer
    v-model="visible"
    direction="btt"
    size="auto"
    title="打卡详情"
    @closed="handleClosed"
  >
    <div class="drawer-body">
      <p v-if="props.date" class="drawer-date">
        打卡日期：{{ formatDateOnly(props.date) }}
      </p>
      <p v-if="detail" class="drawer-status">
        状态：
        <span v-if="detail.status === 1">正常打卡</span>
        <span v-else-if="detail.status === 2">补签</span>
        <span v-else>未知</span>
      </p>
      <p v-if="detail?.note" class="drawer-note">备注：{{ detail.note }}</p>
      <div v-if="detailLoading" class="detail-loading">加载中...</div>
      <div v-else>
        <template v-if="imageSrcs.length">
          <ImagePreviewList :sources="imageSrcs" />
        </template>
        <p v-else class="no-images">无图片</p>
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

.no-images {
  font-size: 13px;
  color: var(--text-muted);
}
</style>
