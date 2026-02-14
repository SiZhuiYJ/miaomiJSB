<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { http } from '../api/http';
import { useCheckinsStore, type CheckinDetail } from '../stores/checkins';
import ImagePreviewList from './ImagePreviewList.vue';
import { notifyError } from '../utils/notification';

const props = defineProps({
  // ... (keep props same)
});

// ... (keep emit and visible same)

const checkinsStore = useCheckinsStore();

function formatDateOnly(date: Date): string {
  // ... (keep same)
}

const detailLoading = ref(false);
const detail = ref<CheckinDetail[] | null>(null); // Changed to array
// const imageSrcs = ref<string[]>([]); // Removed, handle in template or helper

function getImageUrl(relativeUrl: string): string {
  // Assuming relativeUrl is like /mm/files/images/key
  // We need to fetch blob and create object URL or use direct URL if auth handled by cookie/header
  // For simplicity here, let's assume we need to load them. 
  // Actually, CheckinDetailDrawer logic for loading images was complex (blob).
  // To support list, we might need a better way or component.
  // Let's rely on ImagePreviewList handling URLs or refactor loadImages.
  // Given current implementation of loadImages clears previous ones, it's not suitable for list.
  // Let's change ImagePreviewList to accept direct URLs if possible, or we fetch all blobs.
  return relativeUrl;
}

// Since we changed to list, the old logic of `loadImages` and `imageSrcs` is tricky.
// We should probably just pass the relative URLs to ImagePreviewList and let it handle authentication?
// Or we fetch all blobs and map them.
// Let's try to adapt `loadImages` to map specific item's images.
// Actually, `ImagePreviewList` takes `sources`.
// Let's refactor: store objectUrls in a map keyed by something or just fetch all.

const imageObjectUrls = ref<Map<string, string>>(new Map());

async function loadImagesForList(details: CheckinDetail[]): Promise<void> {
  // Clear old
  for (const url of imageObjectUrls.value.values()) {
    URL.revokeObjectURL(url);
  }
  imageObjectUrls.value.clear();

  for (const item of details) {
    for (const url of item.imageUrls) {
      if (!imageObjectUrls.value.has(url)) {
        try {
          const response = await http.get<Blob>(url, { responseType: 'blob' });
          const objectUrl = URL.createObjectURL(response.data);
          imageObjectUrls.value.set(url, objectUrl);
        } catch { }
      }
    }
  }
}

async function fetchDetail(): Promise<void> {
  if (!props.planId || !props.date) return;

  const iso = formatDateOnly(props.date);
  detailLoading.value = true;
  try {
    const result = await checkinsStore.getCheckinDetail(props.planId, iso);
    detail.value = result; // result is now CheckinDetail[]
    await loadImagesForList(result);
  } catch {
    notifyError('加载打卡详情失败');
    visible.value = false;
  } finally {
    detailLoading.value = false;
  }
}

function handleClosed(): void {
  for (const url of imageObjectUrls.value.values()) {
    URL.revokeObjectURL(url);
  }
  imageObjectUrls.value.clear();
  detail.value = null;
}

watch(
  () => visible.value,
  (open) => {
    if (!open) {
      handleClosed();
    }
  },
);

// ... (keep watch date same)

onBeforeUnmount(() => {
  handleClosed();
});

// Helper for template
function getBlobUrl(url: string): string {
  return imageObjectUrls.value.get(url) || '';
}
</script>

<template>
  <el-drawer v-model="visible" direction="btt" size="auto" title="打卡详情" @closed="handleClosed">
    <div class="drawer-body">
      <p v-if="props.date" class="drawer-date">
        打卡日期：{{ formatDateOnly(props.date) }}
      </p>
      <div v-if="detail && Array.isArray(detail)" class="detail-list">
        <div v-for="(item, index) in detail" :key="index" class="detail-item">
          <p class="drawer-status">
            状态：
            <span v-if="item.status === 1">正常打卡</span>
            <span v-else-if="item.status === 2">补签</span>
            <span v-else>未知</span>
            <span v-if="item.timeSlotId" class="time-slot-tag"> (时间段 {{ item.timeSlotId }})</span>
          </p>
          <p v-if="item.note" class="drawer-note">备注：{{ item.note }}</p>
          <div v-if="item.imageUrls.length">
            <ImagePreviewList :sources="item.imageUrls.map(url => getBlobUrl(url)).filter(Boolean)" />
          </div>
          <p v-else class="no-images">无图片</p>
        </div>
      </div>
      <div v-else-if="detailLoading" class="detail-loading">加载中...</div>
      <div v-else class="no-data">暂无打卡记录</div>
    </div>
  </el-drawer>
</template>

<style scoped>
/* ... existing styles ... */
.detail-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.detail-item {
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 16px;
}

.detail-item:last-child {
  border-bottom: none;
}

.time-slot-tag {
  color: var(--text-muted);
  font-size: 12px;
  margin-left: 8px;
}

.no-data {
  text-align: center;
  color: var(--text-muted);
  padding: 20px 0;
}

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
