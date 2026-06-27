<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue';

type GapStyle = 'seamless' | 'line' | 'feather';
type RatioKey = '1:1' | '4:3' | '3:4' | '16:9' | '9:16' | '3:2' | '2:3' | 'screen';
type DragMode = 'move' | 'tl' | 'tr' | 'bl' | 'br' | 'top' | 'right' | 'bottom' | 'left';

interface PuzzleImage {
  id: number;
  name: string;
  src: string;
  naturalWidth: number;
  naturalHeight: number;
  crop: { x: number; y: number; width: number; height: number };
}

interface RatioOption {
  label: string;
  value: RatioKey;
  ratio: number;
}

const presets = [
  { label: '2 × 2', rows: 2, cols: 2 },
  { label: '3 × 3', rows: 3, cols: 3 },
  { label: '4 × 4', rows: 4, cols: 4 },
  { label: '5 × 6', rows: 5, cols: 6 },
  { label: '6 × 5', rows: 6, cols: 5 },
];
const ratios = ref<RatioOption[]>([
  { label: '正方形 1:1', value: '1:1', ratio: 1 },
  { label: '横版 4:3', value: '4:3', ratio: 4 / 3 },
  { label: '竖版 3:4', value: '3:4', ratio: 3 / 4 },
  { label: '宽屏 16:9', value: '16:9', ratio: 16 / 9 },
  { label: '手机 9:16', value: '9:16', ratio: 9 / 16 },
  { label: '相机 3:2', value: '3:2', ratio: 3 / 2 },
  { label: '竖版 2:3', value: '2:3', ratio: 2 / 3 },
  { label: '当前显示器', value: 'screen', ratio: 16 / 9 },
]);

const fileInputRef = ref<HTMLInputElement>();
const boardRef = ref<HTMLElement>();
const rows = ref(2);
const cols = ref(2);
const gapStyle = ref<GapStyle>('line');
const lineColor = ref('#ffffff');
const lineWidth = ref(8);
const featherSize = ref(18);
const ratioKey = ref<RatioKey>('1:1');
const quality = ref(0.96);
const selectedIndex = ref(0);
const images = ref<PuzzleImage[]>([]);
const dragState = reactive({ active: false, mode: 'move' as DragMode, index: 0, startX: 0, startY: 0, startCrop: { x: 0, y: 0, width: 1, height: 1 } });
let imageId = 0;

const ratio = computed(() => ratios.value.find((item) => item.value === ratioKey.value)?.ratio ?? 1);
const cellCount = computed(() => rows.value * cols.value);
const selectedImage = computed(() => images.value[selectedIndex.value]);
const cells = computed(() => Array.from({ length: cellCount.value }, (_, index) => ({ index, image: images.value[index] })));
const applyPresetByIndex = (presetIndex: number) => {
  const preset = presets[presetIndex];
  if (preset) applyPreset(preset);
};
const maxOutputWidth = computed(() => (ratio.value >= 1 ? 3840 : Math.round(3840 * ratio.value)));
const maxOutputHeight = computed(() => Math.round(maxOutputWidth.value / ratio.value));
const boardStyle = computed(() => ({
  aspectRatio: `${ratio.value}`,
  gridTemplateColumns: `repeat(${cols.value}, minmax(0, 1fr))`,
  gridTemplateRows: `repeat(${rows.value}, minmax(0, 1fr))`,
  gap: gapStyle.value === 'seamless' ? '0px' : `${lineWidth.value}px`,
  background: gapStyle.value === 'line' ? lineColor.value : 'transparent',
}));

