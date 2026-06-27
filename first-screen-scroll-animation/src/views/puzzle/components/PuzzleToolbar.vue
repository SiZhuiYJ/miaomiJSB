<script setup lang="ts">
import { usePuzzleContext } from '../context';

const {
  presets,
  ratios,
  rows,
  cols,
  gapStyle,
  lineColor,
  lineWidth,
  featherSize,
  ratioKey,
  exportFormats,
  exportFormat,
  exportQualityEnabled,
  quality,
  fileInputRef,
  images,
  getRangeStyle,
  addFiles,
  clearImages,
  applyPresetByIndex,
  exportPuzzle,
} = usePuzzleContext();

const handleFileChange = (event: Event) => {
  const input = event.target as HTMLInputElement;
  void addFiles(input.files);
  input.value = '';
};
</script>

<template>
  <section class="toolbar panel">
    <div class="brand">
      <span class="brand__badge">Puzzle Lab</span>
      <h1>多图片拼图工作台</h1>
      <p>图片会自动铺满当前区域；拖动图片只改变可视区域，拖动单个点或单条线段只改变对应分界，不会拉伸图片。</p>
    </div>
    <div class="control-grid">
      <label>拼图模式
        <select @change="applyPresetByIndex(Number(($event.target as HTMLSelectElement).value))">
          <option v-for="(preset, index) in presets" :key="preset.label" :value="index">{{ preset.label }}</option>
        </select>
      </label>
      <label>行数<input v-model.number="rows" min="1" max="10" type="number"></label>
      <label>列数<input v-model.number="cols" min="1" max="10" type="number"></label>
      <label>画布比例
        <select v-model="ratioKey">
          <option v-for="item in ratios" :key="item.value" :value="item.value">{{ item.label }}</option>
        </select>
      </label>
      <label>分隔样式
        <select v-model="gapStyle">
          <option value="seamless">无缝</option>
          <option value="line">线条</option>
          <option value="feather">图片边缘羽化过渡</option>
        </select>
      </label>
      <label v-if="gapStyle !== 'seamless'">分隔宽度：{{ lineWidth }}
        <input v-model.number="lineWidth" :style="getRangeStyle(lineWidth, 1, 18)" min="1" max="18" type="range">
      </label>
      <label v-if="gapStyle === 'line'">线条颜色<input v-model="lineColor" type="color"></label>
      <label v-if="gapStyle === 'feather'">羽化强度：{{ featherSize }}
        <input v-model.number="featherSize" :style="getRangeStyle(featherSize, 4, 48)" min="4" max="48" type="range">
      </label>
      <label>导出格式
        <select v-model="exportFormat">
          <option v-for="item in exportFormats" :key="item.value" :value="item.value">{{ item.label }}</option>
        </select>
      </label>
      <label v-if="exportQualityEnabled">导出质量：{{ Math.round(quality * 100) }}%
        <input v-model.number="quality" :style="getRangeStyle(quality, 0.82, 1)" min="0.82" max="1" step="0.01" type="range">
      </label>
    </div>
    <div class="actions">
      <input
        ref="fileInputRef"
        accept="image/*"
        multiple
        type="file"
        hidden
        @change="handleFileChange"
      >
      <button type="button" @click="fileInputRef?.click()">选择图片</button>
      <button type="button" :disabled="!images.length" @click="exportPuzzle">导出高清图片</button>
      <button class="ghost" type="button" :disabled="!images.length" @click="clearImages">清空</button>
    </div>
  </section>
</template>
