<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue';

type GapStyle = 'seamless' | 'line' | 'feather';
type RatioKey = '1:1' | '4:3' | '3:4' | '16:9' | '9:16' | '3:2' | '2:3' | 'screen';
type DragMode = 'image' | 'point' | 'hLine' | 'vLine';

interface PuzzleImage {
  id: number;
  name: string;
  src: string;
  naturalWidth: number;
  naturalHeight: number;
}

interface CellState {
  imageIndex: number;
  offsetX: number;
  offsetY: number;
  zoom: number;
}

interface MeshPoint {
  x: number;
  y: number;
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
const lineWidth = ref(4);
const featherSize = ref(18);
const ratioKey = ref<RatioKey>('1:1');
const quality = ref(0.96);
const selectedIndex = ref(0);
const images = ref<PuzzleImage[]>([]);
const cellsState = ref<CellState[]>([]);
const mesh = ref<MeshPoint[][]>([]);
const dragState = reactive({
  active: false,
  mode: 'image' as DragMode,
  row: 0,
  col: 0,
  index: 0,
  startX: 0,
  startY: 0,
  startCell: { offsetX: 0, offsetY: 0, zoom: 1 },
  startMesh: [] as MeshPoint[][],
});
let imageId = 0;

const ratio = computed(() => ratios.value.find((item) => item.value === ratioKey.value)?.ratio ?? 1);
const cellCount = computed(() => rows.value * cols.value);
const selectedCell = computed(() => cellsState.value[selectedIndex.value]);
const selectedImage = computed(() => {
  const cell = selectedCell.value;
  return cell ? images.value[cell.imageIndex] : undefined;
});
const maxOutputWidth = computed(() => (ratio.value >= 1 ? 3840 : Math.round(3840 * ratio.value)));
const maxOutputHeight = computed(() => Math.round(maxOutputWidth.value / ratio.value));
const boardWrapStyle = computed(() => ({
  aspectRatio: `${ratio.value}`,
  width: `min(100%, 980px, calc((100vh - 250px) * ${ratio.value}))`,
}));
const cells = computed(() => Array.from({ length: cellCount.value }, (_, index) => {
  const row = Math.floor(index / cols.value);
  const col = index % cols.value;
  const cell = cellsState.value[index];
  return { index, row, col, cell, image: cell ? images.value[cell.imageIndex] : undefined };
}));

const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, min), max);
const cloneMesh = () => mesh.value.map((row) => row.map((point) => ({ ...point })));
const createMesh = () => Array.from({ length: rows.value + 1 }, (_, row) => Array.from({ length: cols.value + 1 }, (_, col) => ({
  x: col / cols.value,
  y: row / rows.value,
})));
const resetMesh = () => { mesh.value = createMesh(); };
const fillCells = () => {
  cellsState.value = Array.from({ length: cellCount.value }, (_, index) => {
    const previous = cellsState.value[index];
    return {
      imageIndex: images.value.length ? index % images.value.length : previous?.imageIndex ?? 0,
      offsetX: previous?.offsetX ?? 0,
      offsetY: previous?.offsetY ?? 0,
      zoom: previous?.zoom ?? 1,
    };
  });
};
const applyPreset = (preset: { rows: number; cols: number }) => {
  rows.value = preset.rows;
  cols.value = preset.cols;
};
const applyPresetByIndex = (presetIndex: number) => {
  const preset = presets[presetIndex];
  if (preset) applyPreset(preset);
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
    image.onload = () => resolve({ id: imageId++, name: file.name, src, naturalWidth: image.naturalWidth, naturalHeight: image.naturalHeight });
    image.src = src;
  })));
  images.value.push(...loaded);
  fillCells();
  selectedIndex.value = 0;
};
const clearImages = () => {
  images.value.forEach((image) => URL.revokeObjectURL(image.src));
  images.value = [];
  cellsState.value = [];
  selectedIndex.value = 0;
};
const selectCell = (index: number) => { selectedIndex.value = index; };
const getPoint = (row: number, col: number) => mesh.value[row]?.[col] ?? { x: 0, y: 0 };
const getCellPoints = (row: number, col: number) => [getPoint(row, col), getPoint(row, col + 1), getPoint(row + 1, col + 1), getPoint(row + 1, col)];
const getBounds = (points: MeshPoint[]) => {
  const xs = points.map((point) => point.x);
  const ys = points.map((point) => point.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);
  return { minX, maxX, minY, maxY, width: maxX - minX, height: maxY - minY };
};
const getCellStyle = (row: number, col: number) => {
  const points = getCellPoints(row, col);
  const bounds = getBounds(points);
  const polygon = points.map((point) => `${((point.x - bounds.minX) / bounds.width) * 100}% ${((point.y - bounds.minY) / bounds.height) * 100}%`).join(', ');
  return {
    left: `${bounds.minX * 100}%`,
    top: `${bounds.minY * 100}%`,
    width: `${bounds.width * 100}%`,
    height: `${bounds.height * 100}%`,
    clipPath: `polygon(${polygon})`,
  };
};
const getImageStyle = (cell: CellState) => ({
  transform: `translate(${cell.offsetX}%, ${cell.offsetY}%) scale(${cell.zoom})`,
});
const getFeatherStyle = () => ({ boxShadow: gapStyle.value === 'feather' ? `inset 0 0 ${featherSize.value}px ${Math.round(featherSize.value / 3)}px rgba(255,255,255,.92)` : 'none' });
const getLineStroke = computed(() => gapStyle.value === 'seamless' ? 'transparent' : lineColor.value);
const getLineWidth = computed(() => gapStyle.value === 'seamless' ? 0 : lineWidth.value);
const getPointStyle = (row: number, col: number) => {
  const point = getPoint(row, col);
  return { left: `${point.x * 100}%`, top: `${point.y * 100}%` };
};
const getHLineStyle = (row: number, col: number) => {
  const a = getPoint(row, col);
  const b = getPoint(row, col + 1);
  const length = Math.hypot((b.x - a.x) * 100, (b.y - a.y) * 100);
  const angle = Math.atan2(b.y - a.y, b.x - a.x) * 180 / Math.PI;
  return { left: `${a.x * 100}%`, top: `${a.y * 100}%`, width: `${length}%`, transform: `rotate(${angle}deg)` };
};
const getVLineStyle = (row: number, col: number) => {
  const a = getPoint(row, col);
  const b = getPoint(row + 1, col);
  const length = Math.hypot((b.x - a.x) * 100, (b.y - a.y) * 100);
  const angle = Math.atan2(b.y - a.y, b.x - a.x) * 180 / Math.PI;
  return { left: `${a.x * 100}%`, top: `${a.y * 100}%`, width: `${length}%`, transform: `rotate(${angle}deg)` };
};
const setPoint = (targetMesh: MeshPoint[][], row: number, col: number, x: number, y: number) => {
  const point = targetMesh[row]?.[col];
  if (!point || row === 0 || col === 0 || row === rows.value || col === cols.value) return;
  const minX = Math.max(0.03, (targetMesh[row]?.[col - 1]?.x ?? 0) + 0.03);
  const maxX = Math.min(0.97, (targetMesh[row]?.[col + 1]?.x ?? 1) - 0.03);
  const minY = Math.max(0.03, (targetMesh[row - 1]?.[col]?.y ?? 0) + 0.03);
  const maxY = Math.min(0.97, (targetMesh[row + 1]?.[col]?.y ?? 1) - 0.03);
  point.x = clamp(x, minX, maxX);
  point.y = clamp(y, minY, maxY);
};
const startImageDrag = (event: PointerEvent, index: number) => {
  const cell = cellsState.value[index];
  if (!cell) return;
  event.preventDefault();
  selectedIndex.value = index;
  Object.assign(dragState, { active: true, mode: 'image', index, startX: event.clientX, startY: event.clientY, startCell: { ...cell } });
  window.addEventListener('pointermove', handleDrag);
  window.addEventListener('pointerup', stopDrag, { once: true });
};
const startMeshDrag = (event: PointerEvent, mode: Exclude<DragMode, 'image'>, row: number, col: number) => {
  event.preventDefault();
  Object.assign(dragState, { active: true, mode, row, col, startX: event.clientX, startY: event.clientY, startMesh: cloneMesh() });
  window.addEventListener('pointermove', handleDrag);
  window.addEventListener('pointerup', stopDrag, { once: true });
};
const handleDrag = (event: PointerEvent) => {
  if (!dragState.active) return;
  const board = boardRef.value;
  if (!board) return;
  const rect = board.getBoundingClientRect();
  const dxPx = event.clientX - dragState.startX;
  const dyPx = event.clientY - dragState.startY;
  if (dragState.mode === 'image') {
    const cell = cellsState.value[dragState.index];
    if (!cell) return;
    cell.offsetX = clamp(dragState.startCell.offsetX + (dxPx / rect.width) * 100, -60, 60);
    cell.offsetY = clamp(dragState.startCell.offsetY + (dyPx / rect.height) * 100, -60, 60);
    return;
  }

  const nextMesh = dragState.startMesh.map((row) => row.map((point) => ({ ...point })));
  const dx = dxPx / rect.width;
  const dy = dyPx / rect.height;
  if (dragState.mode === 'point') {
    const startPoint = dragState.startMesh[dragState.row]?.[dragState.col];
    if (startPoint) setPoint(nextMesh, dragState.row, dragState.col, startPoint.x + dx, startPoint.y + dy);
  }
  if (dragState.mode === 'hLine') {
    [dragState.col, dragState.col + 1].forEach((col) => {
      const startPoint = dragState.startMesh[dragState.row]?.[col];
      if (startPoint) setPoint(nextMesh, dragState.row, col, startPoint.x, startPoint.y + dy);
    });
  }
  if (dragState.mode === 'vLine') {
    [dragState.row, dragState.row + 1].forEach((row) => {
      const startPoint = dragState.startMesh[row]?.[dragState.col];
      if (startPoint) setPoint(nextMesh, row, dragState.col, startPoint.x + dx, startPoint.y);
    });
  }
  mesh.value = nextMesh;
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
const drawCoverImage = (ctx: CanvasRenderingContext2D, image: HTMLImageElement, imageData: PuzzleImage, cell: CellState, x: number, y: number, width: number, height: number) => {
  const zoom = cell.zoom;
  const baseScale = Math.max(width / imageData.naturalWidth, height / imageData.naturalHeight) * zoom;
  const drawWidth = imageData.naturalWidth * baseScale;
  const drawHeight = imageData.naturalHeight * baseScale;
  const drawX = x + (width - drawWidth) / 2 + (cell.offsetX / 100) * width;
  const drawY = y + (height - drawHeight) / 2 + (cell.offsetY / 100) * height;
  ctx.drawImage(image, drawX, drawY, drawWidth, drawHeight);
};
const exportPuzzle = async () => {
  if (!images.value.length) return;
  const canvas = document.createElement('canvas');
  canvas.width = maxOutputWidth.value;
  canvas.height = maxOutputHeight.value;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  ctx.fillStyle = gapStyle.value === 'line' ? lineColor.value : '#fff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  for (const cellInfo of cells.value) {
    if (!cellInfo.cell || !cellInfo.image) continue;
    const image = await loadImageElement(cellInfo.image.src);
    const points = getCellPoints(cellInfo.row, cellInfo.col).map((point) => ({ x: point.x * canvas.width, y: point.y * canvas.height }));
    const bounds = getBounds(points);
    ctx.save();
    ctx.beginPath();
    points.forEach((point, index) => index ? ctx.lineTo(point.x, point.y) : ctx.moveTo(point.x, point.y));
    ctx.closePath();
    ctx.clip();
    drawCoverImage(ctx, image, cellInfo.image, cellInfo.cell, bounds.minX, bounds.minY, bounds.width, bounds.height);
    if (gapStyle.value === 'feather') {
      const feather = featherSize.value * (canvas.width / (boardRef.value?.clientWidth || 960));
      ctx.shadowColor = 'rgba(255,255,255,.55)';
      ctx.shadowBlur = feather;
      ctx.strokeStyle = 'rgba(255,255,255,.55)';
      ctx.lineWidth = feather;
      ctx.stroke();
    }
    ctx.restore();
  }

  if (gapStyle.value === 'line') {
    ctx.strokeStyle = lineColor.value;
    ctx.lineWidth = Math.max(1, lineWidth.value * (canvas.width / (boardRef.value?.clientWidth || 960)));
    ctx.lineCap = 'round';
    for (let row = 1; row < rows.value; row += 1) {
      for (let col = 0; col < cols.value; col += 1) {
        const a = getPoint(row, col);
        const b = getPoint(row, col + 1);
        ctx.beginPath();
        ctx.moveTo(a.x * canvas.width, a.y * canvas.height);
        ctx.lineTo(b.x * canvas.width, b.y * canvas.height);
        ctx.stroke();
      }
    }
    for (let col = 1; col < cols.value; col += 1) {
      for (let row = 0; row < rows.value; row += 1) {
        const a = getPoint(row, col);
        const b = getPoint(row + 1, col);
        ctx.beginPath();
        ctx.moveTo(a.x * canvas.width, a.y * canvas.height);
        ctx.lineTo(b.x * canvas.width, b.y * canvas.height);
        ctx.stroke();
      }
    }
  }

  const link = document.createElement('a');
  link.download = `puzzle-${rows.value}x${cols.value}.jpg`;
  link.href = canvas.toDataURL('image/jpeg', quality.value);
  link.click();
};

