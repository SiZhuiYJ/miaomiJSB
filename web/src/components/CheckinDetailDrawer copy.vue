<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch, onMounted } from "vue";
import { FileApi } from "@/features/file/api/index";
import { useCheckinsStore, usePlansStore } from "@/stores";
import type { CheckinDetail } from "@/features/checkin/types";
import type { TimeSlotDto } from "@/features/plans/types";
import ImagePreviewList from "./ImagePreviewList.vue";
import SimpleTimeSlotCard from "./SimpleTimeSlotCard.vue";
import { notifyError } from "../utils/notification";

const props = defineProps<{
  modelValue: boolean;
  planId?: number;
  date?: Date;
  mode?: "default" | "timeSlot"; // 添加模式控制
  timeSlots: TimeSlotDto[];
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: boolean): void;
  (e: "closed"): void;
  (e: "open-checkin"): void;
}>();

const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit("update:modelValue", val),
});

const checkinsStore = useCheckinsStore();
const plansStore = usePlansStore();

const currentPlan = computed(() => {
  return props.planId
    ? plansStore.items.find((p) => p.id === props.planId)
    : null;
});

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

// 时间段模式下的状态计算
const timeSlotCheckinStatus = computed(() => {
  if (!detail.value || !Array.isArray(detail.value)) return null;
  // 如果有任何打卡记录，则认为已打卡
  return detail.value.some((item) => item.status === 1)
    ? 1
    : detail.value.some((item) => item.status === 2)
      ? 2
      : null;
});

const hasTimeSlots = computed(() => {
  return currentPlan.value?.timeSlots && currentPlan.value.timeSlots.length > 0;
});

function getTimeSlots() {
  if (!currentPlan.value?.timeSlots) return [];
  return currentPlan.value.timeSlots
    .filter((slot) => slot.id !== undefined)
    .map((slot) => ({
      id: slot.id!,
      slotName: slot.slotName || "",
      startTime: slot.startTime,
      endTime: slot.endTime,
    }));
}

const detailLoading = ref(false);
const detail = ref<CheckinDetail[] | null>(null);
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
          const response = await FileApi.GetImage(url);

          // 将 ArrayBuffer 转换为 Blob
          let blob: Blob;
          if (response.data instanceof ArrayBuffer) {
            blob = new Blob([response.data], { type: "image/webp" });
          } else if (response.data instanceof Blob) {
            blob = response.data;
          } else {
            // 如果是其他类型，尝试转换
            try {
              blob = new Blob(
                [new Uint8Array(response.data as unknown as ArrayBuffer)],
                { type: "image/webp" },
              );
            } catch (convertError) {
              console.error("Failed to convert data to Blob:", convertError);
              // 作为 fallback，创建一个空的 Blob
              blob = new Blob([], { type: "image/webp" });
            }
          }

          const objectUrl = URL.createObjectURL(blob);
          imageObjectUrls.value.set(url, objectUrl);
        } catch (error) {
          console.error("Failed to load image:", url, error);
          // 在失败时设置一个占位符URL
          imageObjectUrls.value.set(url, "");
        }
      }
    }
  }
}

async function fetchDetail(): Promise<void> {
  if (!props.planId || !props.date) return;

  const iso = formatDateOnly(props.date);
  detailLoading.value = true;
  try {
    console.log("iso", iso);
    const result = await checkinsStore.getCheckinDetail(props.planId, iso);
    detail.value = result;
    console.log("加载打卡详情", detail.value);
    await loadImagesForList(result);
  } catch {
    notifyError("加载打卡详情失败");
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
    } else {
      fetchDetail();
    }
  },
);

watch(
  () => [props.planId, props.date],
  () => {
    if (visible.value) {
      fetchDetail();
    }
  },
);
onMounted(() => {
  console.log("props", props);
});
onBeforeUnmount(() => {
  handleClosed();
});

// Helper for template
function getBlobUrl(url: string): string {
  return imageObjectUrls.value.get(url) || "";
}
</script>

<template>
  <el-drawer
    v-model="visible"
    direction="btt"
    size="auto"
    :title="'打卡详情' + (props.date ? ' · ' + formatDateOnly(props.date) : '')"
    @closed="handleClosed"
  >
    <div class="drawer-body">
      <div v-if="detail && Array.isArray(detail)" class="detail-list">
        <div v-for="(item, index) in detail" :key="index" class="detail-item">
          <p class="drawer-status">
            状态：
            <span v-if="item.status === 1">正常打卡</span>
            <span v-else-if="item.status === 2">补签</span>
            <span v-else>未知</span>
            <span v-if="item.timeSlotId" class="time-slot-tag">
              (时间段 {{ item.timeSlotId }})</span
            >
          </p>
          <p v-if="item.note" class="drawer-note">备注：{{ item.note }}</p>
          <div v-if="item.imageUrls.length">
            <ImagePreviewList
              :sources="
                item.imageUrls.map((url) => getBlobUrl(url)).filter(Boolean)
              "
            />
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
:deep(.el-drawer__header) {
  margin-bottom: 0px;
}
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
