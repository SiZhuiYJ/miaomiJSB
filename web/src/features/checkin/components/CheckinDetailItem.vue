<script setup lang="ts">
import { ref } from "vue";
import ImagePreviewList from "@/components/ImagePreviewList.vue";
import type { CheckinDetail } from "@/features/checkin/types";
const detailLoading = ref(false);
const props = defineProps<{
  checkinDetail?: CheckinDetail;
  imageObjectUrls?: Map<string, string>;
}>();
function getBlobUrl(url: string): string {
  return props.imageObjectUrls?.get(url) || "";
}
</script>

<template>
  <div v-if="checkinDetail" class="detail-item">
    <p class="drawer-status">
      状态：
      <span v-if="checkinDetail.status === 1">正常打卡</span>
      <span v-else-if="checkinDetail.status === 2">补签</span>
      <span v-else>未知</span>
    </p>
    <p v-if="checkinDetail.note" class="drawer-note">
      备注：{{ checkinDetail.note }}
    </p>
    <div v-if="checkinDetail.imageUrls.length">
      <ImagePreviewList
        :sources="
          checkinDetail.imageUrls.map((url) => getBlobUrl(url)).filter(Boolean)
        "
      />
    </div>
    <p v-else class="no-images">无图片</p>
  </div>
  <div v-else-if="detailLoading" class="detail-loading">加载中...</div>
  <div v-else class="no-data">暂无打卡记录</div>
</template>

<style scoped lang="scss">
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
