<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type DividerStyle = 'seamless' | 'line' | 'feather'
type Corner = 'topLeft' | 'topRight' | 'bottomLeft' | 'bottomRight'

type PuzzleMode = {
  label: string
  rows: number
  cols: number
}

type TileCrop = {
  top: number
  right: number
  bottom: number
  left: number
  focusIndex: number
}

type AspectRatioOption = {
  label: string
  value: string
  width: number
  height: number
  description: string
}

const puzzleModes: PuzzleMode[] = [
  { label: '2 × 2', rows: 2, cols: 2 },
  { label: '3 × 3', rows: 3, cols: 3 },
  { label: '4 × 4', rows: 4, cols: 4 },
  { label: '5 × 6', rows: 5, cols: 6 },
]

const dividerOptions: { label: string; value: DividerStyle; description: string }[] = [
  { label: '无缝', value: 'seamless', description: '碎片紧密拼接，不显示间隔。' },
  { label: '线条', value: 'line', description: '使用清晰描边强调每个拼图块。' },
  { label: '边缘羽化', value: 'feather', description: '用柔和渐变让图片边缘自然过渡。' },
]

const focusPresets = [
  { label: '居中', x: 50, y: 50 },
  { label: '左上', x: 24, y: 24 },
  { label: '右上', x: 76, y: 24 },
  { label: '左下', x: 24, y: 76 },
  { label: '右下', x: 76, y: 76 },
]

const screenRatio = getScreenRatio()
const aspectRatioOptions: AspectRatioOption[] = [
  { label: '1:1 方图', value: '1:1', width: 1, height: 1, description: '头像、封面九宫格常用比例。' },
  { label: '4:3 横图', value: '4:3', width: 4, height: 3, description: '传统照片与演示内容常用比例。' },
  { label: '3:4 竖图', value: '3:4', width: 3, height: 4, description: '竖向海报与移动端卡片常用比例。' },
  { label: '16:9 横屏', value: '16:9', width: 16, height: 9, description: '桌面屏、视频封面、横版大图常用比例。' },
  { label: '9:16 竖屏', value: '9:16', width: 9, height: 16, description: '手机全屏故事、短视频封面常用比例。' },
  { label: '3:2 摄影', value: '3:2', width: 3, height: 2, description: '相机照片与作品集常用比例。' },
  { label: '21:9 宽屏', value: '21:9', width: 21, height: 9, description: '超宽屏 Banner 与沉浸式头图常用比例。' },
  { label: `当前显示器 ${screenRatio.width}:${screenRatio.height}`, value: 'screen', width: screenRatio.width, height: screenRatio.height, description: '按当前显示器宽高比预览与导出。' },
]

const currentMode = ref('3 × 3')
const selectedAspectRatio = ref('16:9')
const exportScale = ref(3)
const isExporting = ref(false)
const dividerStyle = ref<DividerStyle>('line')
const imageUrl = ref('https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=1600&q=80')
const activeTileId = ref(0)
const boardRef = ref<HTMLElement | null>(null)
const dragState = ref<{
  tileId: number
  corner: Corner
  startX: number
  startY: number
  startCrop: TileCrop
  tileWidth: number
  tileHeight: number
} | null>(null)

const tileCrops = reactive<Record<number, TileCrop>>({})

const selectedMode = computed<PuzzleMode>(() => puzzleModes.find(mode => mode.label === currentMode.value) ?? puzzleModes[1]!)
const totalTiles = computed(() => selectedMode.value.rows * selectedMode.value.cols)
const activeTile = computed(() => getTileCrop(activeTileId.value))
const dividerDescription = computed(() => dividerOptions.find(option => option.value === dividerStyle.value)?.description ?? '')
const activeFocusLabel = computed(() => (focusPresets[activeTile.value.focusIndex] ?? focusPresets[0]!).label)
const selectedAspect = computed<AspectRatioOption>(() => aspectRatioOptions.find(option => option.value === selectedAspectRatio.value) ?? aspectRatioOptions[3]!)
const boardAspectRatio = computed(() => `${selectedAspect.value.width} / ${selectedAspect.value.height}`)
const exportSize = computed(() => {
  const baseWidth = 1600
  const ratio = selectedAspect.value.height / selectedAspect.value.width

  return {
    width: Math.round(baseWidth * exportScale.value),
    height: Math.round(baseWidth * ratio * exportScale.value),
  }
})

