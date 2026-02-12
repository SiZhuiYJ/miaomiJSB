<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { http } from '../api/http';
import { useCheckinsStore } from '../stores/checkins';
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

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

const visible = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value),
});

const checkinsStore = useCheckinsStore();

const note = ref('');
const images = ref<File[]>([]);
const previewSrcs = ref<string[]>([]);
const loading = ref(false);

function clearPreviews(): void {
  for (const src of previewSrcs.value) {
    URL.revokeObjectURL(src);
  }
  previewSrcs.value = [];
}

function resetForm(): void {
  note.value = '';
  images.value = [];
  clearPreviews();
}

function handleFilesChange(event: Event): void {
  const target = event.target as HTMLInputElement;
  if (!target.files) return;
  const incoming = Array.from(target.files);
  const merged: File[] = [];
  for (const f of images.value) {
    merged.push(f);
  }
  for (const f of incoming) {
    if (merged.length >= 3) break;
    merged.push(f);
  }
  if (merged.length > 3) {
    notifyWarning('最多只能选择 3 张图片');
  }
  images.value = merged.slice(0, 3);
  clearPreviews();
  for (const file of images.value) {
    const url = URL.createObjectURL(file);
    previewSrcs.value.push(url);
  }
}

function removeImage(index: number): void {
  const preview = previewSrcs.value[index];
  if (preview) {
    URL.revokeObjectURL(preview);
  }
  previewSrcs.value.splice(index, 1);
  images.value.splice(index, 1);
}

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = `${date.getMonth() + 1}`.padStart(2, '0');
  const d = `${date.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${d}`;
}

async function uploadImages(): Promise<string[]> {
  if (images.value.length === 0) return [];
  const urls: string[] = [];
  for (const file of images.value) {
    const form = new FormData();
    try {
      const webpBlob = await compressImageToWebP(file);
      const fileName = file.name.substring(0, file.name.lastIndexOf('.')) + '.webp';
      form.append('file', webpBlob, fileName);
    } catch (error) {
      console.warn('WebP conversion failed, fallback to original', error);
      form.append('file', file);
    }
    
    const response = await http.post<{ url: string }>('/mm/Files/images', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    urls.push(response.data.url);
  }
  return urls;
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

    if (targetOnly.getTime() === todayOnly.getTime()) {
      await checkinsStore.dailyCheckin({
        planId: props.planId,
        imageUrls: urls,
        note: note.value || undefined,
      });
      notifySuccess('今日打卡成功');
    } else if (targetOnly.getTime() < todayOnly.getTime()) {
      const isoDate = formatDateOnly(target);
      await checkinsStore.retroCheckin({
        planId: props.planId,
        date: isoDate,
        imageUrls: urls,
        note: note.value || undefined,
      });
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
        目标日期：{{ formatDateOnly(props.date) }}
      </p>
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
</style>
