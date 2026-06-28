<script setup lang="ts">
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
  maxOutputHeight,
  maxOutputWidth,
  nudgeSelectedZoom,
  resetSelectedCellView,
  selectedCell,
  selectedImage,
  selectedZoomPercent,
  setSelectedZoom,
  startImageQueueDrag,
} = usePuzzleContext();
</script>

<template>
  <aside class="side panel">
    <h2>图片队列</h2>
    <p class="hint">导出尺寸：{{ maxOutputWidth }} × {{ maxOutputHeight }}。图片少于格子时循环铺满；图片多于格子时使用前 {{ cellCount }} 个位置。</p>
    <div class="thumbs" :class="{ 'is-sorting': imageQueueDragState.active }" aria-label="图片队列">
      <button v-for="(image, index) in images" :key="image.id" class="thumb" :class="{
        active: selectedImage?.id === image.id,
        dragging: draggedImageId === image.id,
        'drop-target': imageQueueDragState.overIndex === index && draggedImageId !== image.id,
      }" :data-image-queue-index="index" type="button" :aria-label="`拖拽排序 ${image.name}`" @contextmenu.prevent
        @pointerdown="startImageQueueDrag($event, index)">
        <img :src="image.src" :alt="image.name" draggable="false">
        <span class="thumb__order">{{ index + 1 }}</span>
      </button>
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