const getImageStyle = (image: PuzzleImage) => ({
  width: `${100 / image.crop.width}%`,
  height: `${100 / image.crop.height}%`,
  left: `${(-image.crop.x / image.crop.width) * 100}%`,
  top: `${(-image.crop.y / image.crop.height) * 100}%`,
});
const getFeatherStyle = () => ({ boxShadow: gapStyle.value === 'feather' ? `inset 0 0 ${featherSize.value}px ${Math.round(featherSize.value / 3)}px rgba(255,255,255,.92)` : 'none' });
const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, min), max);
const normalizeCrop = (crop: PuzzleImage['crop']) => {
  const minSize = 0.12;
  crop.width = clamp(crop.width, minSize, 1);
  crop.height = clamp(crop.height, minSize, 1);
  crop.x = clamp(crop.x, 0, 1 - crop.width);
  crop.y = clamp(crop.y, 0, 1 - crop.height);
};
const applyPreset = (preset: { rows: number; cols: number }) => {
  rows.value = preset.rows;
  cols.value = preset.cols;
  selectedIndex.value = Math.min(selectedIndex.value, cellCount.value - 1);
};
const updateScreenRatio = () => {
  const screenRatio = window.screen.width / window.screen.height;
  ratios.value = ratios.value.map((item) => (item.value === 'screen' ? { ...item, label: `当前显示器 ${window.screen.width}:${window.screen.height}`, ratio: screenRatio } : item));
};
const addFiles = async (fileList: FileList | null) => {
  if (!fileList) return;
  const files = Array.from(fileList).filter((file) => file.type.startsWith('image/'));
  const loaded = await Promise.all(files.map((file) => new Promise<PuzzleImage>((resolve) => {
    const src = URL.createObjectURL(file);
    const image = new Image();
    image.onload = () => resolve({ id: imageId++, name: file.name, src, naturalWidth: image.naturalWidth, naturalHeight: image.naturalHeight, crop: { x: 0, y: 0, width: 1, height: 1 } });
    image.src = src;
  })));
  images.value.push(...loaded);
  if (images.value.length === loaded.length) selectedIndex.value = 0;
};
const clearImages = () => {
  images.value.forEach((image) => URL.revokeObjectURL(image.src));
  images.value = [];
  selectedIndex.value = 0;
};
const selectCell = (index: number) => { selectedIndex.value = index; };
const startDrag = (event: PointerEvent, index: number, mode: DragMode) => {
  const image = images.value[index];
  if (!image) return;
  event.preventDefault();
  selectedIndex.value = index;
  Object.assign(dragState, { active: true, mode, index, startX: event.clientX, startY: event.clientY, startCrop: { ...image.crop } });
  window.addEventListener('pointermove', handleDrag);
  window.addEventListener('pointerup', stopDrag, { once: true });
};
const handleDrag = (event: PointerEvent) => {
  if (!dragState.active) return;
  const image = images.value[dragState.index];
  const board = boardRef.value;
  if (!image || !board) return;
  const rect = board.getBoundingClientRect();
  const dx = (event.clientX - dragState.startX) / (rect.width / cols.value);
  const dy = (event.clientY - dragState.startY) / (rect.height / rows.value);
  const next = { ...dragState.startCrop };
  if (dragState.mode === 'move') {
    next.x -= dx * next.width;
    next.y -= dy * next.height;
  } else {
    if (dragState.mode.includes('l') || dragState.mode === 'left') { next.x += dx; next.width -= dx; }
    if (dragState.mode.includes('r') || dragState.mode === 'right') next.width += dx;
    if (dragState.mode.includes('t') || dragState.mode === 'top') { next.y += dy; next.height -= dy; }
    if (dragState.mode.includes('b') || dragState.mode === 'bottom') next.height += dy;
  }
  normalizeCrop(next);
  image.crop = next;
};
const stopDrag = () => {
  dragState.active = false;
  window.removeEventListener('pointermove', handleDrag);
};
const loadImageElement = (src: string) => new Promise<HTMLImageElement>((resolve) => {
  const image = new Image();
  image.onload = () => resolve(image);
  image.src = src;
});
const drawImage = async (ctx: CanvasRenderingContext2D, imageData: PuzzleImage, x: number, y: number, width: number, height: number) => {
  const image = await loadImageElement(imageData.src);
  const crop = imageData.crop;
  ctx.drawImage(image, crop.x * image.naturalWidth, crop.y * image.naturalHeight, crop.width * image.naturalWidth, crop.height * image.naturalHeight, x, y, width, height);
};
const exportPuzzle = async () => {
  const canvas = document.createElement('canvas');
  canvas.width = maxOutputWidth.value;
  canvas.height = maxOutputHeight.value;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  const scale = canvas.width / (boardRef.value?.clientWidth || 960);
  const gap = gapStyle.value === 'seamless' ? 0 : Math.round(lineWidth.value * scale);
  const cellWidth = (canvas.width - gap * (cols.value - 1)) / cols.value;
  const cellHeight = (canvas.height - gap * (rows.value - 1)) / rows.value;
  ctx.fillStyle = gapStyle.value === 'line' ? lineColor.value : '#fff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);
  for (let index = 0; index < cellCount.value; index += 1) {
    const image = images.value[index];
    if (!image) continue;
    const col = index % cols.value;
    const row = Math.floor(index / cols.value);
    const x = col * (cellWidth + gap);
    const y = row * (cellHeight + gap);
    await drawImage(ctx, image, x, y, cellWidth, cellHeight);
    if (gapStyle.value === 'feather') {
      const feather = Math.round(featherSize.value * scale);
      const gradient = ctx.createRadialGradient(x + cellWidth / 2, y + cellHeight / 2, Math.max(1, Math.min(cellWidth, cellHeight) / 2 - feather), x + cellWidth / 2, y + cellHeight / 2, Math.max(cellWidth, cellHeight) / 1.35);
      gradient.addColorStop(0, 'rgba(255,255,255,0)');
      gradient.addColorStop(1, 'rgba(255,255,255,.32)');
      ctx.fillStyle = gradient;
      ctx.fillRect(x, y, cellWidth, cellHeight);
    }
  }
  const link = document.createElement('a');
  link.download = `puzzle-${rows.value}x${cols.value}.jpg`;
  link.href = canvas.toDataURL('image/jpeg', quality.value);
  link.click();
};
watch([rows, cols], () => { selectedIndex.value = clamp(selectedIndex.value, 0, Math.max(0, cellCount.value - 1)); });
onMounted(() => { updateScreenRatio(); window.addEventListener('resize', updateScreenRatio); });
onUnmounted(() => {
  images.value.forEach((image) => URL.revokeObjectURL(image.src));
  window.removeEventListener('resize', updateScreenRatio);
  window.removeEventListener('pointermove', handleDrag);
});
</script>