const tiles = computed(() => {
  const { rows, cols } = selectedMode.value

  return Array.from({ length: rows * cols }, (_, id) => {
    const row = Math.floor(id / cols)
    const col = id % cols

    return {
      id,
      row,
      col,
      crop: getTileCrop(id),
    }
  })
})

function getTileCrop(id: number) {
  if (!tileCrops[id]) {
    tileCrops[id] = {
      top: 0,
      right: 0,
      bottom: 0,
      left: 0,
      focusIndex: 0,
    }
  }

  return tileCrops[id]
}

function tileStyle(tile: { row: number; col: number; crop: TileCrop }) {
  const { rows, cols } = selectedMode.value
  const focus = focusPresets[tile.crop.focusIndex] ?? focusPresets[0]!

  return {
    backgroundImage: `url(${imageUrl.value})`,
    backgroundSize: `${cols * 100}% ${rows * 100}%`,
    backgroundPosition: `${(tile.col / Math.max(cols - 1, 1)) * 100}% ${(tile.row / Math.max(rows - 1, 1)) * 100}%`,
    transform: `translate(${(focus.x - 50) * 0.08}px, ${(focus.y - 50) * 0.08}px) scale(${1 + Math.max(tile.crop.left, tile.crop.right, tile.crop.top, tile.crop.bottom) / 180})`,
    clipPath: `inset(${tile.crop.top}% ${tile.crop.right}% ${tile.crop.bottom}% ${tile.crop.left}%)`,
  }
}

function handleImageChange(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]

  if (!file) return

  imageUrl.value = URL.createObjectURL(file)
}

function activateTile(id: number) {
  activeTileId.value = id
  const crop = getTileCrop(id)
  crop.focusIndex = (crop.focusIndex + 1) % focusPresets.length
}

function startDrag(event: PointerEvent, tileId: number, corner: Corner) {
  const targetTile = (event.currentTarget as HTMLElement).closest('.puzzle-tile') as HTMLElement | null

  if (!targetTile) return

  event.preventDefault()
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)

  dragState.value = {
    tileId,
    corner,
    startX: event.clientX,
    startY: event.clientY,
    startCrop: { ...getTileCrop(tileId) },
    tileWidth: targetTile.clientWidth,
    tileHeight: targetTile.clientHeight,
  }
}

function handleDrag(event: PointerEvent) {
  if (!dragState.value) return

  const { tileId, corner, startX, startY, startCrop, tileWidth, tileHeight } = dragState.value
  const crop = getTileCrop(tileId)
  const deltaX = ((event.clientX - startX) / tileWidth) * 100
  const deltaY = ((event.clientY - startY) / tileHeight) * 100

  if (corner.includes('Left')) crop.left = clamp(startCrop.left + deltaX)
  if (corner.includes('Right')) crop.right = clamp(startCrop.right - deltaX)
  if (corner.includes('top')) crop.top = clamp(startCrop.top + deltaY)
  if (corner.includes('bottom')) crop.bottom = clamp(startCrop.bottom - deltaY)
}

function stopDrag() {
  dragState.value = null
}

function clamp(value: number) {
  return Math.min(35, Math.max(0, Number(value.toFixed(2))))
}

function resetActiveTile() {
  Object.assign(getTileCrop(activeTileId.value), {
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    focusIndex: 0,
  })
}

function getScreenRatio() {
  const width = typeof window === 'undefined' ? 16 : window.screen.width
  const height = typeof window === 'undefined' ? 9 : window.screen.height
  const divisor = greatestCommonDivisor(width, height)

  return {
    width: Math.round(width / divisor),
    height: Math.round(height / divisor),
  }
}

function greatestCommonDivisor(a: number, b: number): number {
  return b === 0 ? a : greatestCommonDivisor(b, a % b)
}

function getDividerMetrics(tileWidth: number) {
  if (dividerStyle.value === 'line') return Math.max(8, Math.round(tileWidth * 0.012))

  return 0
}

