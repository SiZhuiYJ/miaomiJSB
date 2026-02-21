<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { FileApi } from "@/features/file/api/index";
import { useCheckinsStore } from "@/stores";
import type { CheckinDetail } from "@/features/checkin/types";
import type { TimeSlotDto } from "@/features/plans/types";
import { notifyError } from "@/utils/notification";
import CheckinDetailItem from "./CheckinDetailItem.vue";
import CheckinForm from "./CheckinForm.vue";

const props = defineProps<{
  modelValue: boolean;
  planId?: number;
  date?: Date;
  mode?: "default" | "timeSlot"; // 添加模式控制
  timeSlots?: TimeSlotDto[];
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

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
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
    const result = await useCheckinsStore().getCheckinDetail(props.planId, iso);
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

// 判断date是否为今天
function isToday(date: Date): boolean {
  const today = new Date();
  return (
    date.getFullYear() === today.getFullYear() &&
    date.getMonth() === today.getMonth() &&
    date.getDate() === today.getDate()
  );
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
onBeforeUnmount(() => {
  handleClosed();
});
const findDetailById = (id: number): CheckinDetail | undefined => {
  return detail.value?.find((c) => c.timeSlotId === id);
};
</script>

<template>
  <el-drawer v-model="visible" direction="btt" size="auto" @closed="handleClosed">
    <template #header="{ titleId, titleClass }">
      <h1 :id="titleId" :class="titleClass">
        打卡详情
        {{ props.date ? ' · ' + formatDateOnly(props.date) : '' }}
        <template v-if="props.mode == 'default' && !detail?.length">
          {{ props.date ? (isToday(props.date) ? ' · 打卡' : ' · 补卡') : '' }}
        </template>
      </h1>
    </template>
    <div class="drawer-body">
      <el-skeleton v-if="detailLoading" class="detail" :rows="3" animated />
      <div v-else-if="!detail" class="detail">
        <p>暂无打卡记录</p>
      </div>
      <div v-else-if="props.mode == 'default'" class="detail">
        <CheckinForm v-if="!detail[0]" :plan-id="props.planId" :date="props.date" @success="fetchDetail" />
        <CheckinDetailItem v-else :checkin-detail="detail[0]" :image-object-urls="imageObjectUrls" />
      </div>
      <div v-else-if="props.mode == 'timeSlot'" v-for="(item, index) in timeSlots" :key="index" class="detail-list">
        <el-collapse expand-icon-position="left">
          <el-collapse-item title="Consistency" name="1">
            <template #title>
              <div class="time-slot-container">
                <span class="time-slot-tag">
                  {{ item.slotName }} · {{ item.startTime }}-{{ item.endTime }}</span>
                <div>
                  <span v-if="!findDetailById(item.id)" class="dot missed">未打卡</span>
                  <span v-else-if="findDetailById(item.id)?.status === 1" class="dot success">
                    已打卡
                  </span>
                  <span v-else-if="findDetailById(item.id)?.status === 2" class="dot retro">已补签</span>
                  <span v-else class="dot unknown">未知</span>
                </div>
              </div>
            </template>
            <CheckinForm v-if="!findDetailById(item.id)" :plan-id="props.planId" :date="props.date"
              :time-slot-id="item.id" @success="fetchDetail" />
            <CheckinDetailItem v-else noStatus :checkin-detail="findDetailById(item.id)!"
              :image-object-urls="imageObjectUrls" />
          </el-collapse-item>
        </el-collapse>
      </div>
    </div>
  </el-drawer>
</template>

<style scoped lang="scss">
.time-slot-container {
  width: 100%;
  display: flex;
  flex-direction: row;
  justify-content: space-between;
  align-items: center;
}

.drawer-body {
  max-height: calc(100vh - 80px);
  overflow-y: auto;
}

.dot {
  padding: 4px;
  border-radius: 8px;
}

.dot.success {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
}

.dot.retro {
  background: rgba(234, 179, 8, 0.2);
  color: #eab308;
}

.dot.missed {
  background: rgba(248, 113, 113, 0.2);
  color: #f87171;
}

.dot.unknown {
  background: #94a3b8;
}
</style>