<template>
  <main class="puzzle-page">
    <section class="toolbar panel">
      <div class="brand">
        <span class="brand__badge">Puzzle Lab</span>
        <h1>多图片拼图工作台</h1>
        <p>上传多张图片后，点击单元格选中图片，拖动图片、四角点或边线调整当前区域展示范围。</p>
      </div>
      <div class="control-grid">
        <label>拼图模式<select @change="applyPresetByIndex(Number(($event.target as HTMLSelectElement).value))"><option v-for="(preset, index) in presets" :key="preset.label" :value="index">{{ preset.label }}</option></select></label>
        <label>行数<input v-model.number="rows" min="1" max="10" type="number"></label>
        <label>列数<input v-model.number="cols" min="1" max="10" type="number"></label>
        <label>画布比例<select v-model="ratioKey"><option v-for="item in ratios" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
        <label>分隔样式<select v-model="gapStyle"><option value="seamless">无缝</option><option value="line">线条</option><option value="feather">图片边缘羽化过渡</option></select></label>
        <label v-if="gapStyle !== 'seamless'">分隔宽度<input v-model.number="lineWidth" min="0" max="36" type="range"></label>
        <label v-if="gapStyle === 'line'">线条颜色<input v-model="lineColor" type="color"></label>
        <label v-if="gapStyle === 'feather'">羽化强度<input v-model.number="featherSize" min="4" max="48" type="range"></label>
        <label>导出质量 {{ Math.round(quality * 100) }}%<input v-model.number="quality" min="0.82" max="1" step="0.01" type="range"></label>
      </div>
      <div class="actions">
        <input ref="fileInputRef" accept="image/*" multiple type="file" hidden @change="addFiles(($event.target as HTMLInputElement).files)">
        <button type="button" @click="fileInputRef?.click()">选择图片</button>
        <button type="button" :disabled="!images.length" @click="exportPuzzle">导出高清图片</button>
        <button class="ghost" type="button" :disabled="!images.length" @click="clearImages">清空</button>
      </div>
    </section>
    <section class="workspace">
      <div class="canvas-shell panel">
        <div ref="boardRef" class="puzzle-board" :style="boardStyle">
          <article v-for="cell in cells" :key="cell.index" class="puzzle-cell" :class="{ selected: selectedIndex === cell.index }" @click="selectCell(cell.index)">
            <template v-if="cell.image">
              <img class="cell-image" :src="cell.image.src" :alt="cell.image.name" :style="getImageStyle(cell.image)" draggable="false" @pointerdown="startDrag($event, cell.index, 'move')">
              <div class="feather-mask" :style="getFeatherStyle()" />
              <div class="crop-lines">
                <button class="edge edge--top" @pointerdown.stop="startDrag($event, cell.index, 'top')" />
                <button class="edge edge--right" @pointerdown.stop="startDrag($event, cell.index, 'right')" />
                <button class="edge edge--bottom" @pointerdown.stop="startDrag($event, cell.index, 'bottom')" />
                <button class="edge edge--left" @pointerdown.stop="startDrag($event, cell.index, 'left')" />
                <button class="handle handle--tl" @pointerdown.stop="startDrag($event, cell.index, 'tl')" />
                <button class="handle handle--tr" @pointerdown.stop="startDrag($event, cell.index, 'tr')" />
                <button class="handle handle--bl" @pointerdown.stop="startDrag($event, cell.index, 'bl')" />
                <button class="handle handle--br" @pointerdown.stop="startDrag($event, cell.index, 'br')" />
              </div>
            </template>
            <button v-else class="empty-cell" type="button" @click.stop="fileInputRef?.click()">+ 添加图片</button>
          </article>
        </div>
      </div>
      <aside class="side panel">
        <h2>图片队列</h2>
        <p class="hint">导出尺寸：{{ maxOutputWidth }} × {{ maxOutputHeight }}，空位不会导出；超过格子时仅使用前 {{ cellCount }} 张。</p>
        <div class="thumbs">
          <button v-for="(image, index) in images" :key="image.id" class="thumb" :class="{ active: selectedIndex === index }" type="button" @click="selectCell(index)"><img :src="image.src" :alt="image.name"><span>{{ index + 1 }}</span></button>
        </div>
        <div v-if="selectedImage" class="meta"><strong>当前图片</strong><span>{{ selectedImage.name }}</span><span>{{ selectedImage.naturalWidth }} × {{ selectedImage.naturalHeight }}</span></div>
      </aside>
    </section>
  </main>