function loadExportImage() {
  return new Promise<HTMLImageElement>((resolve, reject) => {
    const image = new Image()

    if (!imageUrl.value.startsWith('blob:') && !imageUrl.value.startsWith('data:')) {
      image.crossOrigin = 'anonymous'
    }

    image.onload = () => resolve(image)
    image.onerror = () => reject(new Error('图片加载失败，请更换本地图片后再导出。'))
    image.src = imageUrl.value
  })
}

function drawFeather(ctx: CanvasRenderingContext2D, x: number, y: number, width: number, height: number) {
  const feather = Math.max(18, Math.min(width, height) * 0.12)
  const gradient = ctx.createRadialGradient(x + width / 2, y + height / 2, Math.max(1, Math.min(width, height) / 2 - feather), x + width / 2, y + height / 2, Math.max(width, height) / 2)

  gradient.addColorStop(0, 'rgba(248, 250, 252, 0)')
  gradient.addColorStop(1, 'rgba(248, 250, 252, 0.38)')
  ctx.fillStyle = gradient
  ctx.fillRect(x, y, width, height)
}

async function exportPuzzle() {
  if (isExporting.value) return

  isExporting.value = true

  try {
    const image = await loadExportImage()
    const { rows, cols } = selectedMode.value
    const { width, height } = exportSize.value
    const canvas = document.createElement('canvas')
    const ctx = canvas.getContext('2d')

    if (!ctx) return

    canvas.width = width
    canvas.height = height
    ctx.imageSmoothingEnabled = true
    ctx.imageSmoothingQuality = 'high'
    ctx.fillStyle = '#0f172a'
    ctx.fillRect(0, 0, width, height)

    const gap = getDividerMetrics(width / cols)
    const tileWidth = (width - gap * (cols + 1)) / cols
    const tileHeight = (height - gap * (rows + 1)) / rows

    tiles.value.forEach((tile) => {
      const crop = tile.crop
      const focus = focusPresets[crop.focusIndex] ?? focusPresets[0]!
      const x = gap + tile.col * (tileWidth + gap)
      const y = gap + tile.row * (tileHeight + gap)
      const visibleX = x + tileWidth * (crop.left / 100)
      const visibleY = y + tileHeight * (crop.top / 100)
      const visibleWidth = tileWidth * (1 - (crop.left + crop.right) / 100)
      const visibleHeight = tileHeight * (1 - (crop.top + crop.bottom) / 100)
      const sourceWidth = image.naturalWidth / cols
      const sourceHeight = image.naturalHeight / rows
      const focusOffsetX = ((focus.x - 50) / 50) * sourceWidth * 0.08
      const focusOffsetY = ((focus.y - 50) / 50) * sourceHeight * 0.08
      const sourceX = clampRange(tile.col * sourceWidth + focusOffsetX, 0, image.naturalWidth - sourceWidth)
      const sourceY = clampRange(tile.row * sourceHeight + focusOffsetY, 0, image.naturalHeight - sourceHeight)

      ctx.save()
      ctx.beginPath()
      ctx.rect(visibleX, visibleY, visibleWidth, visibleHeight)
      ctx.clip()
      ctx.drawImage(image, sourceX, sourceY, sourceWidth, sourceHeight, x, y, tileWidth, tileHeight)

      if (dividerStyle.value === 'feather') {
        drawFeather(ctx, x, y, tileWidth, tileHeight)
      }

      ctx.restore()
    })

    const link = document.createElement('a')
    link.download = `puzzle-${selectedAspect.value.value}-${selectedMode.value.rows}x${selectedMode.value.cols}@${exportScale.value}x.png`
    link.href = canvas.toDataURL('image/png', 1)
    link.click()
  } finally {
    isExporting.value = false
  }
}

function clampRange(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value))
}
</script>