watch([rows, cols], () => {
  rows.value = clamp(rows.value, 1, 10);
  cols.value = clamp(cols.value, 1, 10);
  resetMesh();
  fillCells();
  selectedIndex.value = clamp(selectedIndex.value, 0, Math.max(0, cellCount.value - 1));
}, { immediate: true });
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
        <p>图片会自动铺满画布；拖动图片只改变可视区域，拖动单个点或单条线段只改变对应分界，不会拉伸图片。</p>
      </div>
      <div class="control-grid">
        <label>拼图模式<select @change="applyPresetByIndex(Number(($event.target as HTMLSelectElement).value))"><option v-for="(preset, index) in presets" :key="preset.label" :value="index">{{ preset.label }}</option></select></label>
        <label>行数<input v-model.number="rows" min="1" max="10" type="number"></label>
        <label>列数<input v-model.number="cols" min="1" max="10" type="number"></label>
        <label>画布比例<select v-model="ratioKey"><option v-for="item in ratios" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
        <label>分隔样式<select v-model="gapStyle"><option value="seamless">无缝</option><option value="line">线条</option><option value="feather">图片边缘羽化过渡</option></select></label>
        <label v-if="gapStyle !== 'seamless'">分隔宽度<input v-model.number="lineWidth" min="1" max="18" type="range"></label>
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
        <div class="board-wrap" :style="boardWrapStyle">
          <div ref="boardRef" class="puzzle-board" :style="{ background: gapStyle === 'line' ? lineColor : '#fff' }">
            <article v-for="cellInfo in cells" :key="cellInfo.index" class="puzzle-cell" :class="{ selected: selectedIndex === cellInfo.index }" :style="getCellStyle(cellInfo.row, cellInfo.col)" @click="selectCell(cellInfo.index)">
              <template v-if="cellInfo.cell && cellInfo.image">
                <img class="cell-image" :src="cellInfo.image.src" :alt="cellInfo.image.name" :style="getImageStyle(cellInfo.cell)" draggable="false" @pointerdown="startImageDrag($event, cellInfo.index)">
                <div class="feather-mask" :style="getFeatherStyle()" />
              </template>
              <button v-else class="empty-cell" type="button" @click.stop="fileInputRef?.click()">+ 添加图片</button>
            </article>
            <template v-if="images.length">
              <button v-for="row in rows - 1" :key="`point-row-${row}`" v-show="false" />
              <template v-for="row in rows - 1" :key="`h-row-${row}`">
                <button v-for="col in cols" :key="`h-${row}-${col}`" class="mesh-line mesh-line--h" :style="getHLineStyle(row, col - 1)" type="button" @pointerdown="startMeshDrag($event, 'hLine', row, col - 1)" />
              </template>
              <template v-for="col in cols - 1" :key="`v-col-${col}`">
                <button v-for="row in rows" :key="`v-${row}-${col}`" class="mesh-line mesh-line--v" :style="getVLineStyle(row - 1, col)" type="button" @pointerdown="startMeshDrag($event, 'vLine', row - 1, col)" />
              </template>
              <template v-for="row in rows - 1" :key="`p-row-${row}`">
                <button v-for="col in cols - 1" :key="`p-${row}-${col}`" class="mesh-point" :style="getPointStyle(row, col)" type="button" @pointerdown="startMeshDrag($event, 'point', row, col)" />
              </template>
            </template>
          </div>
        </div>
      </div>
      <aside class="side panel">
        <h2>图片队列</h2>
        <p class="hint">导出尺寸：{{ maxOutputWidth }} × {{ maxOutputHeight }}。图片少于格子时循环铺满；图片多于格子时使用前 {{ cellCount }} 个位置。</p>
        <div class="thumbs">
          <button v-for="image in images" :key="image.id" class="thumb" :class="{ active: selectedImage?.id === image.id }" type="button"><img :src="image.src" :alt="image.name"></button>
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
.board-wrap { position: relative; }
.puzzle-board { position: absolute; inset: 0; overflow: hidden; border-radius: 18px; box-shadow: 0 16px 50px rgba(15, 23, 42, .18); }
.puzzle-cell { position: absolute; overflow: hidden; background: #e2e8f0; isolation: isolate; }
.puzzle-cell.selected { outline: 2px solid #38bdf8; outline-offset: -2px; z-index: 2; }
.cell-image { position: absolute; inset: 0; width: 100%; height: 100%; object-fit: cover; user-select: none; cursor: grab; transform-origin: center; }
.cell-image:active { cursor: grabbing; }
.feather-mask { position: absolute; inset: 0; pointer-events: none; }
.mesh-line, .mesh-point { position: absolute; z-index: 5; border: 0; padding: 0; background: #38bdf8; box-shadow: 0 0 0 2px #fff, 0 6px 16px rgba(2, 132, 199, .32); }
.mesh-line { height: 3px; min-height: 3px; transform-origin: 0 50%; }
.mesh-line--h { cursor: ns-resize; }
.mesh-line--v { cursor: ew-resize; }
.mesh-point { width: 14px; min-width: 14px; height: 14px; min-height: 14px; border-radius: 50%; transform: translate(-50%, -50%); cursor: move; }
.empty-cell { width: 100%; height: 100%; min-height: 90px; color: #64748b; background: repeating-linear-gradient(45deg, #f8fafc 0 12px, #e2e8f0 12px 24px); }
.side { padding: 22px; }
.side h2 { margin: 0 0 10px; }
.thumbs { display: grid; grid-template-columns: repeat(3, 1fr); gap: 10px; margin: 18px 0; }
.thumb { position: relative; overflow: hidden; aspect-ratio: 1; padding: 0; border: 3px solid transparent; background: #e2e8f0; }
.thumb.active { border-color: #38bdf8; }
.thumb img { width: 100%; height: 100%; object-fit: cover; }
.meta { display: grid; gap: 8px; padding: 14px; border-radius: 16px; color: #475569; background: #f8fafc; }
@media (max-width: 1180px) { .toolbar, .workspace { grid-template-columns: 1fr; } .control-grid { grid-template-columns: repeat(2, minmax(130px, 1fr)); } }
@media (max-width: 640px) { .puzzle-page { padding: 14px; } .control-grid { grid-template-columns: 1fr; } }
</style>
