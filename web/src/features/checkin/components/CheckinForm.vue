<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { FileApi } from "@/features/file/api/index";
import { useCheckinsStore, usePlansStore } from "@/stores";
import ImagePreviewList from "@/features/file/components/ImagePreviewList.vue";
import {
  notifySuccess,
  notifyError,
  notifyWarning,
} from "@/utils/notification";

const props = defineProps<{
  planId?: number;
  date?: Date;
  timeSlotId?: number;
}>();

const emit = defineEmits<{
  (e: "success"): void;
  (e: "closed"): void;
}>();

const checkinsStore = useCheckinsStore();
const plansStore = usePlansStore();

const note = ref("");
const images = ref<File[]>([]);
const previewSrcs = ref<string[]>([]);
const loading = ref(false);

const currentPlan = computed(() => {
  return plansStore.items.find((p) => p.id === props.planId);
});

function clearPreviews(): void {
  for (const src of previewSrcs.value) {
    URL.revokeObjectURL(src);
  }
  previewSrcs.value = [];
}

function resetForm(): void {
  note.value = "";
  images.value = [];
  clearPreviews();
}

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

async function uploadImages(): Promise<string[]> {
  const urls: string[] = [];
  for (const file of images.value) {
    const formData = new FormData();
    formData.append("file", file);
    const res = await FileApi.UploadImage(formData);
    urls.push(res.data.url);
  }
  return urls;
}

function handleFilesChange(event: Event) {
  const input = event.target as HTMLInputElement;
  if (input.files) {
    const newFiles = Array.from(input.files);
    if (images.value.length + newFiles.length > 3) {
      notifyWarning("最多只能上传三张图片");
      return;
    }
    images.value.push(...newFiles);
    newFiles.forEach((file) => {
      previewSrcs.value.push(URL.createObjectURL(file));
    });
  }
  input.value = "";
}

function removeImage(index: number) {
  images.value.splice(index, 1);
  const src = previewSrcs.value[index];
  if (src) URL.revokeObjectURL(src);
  previewSrcs.value.splice(index, 1);
}

async function handleSubmit(): Promise<void> {
  if (!props.planId || !props.date) return;
  if (images.value.length < 1) {
    notifyWarning("请至少上传一张图片");
    return;
  }
  if (images.value.length > 3) {
    notifyWarning("最多只能上传三张图片");
    return;
  }

  // Check time slot requirement
  if (
    currentPlan.value?.timeSlots &&
    currentPlan.value.timeSlots.length > 0 &&
    !props.timeSlotId
  ) {
    notifyWarning("请选择打卡时间段");
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
      timeSlotId: props.timeSlotId || undefined,
    };

    if (targetOnly.getTime() === todayOnly.getTime()) {
      // Check if force retro needed for today
      let forceRetro = false;
      if (props.timeSlotId && currentPlan.value?.timeSlots) {
        const slot = currentPlan.value.timeSlots.find(
          (s) => s.id === props.timeSlotId,
        );
        if (slot) {
          const now = new Date();
          const nowTimeStr = now.toTimeString().split(" ")[0] || "";
          if (nowTimeStr > slot.endTime) {
            forceRetro = true;
          }
        }
      }

      if (forceRetro) {
        const isoDate = formatDateOnly(target);
        await checkinsStore.retroCheckin({ ...payload, date: isoDate });
        notifySuccess("补签成功");
      } else {
        await checkinsStore.dailyCheckin(payload);
        notifySuccess("今日打卡成功");
      }
    } else if (targetOnly.getTime() < todayOnly.getTime()) {
      const isoDate = formatDateOnly(target);
      await checkinsStore.retroCheckin({ ...payload, date: isoDate });
      notifySuccess("补签成功");
    } else {
      notifyError("仅支持今日打卡或过去日期补签");
      return;
    }

    resetForm();
    emit("success");
  } catch {
    notifyError("打卡失败，请稍后重试");
  } finally {
    loading.value = false;
  }
}

function handleClosed(): void {
  resetForm();
}

watch(
  () => [props.date],
  ([date, open]) => {
    if (open && date) {
      resetForm();
    }
  },
);

onBeforeUnmount(() => {
  clearPreviews();
});
// 暴露出组件handleClosed()
defineExpose({
  handleClosed,
});
</script>

<template>
  <div class="drawer-form">
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
      {{ loading ? "提交中..." : "提交打卡" }}
    </button>
  </div>
</template>

<style scoped>
.drawer-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 0 12px;
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
  display: flex;
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
