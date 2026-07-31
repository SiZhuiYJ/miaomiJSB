<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'

import type { LoadedImagePayload, OcrBox, OcrTextBlock } from '@/types/ocr'

const props = defineProps<{
  imageUrl: string
  boxes: OcrBox[]
  blocks: OcrTextBlock[]
  isBusy: boolean
}>()

const emit = defineEmits<{
  imageLoaded: [payload: LoadedImagePayload]
  rerun: []
}>()

const canvasRef = ref<HTMLCanvasElement | null>(null)
const imageElement = ref<HTMLImageElement | null>(null)
const loadError = ref('')

function getContext(): CanvasRenderingContext2D | null {
  return canvasRef.value?.getContext('2d', { willReadFrequently: true }) ?? null
}

function drawScene() {
  const canvas = canvasRef.value
  const ctx = getContext()
  const image = imageElement.value
  if (!canvas || !ctx || !image) return

  canvas.width = image.naturalWidth
  canvas.height = image.naturalHeight
  ctx.clearRect(0, 0, canvas.width, canvas.height)
  ctx.drawImage(image, 0, 0)

  ctx.lineWidth = Math.max(2, Math.round(Math.max(canvas.width, canvas.height) / 900))
  ctx.font = `${Math.max(14, Math.round(canvas.width / 90))}px system-ui, sans-serif`

  for (const box of props.boxes) {
    ctx.strokeStyle = 'rgba(245, 158, 11, 0.65)'
    ctx.setLineDash([8, 6])
    ctx.strokeRect(box.x0, box.y0, box.x1 - box.x0, box.y1 - box.y0)
  }

  ctx.setLineDash([])
  for (const block of props.blocks) {
    const { box } = block
    const label = `#${block.id}`
    const labelWidth = ctx.measureText(label).width + 12
    const labelHeight = 22
    const labelY = Math.max(0, box.y0 - labelHeight)

    ctx.strokeStyle = 'rgba(16, 185, 129, 0.96)'
    ctx.fillStyle = 'rgba(16, 185, 129, 0.9)'
    ctx.strokeRect(box.x0, box.y0, box.x1 - box.x0, box.y1 - box.y0)
    ctx.fillRect(box.x0, labelY, labelWidth, labelHeight)
    ctx.fillStyle = '#ffffff'
    ctx.fillText(label, box.x0 + 6, labelY + 16)
  }
}

async function loadImage(url: string) {
  loadError.value = ''
  imageElement.value = null
  await nextTick()

  const canvas = canvasRef.value
  const ctx = getContext()
  if (canvas && ctx) {
    ctx.clearRect(0, 0, canvas.width, canvas.height)
  }

  if (!url) return

  const image = new Image()
  image.onload = () => {
    imageElement.value = image
    drawScene()

    const canvas = canvasRef.value
    const ctx = getContext()
    if (!canvas || !ctx) return

    emit('imageLoaded', {
      imageData: ctx.getImageData(0, 0, canvas.width, canvas.height),
      width: image.naturalWidth,
      height: image.naturalHeight,
    })
  }
  image.onerror = () => {
    loadError.value = '图片读取失败'
  }
  image.src = url
}

watch(() => props.imageUrl, loadImage, { immediate: true })
watch(() => [props.blocks, props.boxes], drawScene, { deep: true })
</script>

<template>
  <section class="canvas-panel" aria-label="图片预览">
    <div v-if="!imageUrl" class="empty-canvas">
      <div class="empty-title">选择图片后开始识别</div>
    </div>

    <div v-else class="canvas-frame" :class="{ busy: isBusy }">
      <canvas ref="canvasRef" class="ocr-canvas" title="点击重新识别" @click="emit('rerun')" />
      <div v-if="isBusy" class="canvas-badge">处理中</div>
    </div>

    <div v-if="loadError" class="inline-error">{{ loadError }}</div>
  </section>
</template>