<template>
  <main class="puzzle-page" @pointermove="handleDrag" @pointerup="stopDrag" @pointercancel="stopDrag">
    <section class="control-panel" aria-label="拼图设置">
      <p class="eyebrow">Puzzle Editor</p>
      <h1>可调区域拼图</h1>
      <p class="intro">选择拼图行列与分隔样式，拖动每张图片四角的控制点调整展示区域；点击图片会切换当前碎片的展示焦点。</p>

      <label class="field">
        <span>拼图模式</span>
        <select v-model="currentMode">
          <option v-for="mode in puzzleModes" :key="mode.label" :value="mode.label">
            {{ mode.label }}（{{ mode.rows }} 行 × {{ mode.cols }} 列）
          </option>
        </select>
      </label>

      <label class="field">
        <span>画布比例</span>
        <select v-model="selectedAspectRatio">
          <option v-for="ratio in aspectRatioOptions" :key="ratio.value" :value="ratio.value">
            {{ ratio.label }}
          </option>
        </select>
        <small>{{ selectedAspect.description }}</small>
      </label>

      <div class="field">
        <span>图片分隔样式</span>
        <div class="segmented-control">
          <button
            v-for="option in dividerOptions"
            :key="option.value"
            type="button"
            :class="{ active: dividerStyle === option.value }"
            @click="dividerStyle = option.value"
          >
            {{ option.label }}
          </button>
        </div>
        <small>{{ dividerDescription }}</small>
      </div>

      <label class="field">
        <span>导出质量</span>
        <select v-model.number="exportScale">
          <option :value="2">高清 2x（{{ Math.round(exportSize.width / 2) }}px 基准）</option>
          <option :value="3">超清 3x（推荐）</option>
          <option :value="4">印刷级 4x</option>
        </select>
        <small>导出尺寸：{{ exportSize.width }} × {{ exportSize.height }} px，PNG 无损输出。</small>
      </label>

      <label class="field file-field">
        <span>更换图片</span>
        <input type="file" accept="image/*" @change="handleImageChange">
      </label>

      <div class="tile-status">
        <strong>当前碎片 #{{ activeTileId + 1 }} / {{ totalTiles }}</strong>
        <span>焦点：{{ activeFocusLabel }}</span>
        <button type="button" @click="resetActiveTile">重置当前碎片</button>
        <button type="button" class="export-button" :disabled="isExporting" @click="exportPuzzle">
          {{ isExporting ? '导出中…' : '导出高清 PNG' }}
        </button>
      </div>
    </section>

    <section class="workspace" aria-label="拼图预览">
      <div
        ref="boardRef"
        class="puzzle-board"
        :class="`divider-${dividerStyle}`"
        :style="{
          '--rows': selectedMode.rows,
          '--cols': selectedMode.cols,
          '--board-aspect': boardAspectRatio,
        }"
      >
        <button
          v-for="tile in tiles"
          :key="`${selectedMode.label}-${tile.id}`"
          type="button"
          class="puzzle-tile"
          :class="{ active: activeTileId === tile.id }"
          @click="activateTile(tile.id)"
        >
          <span class="tile-image" :style="tileStyle(tile)" />
          <span class="tile-number">{{ tile.id + 1 }}</span>
          <span
            v-for="corner in ['topLeft', 'topRight', 'bottomLeft', 'bottomRight'] as Corner[]"
            :key="corner"
            class="drag-dot"
            :class="corner"
            @click.stop
            @pointerdown.stop="startDrag($event, tile.id, corner)"
          />
        </button>
      </div>
    </section>
  </main>
</template>