</template>

<style scoped lang="scss">
.puzzle-page { min-height: 100vh; padding: 28px; color: #172033; background: radial-gradient(circle at top left, #e0f2fe, transparent 34%), linear-gradient(135deg, #f8fafc, #e2e8f0); }
.panel { border: 1px solid rgba(148, 163, 184, .28); border-radius: 24px; background: rgba(255, 255, 255, .82); box-shadow: 0 20px 60px rgba(15, 23, 42, .12); backdrop-filter: blur(18px); }
.toolbar { display: grid; grid-template-columns: minmax(240px, .8fr) 1.6fr auto; gap: 22px; align-items: end; padding: 24px; }
.brand h1 { margin: 10px 0 8px; font-size: clamp(28px, 4vw, 46px); }
.brand p, .hint { margin: 0; color: #64748b; line-height: 1.7; }
.brand__badge { display: inline-flex; padding: 6px 12px; border-radius: 999px; color: #0369a1; background: #e0f2fe; font-weight: 700; }
.control-grid { display: grid; grid-template-columns: repeat(4, minmax(120px, 1fr)); gap: 14px; }
label { display: grid; gap: 7px; font-size: 13px; font-weight: 700; color: #475569; }
select, input, button { min-height: 40px; border: 1px solid #cbd5e1; border-radius: 12px; padding: 0 12px; font: inherit; }
button { cursor: pointer; border: 0; color: #fff; background: linear-gradient(135deg, #2563eb, #7c3aed); font-weight: 800; }
button:disabled { cursor: not-allowed; opacity: .5; }
.ghost { color: #334155; background: #e2e8f0; }
.actions { display: grid; gap: 10px; }
.workspace { display: grid; grid-template-columns: minmax(0, 1fr) 300px; gap: 22px; margin-top: 22px; }
.canvas-shell { display: grid; place-items: center; min-height: 560px; padding: 26px; }
.puzzle-board { display: grid; width: min(100%, 980px); max-height: calc(100vh - 250px); overflow: hidden; border-radius: 18px; box-shadow: 0 16px 50px rgba(15, 23, 42, .18); }
.puzzle-cell { position: relative; overflow: hidden; min-width: 0; min-height: 0; background: #e2e8f0; isolation: isolate; }
.puzzle-cell.selected { outline: 3px solid #38bdf8; outline-offset: -3px; z-index: 2; }
.cell-image { position: absolute; object-fit: fill; user-select: none; cursor: grab; }
.cell-image:active { cursor: grabbing; }
.feather-mask, .crop-lines { position: absolute; inset: 0; pointer-events: none; }
.edge, .handle { position: absolute; pointer-events: auto; border: 0; padding: 0; background: #38bdf8; box-shadow: 0 0 0 2px #fff, 0 6px 18px rgba(2, 132, 199, .35); }
.edge--top, .edge--bottom { left: 20px; right: 20px; height: 3px; cursor: ns-resize; }
.edge--top { top: 16px; }
.edge--bottom { bottom: 16px; }
.edge--left, .edge--right { top: 20px; bottom: 20px; width: 3px; cursor: ew-resize; }
.edge--left { left: 16px; }
.edge--right { right: 16px; }
.handle { width: 14px; height: 14px; border-radius: 999px; }
.handle--tl { top: 10px; left: 10px; cursor: nwse-resize; }
.handle--tr { top: 10px; right: 10px; cursor: nesw-resize; }
.handle--bl { bottom: 10px; left: 10px; cursor: nesw-resize; }
.handle--br { right: 10px; bottom: 10px; cursor: nwse-resize; }
.empty-cell { width: 100%; height: 100%; min-height: 90px; color: #64748b; background: repeating-linear-gradient(45deg, #f8fafc 0 12px, #e2e8f0 12px 24px); }
.side { padding: 22px; }
.side h2 { margin: 0 0 10px; }
.thumbs { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin: 18px 0; }
.thumb { position: relative; overflow: hidden; aspect-ratio: 1; padding: 0; border: 3px solid transparent; background: #e2e8f0; }
.thumb.active { border-color: #38bdf8; }
.thumb img { width: 100%; height: 100%; object-fit: cover; }
.thumb span { position: absolute; right: 4px; bottom: 4px; display: grid; place-items: center; width: 22px; height: 22px; border-radius: 999px; background: rgba(15, 23, 42, .76); }
.meta { display: grid; gap: 8px; padding: 14px; border-radius: 16px; color: #475569; background: #f8fafc; }
@media (max-width: 1180px) { .toolbar, .workspace { grid-template-columns: 1fr; } .control-grid { grid-template-columns: repeat(2, minmax(130px, 1fr)); } }
@media (max-width: 640px) { .puzzle-page { padding: 14px; } .control-grid { grid-template-columns: 1fr; } }
</style>
