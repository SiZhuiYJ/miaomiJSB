<script setup lang="ts">
import { computed, ref } from 'vue'
import { FileUp, LoaderCircle, Play, RotateCcw } from 'lucide-vue-next'

import type { OcrProgress } from '@/types/ocr'

const props = defineProps<{
  statusMessage: string
  selectedFileName: string
  canRun: boolean
  isBusy: boolean
  progress: OcrProgress | null
  imageSize: { width: number; height: number }
}>()

const emit = defineEmits<{
  selectFile: [file: File]
  run: []
  clear: []
}>()

const fileInput = ref<HTMLInputElement | null>(null)

const imageMeta = computed(() => {
  if (!props.selectedFileName) return '未选择图片'
  if (!props.imageSize.width || !props.imageSize.height) return props.selectedFileName
  return `${props.selectedFileName} · ${props.imageSize.width} x ${props.imageSize.height}`
})

function openFilePicker() {
  fileInput.value?.click()
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (file) emit('selectFile', file)
  input.value = ''
}
</script>

<template>
  <section class="toolbar" aria-label="OCR 控制台">
    <div class="toolbar-actions">
      <input ref="fileInput" type="file" accept="image/*" class="file-input" @change="onFileChange" />

      <button type="button" class="tool-button primary" @click="openFilePicker">
        <FileUp :size="17" aria-hidden="true" />
        <span>选择图片</span>
      </button>

      <button type="button" class="tool-button" :disabled="!canRun" @click="emit('run')">
        <LoaderCircle v-if="isBusy" :size="17" class="spin" aria-hidden="true" />
        <Play v-else :size="17" aria-hidden="true" />
        <span>{{ isBusy ? '识别中' : '开始识别' }}</span>
      </button>

      <button type="button" class="icon-button" :disabled="isBusy || !selectedFileName" title="清空" @click="emit('clear')">
        <RotateCcw :size="17" aria-hidden="true" />
      </button>
    </div>

    <div class="toolbar-status">
      <div class="status-line">{{ statusMessage }}</div>
      <div class="file-line">{{ imageMeta }}</div>
      <div v-if="progress?.total" class="progress-track" aria-hidden="true">
        <span :style="{ width: `${Math.round(((progress.current ?? 0) / progress.total) * 100)}%` }"></span>
      </div>
    </div>
  </section>
</template>