<style scoped lang="scss">
.puzzle-page {
  min-height: 100vh;
  display: grid;
  grid-template-columns: minmax(280px, 380px) 1fr;
  gap: 32px;
  padding: 36px;
  color: #1f2937;
  background:
    radial-gradient(circle at top left, rgba(96, 165, 250, 0.22), transparent 34%),
    linear-gradient(135deg, #f8fafc 0%, #eef2ff 100%);
}

.control-panel,
.workspace {
  border: 1px solid rgba(148, 163, 184, 0.35);
  border-radius: 28px;
  background: rgba(255, 255, 255, 0.78);
  box-shadow: 0 24px 70px rgba(15, 23, 42, 0.12);
  backdrop-filter: blur(18px);
}

.control-panel {
  align-self: start;
  padding: 28px;
}

.eyebrow {
  margin: 0 0 8px;
  color: #6366f1;
  font-size: 13px;
  font-weight: 800;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  font-size: clamp(30px, 5vw, 52px);
  line-height: 1;
}

.intro {
  margin: 18px 0 28px;
  color: #64748b;
  line-height: 1.7;
}

.field {
  display: grid;
  gap: 10px;
  margin-bottom: 22px;
  font-weight: 700;

  select,
  input {
    width: 100%;
    border: 1px solid #cbd5e1;
    border-radius: 16px;
    padding: 12px 14px;
    color: #0f172a;
    background: #fff;
    font: inherit;
  }

  small {
    color: #64748b;
    font-weight: 500;
  }
}

.segmented-control {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;

  button {
    border: 0;
    border-radius: 14px;
    padding: 12px 8px;
    color: #475569;
    background: #e2e8f0;
    font-weight: 800;
    cursor: pointer;
    transition: 0.2s ease;

    &.active,
    &:hover {
      color: #fff;
      background: #4f46e5;
      box-shadow: 0 10px 24px rgba(79, 70, 229, 0.28);
    }
  }
}

.tile-status {
  display: grid;
  gap: 8px;
  border-radius: 20px;
  padding: 18px;
  background: #f8fafc;

  span {
    color: #64748b;
  }

  button {
    border: 0;
    border-radius: 14px;
    padding: 10px 12px;
    color: #fff;
    background: #0f172a;
    font-weight: 800;
    cursor: pointer;

    &:disabled {
      cursor: wait;
      opacity: 0.68;
    }
  }

  .export-button {
    background: linear-gradient(135deg, #4f46e5, #06b6d4);
  }
}

.workspace {
  min-width: 0;
  display: grid;
  place-items: center;
  padding: 32px;
}

.puzzle-board {
  width: min(100%, 980px);
  aspect-ratio: var(--board-aspect);
  display: grid;
  grid-template-columns: repeat(var(--cols), 1fr);
  grid-template-rows: repeat(var(--rows), 1fr);
  overflow: hidden;
  border-radius: 30px;
  background: #0f172a;

  &.divider-line {
    gap: 4px;
    padding: 4px;
  }

  &.divider-feather {
    gap: 0;
    padding: 0;

    .tile-image::after {
      content: '';
      position: absolute;
      inset: 0;
      pointer-events: none;
      box-shadow: inset 0 0 22px 12px rgba(248, 250, 252, 0.42);
    }
  }
}

.puzzle-tile {
  position: relative;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
  border: 0;
  padding: 0;
  background: #111827;
  cursor: pointer;
  isolation: isolate;

  &.active {
    z-index: 2;
    outline: 3px solid #f59e0b;
    outline-offset: -3px;
  }

  &:hover .drag-dot,
  &.active .drag-dot {
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
  }
}

.tile-image {
  position: absolute;
  inset: 0;
  background-repeat: no-repeat;
  transition: transform 0.28s ease, clip-path 0.18s ease;
}

.tile-number {
  position: absolute;
  left: 10px;
  top: 10px;
  border-radius: 999px;
  padding: 4px 8px;
  color: #fff;
  background: rgba(15, 23, 42, 0.58);
  font-size: 12px;
  font-weight: 900;
}

.drag-dot {
  position: absolute;
  width: 18px;
  height: 18px;
  border: 3px solid #fff;
  border-radius: 999px;
  background: #f59e0b;
  box-shadow: 0 4px 14px rgba(15, 23, 42, 0.28);
  cursor: grab;
  opacity: 0;
  transform: translate(-50%, -50%) scale(0.72);
  transition: 0.18s ease;
  z-index: 3;

  &:active {
    cursor: grabbing;
  }

  &.topLeft {
    left: 0;
    top: 0;
  }

  &.topRight {
    left: 100%;
    top: 0;
  }

  &.bottomLeft {
    left: 0;
    top: 100%;
  }

  &.bottomRight {
    left: 100%;
    top: 100%;
  }
}

@media (max-width: 900px) {
  .puzzle-page {
    grid-template-columns: 1fr;
    padding: 20px;
  }

  .workspace {
    padding: 16px;
  }
}
</style>
