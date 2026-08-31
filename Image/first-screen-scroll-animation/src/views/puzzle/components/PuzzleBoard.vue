<script setup lang="ts">
import { usePuzzleContext } from '../context';

const {
  boardRef,
  boardWrapStyle,
  cells,
  fileInputRef,
  gapStyle,
  getCellStyle,
  getHandleStyle,
  getImageStyle,
  getLineStyle,
  horizontalLineSegments,
  images,
  lineColor,
  seamHandles,
  selectedIndex,
  selectCell,
  startHandleDrag,
  startImageDrag,
  startSegmentDrag,
  verticalLineSegments,
  zoomCellFromWheel,
  openFilePickerForCell,
} = usePuzzleContext();
</script>

<template>
  <div class="canvas-shell panel">
    <div class="board-wrap" :style="boardWrapStyle">
      <div ref="boardRef" class="puzzle-board" :style="{ background: gapStyle === 'line' ? lineColor : '#fff' }">
        <article v-for="cellInfo in cells" :key="cellInfo.index" class="puzzle-cell"
          :class="{ selected: selectedIndex === cellInfo.index }" :style="getCellStyle(cellInfo.row, cellInfo.col)"
          @click="selectCell(cellInfo.index)">
          <template v-if="cellInfo.cell && cellInfo.image">
            <div class="cell-image-mask" @pointerdown="startImageDrag($event, cellInfo.index)"
              @wheel.prevent="zoomCellFromWheel($event, cellInfo.index)">
              <img class="cell-image" :src="cellInfo.image.src" :alt="cellInfo.image.name"
                :style="getImageStyle(cellInfo.row, cellInfo.col, cellInfo.cell, cellInfo.image)" draggable="false">
            </div>
          </template>
          <button v-else class="empty-cell" type="button" :title="`为第 ${cellInfo.index + 1} 格添加图片`"
            @click.stop="openFilePickerForCell(cellInfo.index)">+ 添加图片</button>
        </article>

        <template v-if="images.length">
          <button v-for="segment in horizontalLineSegments" :key="segment.id" class="seam-line seam-line--horizontal"
            :style="getLineStyle(segment)" type="button" aria-label="拖动水平分割线"
            @pointerdown="startSegmentDrag($event, segment)" />
          <button v-for="segment in verticalLineSegments" :key="segment.id" class="seam-line seam-line--vertical"
            :style="getLineStyle(segment)" type="button" aria-label="拖动垂直分割线"
            @pointerdown="startSegmentDrag($event, segment)" />
          <button v-for="handle in seamHandles" :key="handle.id" class="seam-point" :style="getHandleStyle(handle)"
            type="button" aria-label="拖动分割端点" @pointerdown="startHandleDrag($event, handle)" />
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
@use "../styles.scss";
</style>
