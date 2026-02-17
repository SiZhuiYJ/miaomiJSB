<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { http } from '../api/http';
import { API_BASE_URL } from '../config';
import { useCheckinsStore } from '../stores/checkins';
import { usePlansStore, type PlanSummary, type TimeSlotDto } from '../stores/plans';
import { useAuthStore } from "../stores/auth"
import ImagePreviewList from './ImagePreviewList.vue';
import { notifySuccess, notifyError, notifyWarning } from '../utils/notification';
import { compressImageToWebP } from '../utils/image';

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

// ... emit and visible ...

const checkinsStore = useCheckinsStore();
const plansStore = usePlansStore();
const authStore = useAuthStore();

const visible = ref<boolean>(false);

const note = ref('');
const images = ref<File[]>([]);
const previewSrcs = ref<string[]>([]);
const loading = ref(false);
const selectedTimeSlotId = ref<number | null>(null);

const currentPlan = computed(() => {
  return plansStore.items.find(p => p.id === props.planId);
});

function clearPreviews(): void {
  for (const src of previewSrcs.value) {
    URL.revokeObjectURL(src);
  }
  previewSrcs.value = [];
}

function resetForm(): void {
  note.value = '';
  images.value = [];
  selectedTimeSlotId.value = null;
  clearPreviews();
}

// ... handleFilesChange, removeImage, formatDateOnly, uploadImages ...


async function uploadImages(filePath: string): Promise<string> {
  return new Promise((resolve, reject) => {
    http.uploadFile({
      url: `${API_BASE_URL}/mm/Files/images`,
      filePath: filePath,
      name: 'file',
      header: {
        Authorization: `Bearer ${authStore.accessToken}`,
      },
      success: (uploadFileRes) => {
        if (uploadFileRes.statusCode === 200) {
          const data = JSON.parse(uploadFileRes.data);
          resolve(data.url);
        } else {
          reject(new Error('Upload failed'));
        }
      },
      fail: (err) => {
        reject(err);
      },
    });
  });
}


async function handleSubmit(): Promise<void> {
  if (!props.planId || !props.date) return;
  if (images.value.length < 1) {
    notifyWarning('请至少上传一张图片');
    return;
  }
  if (images.value.length > 3) {
    notifyWarning('最多只能上传三张图片');
    return;
  }

  // Check time slot requirement
  if (currentPlan.value?.timeSlots && currentPlan.value.timeSlots.length > 0 && !selectedTimeSlotId.value) {
    notifyWarning('请选择打卡时间段');
    return;
  }

  loading.value = true;
  try {
    const urls = await uploadImages();

    const todayDate = new Date();
    const target = props.date;
    const todayOnly = new Date(
      todayDate.getFullYear(),
      todayDate.getMonth(),
      todayDate.getDate(),
    );
    const targetOnly = new Date(
      target.getFullYear(),
      target.getMonth(),
      target.getDate(),
    );

    const payload = {
      planId: props.planId,
      imageUrls: urls,
      note: note.value || undefined,
      timeSlotId: selectedTimeSlotId.value || undefined,
    };

    if (targetOnly.getTime() === todayOnly.getTime()) {
      // Check if force retro needed for today
      let forceRetro = false;
      if (selectedTimeSlotId.value && currentPlan.value?.timeSlots) {
        const slot = currentPlan.value.timeSlots.find(s => s.id === selectedTimeSlotId.value);
        if (slot) {
          const now = new Date();
          const nowTimeStr = now.toTimeString().split(' ')[0];
          if (nowTimeStr > slot.endTime) {
            forceRetro = true;
          }
        }
      }

      if (forceRetro) {
        const isoDate = formatDateOnly(target);
        await checkinsStore.retroCheckin({ ...payload, date: isoDate });
        notifySuccess('补签成功');
      } else {
        await checkinsStore.dailyCheckin(payload);
        notifySuccess('今日打卡成功');
      }
    } else if (targetOnly.getTime() < todayOnly.getTime()) {
      const isoDate = formatDateOnly(target);
      await checkinsStore.retroCheckin({ ...payload, date: isoDate });
      notifySuccess('补签成功');
    } else {
      notifyError('仅支持今日打卡或过去日期补签');
      return;
    }

    resetForm();
    visible.value = false;
    emit('success');
  } catch {
    notifyError('打卡失败，请稍后重试');
  } finally {
    loading.value = false;
  }
}

function handleClosed(): void {
  resetForm();
}

watch(
  () => [props.date, visible.value],
  ([date, open]) => {
    if (open && date) {
      resetForm();
    }
  },
);

onBeforeUnmount(() => {
  clearPreviews();
});
</script>

<template>
  <el-drawer v-model="visible" direction="btt" size="auto" :title="props.date ? '打卡 / 补签' : '打卡'"
    @closed="handleClosed">
    <div class="drawer-body">
      <p v-if="props.date" class="drawer-date">
        <!-- 目标日期：{{ formatDateOnly(props.date) }} -->
        目标日期：{{ props.date }}

      </p>

      <div v-if="currentPlan?.timeSlots?.length" class="time-slot-selection">
        <p class="section-label">选择打卡时间段</p>
        <div class="slots-grid">
          <div v-for="slot in currentPlan.timeSlots" :key="slot.id" class="slot-option"
            :class="{ active: selectedTimeSlotId === slot.id }" @click="selectedTimeSlotId = slot.id || null">
            <span class="slot-name">{{ slot.slotName }}</span>
            <span class="slot-time">{{ slot.startTime.slice(0, 5) }} - {{ slot.endTime.slice(0, 5) }}</span>
          </div>
        </div>
      </div>

      <label class="field">
        <span>备注</span>
        <textarea v-model="note" rows="3" />
      </label>
      <label class="field">
        <span>上传图片（至少 1 张，最多 3 张）</span>
        <input type="file" accept="image/*" multiple @change="handleFilesChange" />
      </label>
      <ImagePreviewList :sources="previewSrcs" removable @remove="removeImage" />
      <button type="button" class="primary" :disabled="loading" @click="handleSubmit">
        {{ loading ? '提交中...' : '提交打卡' }}
      </button>
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
  background: linear-gradient(to right, var(--accent-color), var(--accent-alt));
  color: var(--accent-on);
  cursor: pointer;
}

.drawer-date {
  font-size: 14px;
  color: var(--text-muted);
}

.time-slot-selection {
  margin-bottom: 16px;
}

.section-label {
  font-size: 14px;
  color: var(--text-muted);
  margin-bottom: 8px;
}

.slots-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
}

.slot-option {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 10px;
  border: 1px solid var(--border-color);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  background-color: var(--bg-primary);
}

.slot-option.active {
  border-color: var(--accent-color);
  background-color: rgba(var(--accent-color-rgb), 0.05);
  color: var(--accent-color);
}

.slot-name {
  font-weight: 500;
  font-size: 14px;
}

.slot-time {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 4px;
}
</style>
