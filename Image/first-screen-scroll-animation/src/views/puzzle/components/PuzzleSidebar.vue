<script setup lang="ts">
import { nextTick, ref } from 'vue';
import type { QueueUploadMode } from '../composables/usePuzzleEditor';
import { usePuzzleContext } from '../context';

const {
  IMAGE_ZOOM_STEP,
  MAX_IMAGE_ZOOM,
  MIN_IMAGE_ZOOM,
  cellCount,
  getRangeStyle,
  draggedImageId,
  images,
  imageQueueDragState,
  insertFiles,
  isImageUploadLimitReached,
  maxOutputHeight,
  maxOutputWidth,
  nudgeSelectedZoom,
  removeImage,
  resetSelectedCellView,
  replaceImage,
  selectedCell,
  selectedImage,
  selectedZoomPercent,
  setSelectedZoom,
  startImageQueueDrag,
} = usePuzzleContext();

const queueFileInputRef = ref<HTMLInputElement>();
const queueUploadAction = ref<{ mode: QueueUploadMode; index: number } | null>(null);

const openQueueImagePicker = async (mode: QueueUploadMode, index: number) => {
  queueUploadAction.value = { mode, index };
  await nextTick();
  queueFileInputRef.value?.click();
};

const handleQueueFileChange = (event: Event) => {
  const input = event.target as HTMLInputElement;
  const action = queueUploadAction.value;
  if (action?.mode === 'replace') void replaceImage(action.index, input.files);
  if (action?.mode === 'insert') void insertFiles(action.index + 1, input.files);
  input.value = '';
  queueUploadAction.value = null;
};
</script>

<template>
  <aside class="side panel">
    <h2>图片队列</h2>
    <p class="hint">导出尺寸：{{ maxOutputWidth }} × {{ maxOutputHeight }}。图片少于格子时循环铺满；图片多于格子时使用前 {{ cellCount }} 个位置。</p>
    <input ref="queueFileInputRef" accept="image/*" :multiple="queueUploadAction?.mode === 'insert'" type="file" hidden
      @change="handleQueueFileChange">
    <div class="thumbs" :class="{ 'is-sorting': imageQueueDragState.active }" aria-label="图片队列">
      <article v-for="(image, index) in images" :key="image.id" class="thumb" :class="{
        active: selectedImage?.id === image.id,
        dragging: draggedImageId === image.id,
        'drag-source': imageQueueDragState.active && imageQueueDragState.startIndex === index && imageQueueDragState.hasMoved,
        'drop-target': imageQueueDragState.active && imageQueueDragState.overIndex === index && imageQueueDragState.startIndex !== index && imageQueueDragState.hasMoved,
      }" :data-image-queue-index="index" role="button" tabindex="0" :aria-label="`拖拽排序 ${image.name}`" @contextmenu.prevent
        @pointerdown="startImageQueueDrag($event, index)">
        <img :src="image.src" :alt="image.name" draggable="false">
        <span class="thumb__order">{{ index + 1 }}</span>
        <span class="thumb__actions" @pointerdown.stop>
          <button class="thumb__action" type="button" :disabled="isImageUploadLimitReached" title="插入图片"
            aria-label="插入图片" @pointerdown.stop @click.stop="openQueueImagePicker('insert', index)">+</button>
          <button class="thumb__action" type="button" title="替换图片" aria-label="替换图片" @pointerdown.stop
            @click.stop="openQueueImagePicker('replace', index)">↻</button>
          <button class="thumb__action thumb__action--danger" type="button" title="移除图片" aria-label="移除图片"
            @pointerdown.stop @click.stop="removeImage(index)">×</button>
        </span>
      </article>
    </div>
    <div v-if="selectedImage && selectedCell" class="meta">
      <strong>当前图片</strong>
      <span>{{ selectedImage.name }}</span>
      <span>{{ selectedImage.naturalWidth }} × {{ selectedImage.naturalHeight }}</span>
      <div class="zoom-panel">
        <div class="zoom-row">
          <strong>区域缩放</strong>
          <span>{{ selectedZoomPercent }}%</span>
        </div>
        <div class="zoom-controls">
          <button class="zoom-button ghost" type="button" @click="nudgeSelectedZoom(-IMAGE_ZOOM_STEP)">-</button>
          <input :value="selectedCell.zoom" :style="getRangeStyle(selectedCell.zoom, MIN_IMAGE_ZOOM, MAX_IMAGE_ZOOM)"
            :min="MIN_IMAGE_ZOOM" :max="MAX_IMAGE_ZOOM" step="0.01" type="range" aria-label="调整当前区域图片缩放"
            @input="setSelectedZoom(Number(($event.target as HTMLInputElement).value))">
          <button class="zoom-button ghost" type="button" @click="nudgeSelectedZoom(IMAGE_ZOOM_STEP)">+</button>
        </div>
        <button class="reset-view ghost" type="button" @click="resetSelectedCellView">重置位置和缩放</button>
      </div>
    </div>
  </aside>
</template>

<style scoped lang="scss">
@use "../styles.scss";
</style>
