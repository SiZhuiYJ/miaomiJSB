import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue';
import type { CSSProperties } from 'vue';

export type GapStyle = 'seamless' | 'line' | 'feather';
export type RatioKey =
  | '1:1'
  | '5:4' | '4:5'
  | '4:3' | '3:4'
  | '3:2' | '2:3'
  | '16:10' | '10:16'
  | '16:9' | '9:16'
  | '21:9' | '9:21'
  | 'a4' | 'a4-vertical'
  | 'bilibili-cover'
  | 'wechat-moments'
  | 'xiaohongshu'
  | 'wechat-article'
  | 'screen';
export type DragMode = 'image' | 'pinch' | 'segment' | 'endpoint';
export type SegmentKind = 'horizontal' | 'vertical';
export type EndpointKey = 'start' | 'end';
export type ExportFormat = 'jpeg' | 'png' | 'webp';

export interface PuzzleImage {
  id: number;
  name: string;
  src: string;
  naturalWidth: number;
  naturalHeight: number;
}

export interface CellState {
  imageIndex: number;
  offsetX: number;
  offsetY: number;
  zoom: number;
}

export interface MeshPoint {
  x: number;
  y: number;
}

export interface PointerPosition {
  x: number;
  y: number;
}

export interface ImageQueueDragState {
  active: boolean;
  imageId: number | null;
  startIndex: number;
  overIndex: number;
  startX: number;
  startY: number;
  currentX: number;
  currentY: number;
  hasMoved: boolean;
}

export type QueueUploadMode = 'insert' | 'replace';

export interface Bounds {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
  width: number;
  height: number;
}

export interface RatioOption {
  label: string;
  value: RatioKey;
  ratio: number;
}

export interface ExportFormatOption {
  label: string;
  value: ExportFormat;
  mimeType: string;
  extension: string;
  supportsQuality: boolean;
}

export interface SeamSegment {
  id: string;
  kind: SegmentKind;
  row: number;
  col: number;
  start: MeshPoint;
  end: MeshPoint;
}

export interface SeamSnapshot {
  horizontal: SeamSegment[][];
  vertical: SeamSegment[][];
}

export interface SeamConnection {
  kind: SegmentKind;
  row: number;
  col: number;
  endpoint: EndpointKey;
}

export interface SeamHandle {
  id: string;
  row: number;
  col: number;
  point: MeshPoint;
  connections: SeamConnection[];
}

export interface RenderCell {
  row: number;
  col: number;
  cell: CellState;
  imageData: PuzzleImage;
  image: HTMLImageElement;
  points: MeshPoint[];
  bounds: Bounds;
}

export interface HardMaskEdges {
  top?: number;
  right?: number;
  bottom?: number;
  left?: number;
}

export const usePuzzleEditor = () => {
  const presets = [
    { label: '2 × 2', rows: 2, cols: 2 },
    { label: '3 × 3', rows: 3, cols: 3 },
    { label: '4 × 4', rows: 4, cols: 4 },
    { label: '5 × 6', rows: 5, cols: 6 },
    { label: '6 × 5', rows: 6, cols: 5 },
  ];
  const ratios = ref<RatioOption[]>([
    { label: '◆ 正方形 1:1', value: '1:1', ratio: 1 },
    { label: '▢ 大画幅 5:4（传统相框）', value: '5:4', ratio: 5 / 4 },
    { label: '▢ 大画幅 4:5（Instagram竖方）', value: '4:5', ratio: 4 / 5 },
    { label: '🖥️ 电脑/显示器 16:10（MacBook Pro）', value: '16:10', ratio: 16 / 10 },
    { label: '🖥️ 电脑/显示器 10:16（竖屏MacBook）', value: '10:16', ratio: 10 / 16 },
    { label: '🖥️ 电脑 16:9（主流宽屏）', value: '16:9', ratio: 16 / 9 },
    { label: '🖥️ 电脑 9:16（竖屏显示器）', value: '9:16', ratio: 9 / 16 },
    { label: '🖥️ 超宽屏 21:9（带鱼屏）', value: '21:9', ratio: 21 / 9 },
    { label: '🖥️ 超宽屏 9:21（竖带鱼屏）', value: '9:21', ratio: 9 / 21 },
    { label: '📱 平板 4:3（iPad横屏）', value: '4:3', ratio: 4 / 3 },
    { label: '📱 平板 3:4（iPad竖屏）', value: '3:4', ratio: 3 / 4 },
    { label: '📷 相机 3:2（全画幅/APS-C横）', value: '3:2', ratio: 3 / 2 },
    { label: '📷 相机 2:3（全画幅/APS-C竖）', value: '2:3', ratio: 2 / 3 },
    { label: '📄 A4 横向 1.414:1', value: 'a4', ratio: Math.sqrt(2) },
    { label: '📄 A4 纵向 1:1.414', value: 'a4-vertical', ratio: 1 / Math.sqrt(2) },
    { label: '🎬 B站封面 16:10', value: 'bilibili-cover', ratio: 16 / 10 },
    { label: '💬 微信朋友圈 3:4（推荐）', value: 'wechat-moments', ratio: 3 / 4 },
    { label: '📕 小红书封面 3:4（单图推荐）', value: 'xiaohongshu', ratio: 3 / 4 },
    { label: '📰 微信公众号封面 2.35:1', value: 'wechat-article', ratio: 2.35 },
    { label: '💻 当前显示器', value: 'screen', ratio: 16 / 9 },
  ]);
  const defaultExportFormat: ExportFormatOption = { label: 'JPEG (.jpg)', value: 'jpeg', mimeType: 'image/jpeg', extension: 'jpg', supportsQuality: true };
  const exportFormats: ExportFormatOption[] = [
    defaultExportFormat,
    { label: 'PNG (.png)', value: 'png', mimeType: 'image/png', extension: 'png', supportsQuality: false },
    { label: 'WebP (.webp)', value: 'webp', mimeType: 'image/webp', extension: 'webp', supportsQuality: true },
  ];

  const MIN_ENDPOINT_GAP = 0.018;
  const FALLBACK_BOARD_WIDTH = 960;
  const MIN_IMAGE_ZOOM = 0.5;
  const MAX_IMAGE_ZOOM = 3;
  const IMAGE_ZOOM_STEP = 0.08;
  const IMAGE_OFFSET_LIMIT = 150;
  const QUEUE_DRAG_THRESHOLD = 6;

  const fileInputRef = ref<HTMLInputElement>();
  const boardRef = ref<HTMLElement>();
  const pendingCellIndex = ref<number | null>(null);
  const rows = ref(2);
  const cols = ref(2);
  const gapStyle = ref<GapStyle>('line');
  const lineColor = ref('#ffffff');
  const lineWidth = ref(4);
  const featherSize = ref(18);
  const ratioKey = ref<RatioKey>('1:1');
  const exportFormat = ref<ExportFormat>('jpeg');
  const quality = ref(0.96);
  const selectedIndex = ref(0);
  const images = ref<PuzzleImage[]>([]);
  const imageUploadFeedback = ref('');
  const cellsState = ref<CellState[]>([]);
  const horizontalSegments = ref<SeamSegment[][]>([]);
  const verticalSegments = ref<SeamSegment[][]>([]);
  const dragState = reactive({
    active: false,
    mode: 'image' as DragMode,
    kind: 'horizontal' as SegmentKind,
    row: 0,
    col: 0,
    endpoint: 'start' as EndpointKey,
    index: 0,
    startX: 0,
    startY: 0,
    startCell: { imageIndex: 0, offsetX: 0, offsetY: 0, zoom: 1 },
    startSeams: { horizontal: [], vertical: [] } as SeamSnapshot,
    startDistance: 0,
    startRectWidth: 1,
    startRectHeight: 1,
  });
  const imageQueueDragState = reactive<ImageQueueDragState>({
    active: false,
    imageId: null,
    startIndex: -1,
    overIndex: -1,
    startX: 0,
    startY: 0,
    currentX: 0,
    currentY: 0,
    hasMoved: false,
  });
  let imageId = 0;
  const activeImagePointers = new Map<number, PointerPosition>();

  const ratio = computed(() => ratios.value.find((item) => item.value === ratioKey.value)?.ratio ?? 1);
  const cellCount = computed(() => rows.value * cols.value);
  const imageUploadLimit = computed(() => cellCount.value);
  const remainingImageSlots = computed(() => Math.max(0, imageUploadLimit.value - images.value.length));
  const isImageUploadLimitReached = computed(() => remainingImageSlots.value <= 0);
  const imageUploadStatus = computed(() => `${images.value.length} / ${imageUploadLimit.value}`);
  const selectedCell = computed(() => cellsState.value[selectedIndex.value]);
  const selectedImage = computed(() => {
    const cell = selectedCell.value;
    if (!cell || cell.imageIndex < 0 || cell.imageIndex >= images.value.length) return undefined;
    return images.value[cell.imageIndex];
  });
  const selectedExportFormat = computed<ExportFormatOption>(() => exportFormats.find((item) => item.value === exportFormat.value) ?? defaultExportFormat);
  const exportQualityEnabled = computed(() => selectedExportFormat.value.supportsQuality);
  const selectedZoomPercent = computed(() => Math.round((selectedCell.value?.zoom ?? 1) * 100));
  const activePresetIndex = computed(() => presets.findIndex((p) => p.rows === rows.value && p.cols === cols.value));
  const maxOutputWidth = computed(() => (ratio.value >= 1 ? 3840 : Math.round(3840 * ratio.value)));
  const maxOutputHeight = computed(() => Math.round(maxOutputWidth.value / ratio.value));
  const boardWrapStyle = computed(() => ({
    aspectRatio: `${ratio.value}`,
    width: `min(100%, 980px, calc((100vh - 300px) * ${ratio.value}))`,
  }));
  const getRangeStyle = (value: number, min: number, max: number): CSSProperties => {
    const percent = max === min ? 0 : ((value - min) / (max - min)) * 100;
    const progress = Math.min(Math.max(percent, 0), 100);
    return { '--range-progress': `${progress}%` };
  };
  const cells = computed(() => Array.from({ length: cellCount.value }, (_, index) => {
    const row = Math.floor(index / cols.value);
    const col = index % cols.value;
    const cell = cellsState.value[index];
    const image = cell && cell.imageIndex >= 0 && cell.imageIndex < images.value.length ? images.value[cell.imageIndex] : undefined;
    return { index, row, col, cell, image };
  }));
  const horizontalLineSegments = computed(() => horizontalSegments.value.flat());
  const verticalLineSegments = computed(() => verticalSegments.value.flat());
  const allLineSegments = computed(() => [...horizontalLineSegments.value, ...verticalLineSegments.value]);
  const draggedImageId = computed(() => imageQueueDragState.active ? imageQueueDragState.imageId : null);
  const seamHandles = computed<SeamHandle[]>(() => {
    const snapshot = { horizontal: horizontalSegments.value, vertical: verticalSegments.value };
    const handles: SeamHandle[] = [];

    for (let row = 0; row <= rows.value; row += 1) {
      for (let col = 0; col <= cols.value; col += 1) {
        const connections = getJunctionConnections(row, col);
        const point = getHandlePoint(snapshot, row, col);
        if (connections.length && point) handles.push({ id: `j-${row}-${col}`, row, col, point, connections });
      }
    }

    return handles;
  });

  /* ----------------------------- Geometry ----------------------------- */
  const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, Math.min(min, max)), Math.max(min, max));
  const createPoint = (x: number, y: number): MeshPoint => ({ x, y });
  const samePoint = (a: MeshPoint, b: MeshPoint) => Math.abs(a.x - b.x) < 0.0001 && Math.abs(a.y - b.y) < 0.0001;
  const gridPoint = (row: number, col: number) => createPoint(col / cols.value, row / rows.value);
  const getBounds = (points: MeshPoint[]): Bounds => {
    const xs = points.map((point) => point.x);
    const ys = points.map((point) => point.y);
    const minX = Math.min(...xs);
    const maxX = Math.max(...xs);
    const minY = Math.min(...ys);
    const maxY = Math.max(...ys);
    return { minX, maxX, minY, maxY, width: maxX - minX, height: maxY - minY };
  };
  const compactPolygon = (points: MeshPoint[]) => {
    const compacted = points.reduce<MeshPoint[]>((result, point) => {
      const previous = result[result.length - 1];
      if (!previous || !samePoint(previous, point)) result.push(point);
      return result;
    }, []);

    const first = compacted[0];
    const last = compacted[compacted.length - 1];
    if (compacted.length > 1 && first && last && samePoint(first, last)) compacted.pop();
    return compacted;
  };
  const drawPath = (ctx: CanvasRenderingContext2D, points: MeshPoint[]) => {
    ctx.beginPath();
    points.forEach((point, index) => {
      if (index) ctx.lineTo(point.x, point.y);
      else ctx.moveTo(point.x, point.y);
    });
    ctx.closePath();
  };

  /* -------------------------- Seam Segments -------------------------- */
  const createHorizontalSegments = () => Array.from({ length: Math.max(0, rows.value - 1) }, (_, row) => Array.from({ length: cols.value }, (_, col): SeamSegment => {
    const y = (row + 1) / rows.value;
    return {
      id: `h-${row}-${col}`,
      kind: 'horizontal',
      row,
      col,
      start: createPoint(col / cols.value, y),
      end: createPoint((col + 1) / cols.value, y),
    };
  }));
  const createVerticalSegments = () => Array.from({ length: rows.value }, (_, row) => Array.from({ length: Math.max(0, cols.value - 1) }, (_, col): SeamSegment => {
    const x = (col + 1) / cols.value;
    return {
      id: `v-${row}-${col}`,
      kind: 'vertical',
      row,
      col,
      start: createPoint(x, row / rows.value),
      end: createPoint(x, (row + 1) / rows.value),
    };
  }));
  const resetSeams = () => {
    horizontalSegments.value = createHorizontalSegments();
    verticalSegments.value = createVerticalSegments();
  };
  const cloneSegment = (segment: SeamSegment): SeamSegment => ({
    ...segment,
    start: { ...segment.start },
    end: { ...segment.end },
  });
  const cloneSeams = (source: SeamSnapshot = { horizontal: horizontalSegments.value, vertical: verticalSegments.value }): SeamSnapshot => ({
    horizontal: source.horizontal.map((row) => row.map(cloneSegment)),
    vertical: source.vertical.map((row) => row.map(cloneSegment)),
  });
  const getSnapshotSegment = (snapshot: SeamSnapshot, kind: SegmentKind, row: number, col: number) => (kind === 'horizontal' ? snapshot.horizontal[row]?.[col] : snapshot.vertical[row]?.[col]);
  const getSnapshotConnectionSegment = (snapshot: SeamSnapshot, connection: SeamConnection) => getSnapshotSegment(snapshot, connection.kind, connection.row, connection.col);
  const getHorizontalSegment = (row: number, col: number) => horizontalSegments.value[row]?.[col];
  const getVerticalSegment = (row: number, col: number) => verticalSegments.value[row]?.[col];
  const getJunctionConnections = (row: number, col: number): SeamConnection[] => {
    const connections: SeamConnection[] = [];

    if (row > 0 && row < rows.value) {
      if (col > 0) connections.push({ kind: 'horizontal', row: row - 1, col: col - 1, endpoint: 'end' });
      if (col < cols.value) connections.push({ kind: 'horizontal', row: row - 1, col, endpoint: 'start' });
    }

    if (col > 0 && col < cols.value) {
      if (row > 0) connections.push({ kind: 'vertical', row: row - 1, col: col - 1, endpoint: 'end' });
      if (row < rows.value) connections.push({ kind: 'vertical', row, col: col - 1, endpoint: 'start' });
    }

    return connections;
  };
  const getHandlePoint = (snapshot: SeamSnapshot, row: number, col: number) => {
    const points = getJunctionConnections(row, col)
      .map((connection) => getSnapshotConnectionSegment(snapshot, connection)?.[connection.endpoint])
      .filter((point): point is MeshPoint => Boolean(point));

    if (!points.length) return undefined;
    return createPoint(
      points.reduce((sum, point) => sum + point.x, 0) / points.length,
      points.reduce((sum, point) => sum + point.y, 0) / points.length,
    );
  };
  const getCellPoints = (row: number, col: number) => compactPolygon([
    row === 0 ? gridPoint(row, col) : getHorizontalSegment(row - 1, col)?.start ?? gridPoint(row, col),
    row === 0 ? gridPoint(row, col + 1) : getHorizontalSegment(row - 1, col)?.end ?? gridPoint(row, col + 1),
    col === cols.value - 1 ? gridPoint(row, col + 1) : getVerticalSegment(row, col)?.start ?? gridPoint(row, col + 1),
    col === cols.value - 1 ? gridPoint(row + 1, col + 1) : getVerticalSegment(row, col)?.end ?? gridPoint(row + 1, col + 1),
    row === rows.value - 1 ? gridPoint(row + 1, col + 1) : getHorizontalSegment(row, col)?.end ?? gridPoint(row + 1, col + 1),
    row === rows.value - 1 ? gridPoint(row + 1, col) : getHorizontalSegment(row, col)?.start ?? gridPoint(row + 1, col),
    col === 0 ? gridPoint(row + 1, col) : getVerticalSegment(row, col - 1)?.end ?? gridPoint(row + 1, col),
    col === 0 ? gridPoint(row, col) : getVerticalSegment(row, col - 1)?.start ?? gridPoint(row, col),
  ]);
  const getCellStyle = (row: number, col: number) => {
    const points = getCellPoints(row, col);
    const bounds = getBounds(points);
    const safeWidth = Math.max(bounds.width, 0.001);
    const safeHeight = Math.max(bounds.height, 0.001);
    const polygon = points.map((point) => `${((point.x - bounds.minX) / safeWidth) * 100}% ${((point.y - bounds.minY) / safeHeight) * 100}%`).join(', ');
    return {
      left: `${bounds.minX * 100}%`,
      top: `${bounds.minY * 100}%`,
      width: `${safeWidth * 100}%`,
      height: `${safeHeight * 100}%`,
      clipPath: `polygon(${polygon})`,
    };
  };
  const getLineStyle = (segment: SeamSegment) => {
    const dx = segment.end.x - segment.start.x;
    const dy = (segment.end.y - segment.start.y) / ratio.value;
    const length = Math.hypot(dx, dy) * 100;
    const angle = Math.atan2(dy, dx) * 180 / Math.PI;
    return { left: `${segment.start.x * 100}%`, top: `${segment.start.y * 100}%`, width: `${length}%`, transform: `rotate(${angle}deg)` };
  };
  const getHandleStyle = (handle: SeamHandle) => ({
    left: `${handle.point.x * 100}%`,
    top: `${handle.point.y * 100}%`,
    transform: 'translate(-50%, -50%)',
  });
  /* ----------------------------- Images ------------------------------ */
  const revokeImages = (items: PuzzleImage[]) => {
    items.forEach((image) => URL.revokeObjectURL(image.src));
  };
  const getImageFiles = (fileList: FileList | null) => (fileList ? Array.from(fileList).filter((file) => file.type.startsWith('image/')) : []);
  const loadImageFile = (file: File) => new Promise<PuzzleImage>((resolve, reject) => {
    const src = URL.createObjectURL(file);
    const image = new Image();
    image.onload = () => resolve({ id: imageId++, name: file.name, src, naturalWidth: image.naturalWidth, naturalHeight: image.naturalHeight });
    image.onerror = () => {
      URL.revokeObjectURL(src);
      reject(new Error(`Failed to load image: ${file.name}`));
    };
    image.src = src;
  });
  const loadImageFiles = async (fileList: FileList | null, limit: number) => {
    const files = getImageFiles(fileList);
    const safeLimit = Math.max(0, Math.floor(limit));
    const acceptedFiles = files.slice(0, safeLimit);
    const loadedResults = await Promise.allSettled(acceptedFiles.map(loadImageFile));
    const loaded = loadedResults.flatMap((result) => (result.status === 'fulfilled' ? [result.value] : []));
    return {
      loaded,
      skippedCount: Math.max(0, files.length - acceptedFiles.length),
      failedCount: acceptedFiles.length - loaded.length,
      imageFileCount: files.length,
    };
  };
  const setUploadFeedback = (loadedCount: number, skippedCount = 0, failedCount = 0) => {
    const messages: string[] = [];
    if (loadedCount) messages.push(`已添加 ${loadedCount} 张图片`);
    if (skippedCount) messages.push(`超过当前 ${imageUploadLimit.value} 张上限，已忽略 ${skippedCount} 张`);
    if (failedCount) messages.push(`${failedCount} 张图片读取失败`);
    imageUploadFeedback.value = messages.join('，');
  };
  const fillCells = () => {
    cellsState.value = Array.from({ length: cellCount.value }, (_, index) => {
      const previous = cellsState.value[index];
      const previousValid = previous && previous.imageIndex >= 0 && previous.imageIndex < images.value.length;
      if (previousValid) {
        return {
          imageIndex: previous.imageIndex,
          offsetX: previous.offsetX ?? 0,
          offsetY: previous.offsetY ?? 0,
          zoom: previous.zoom ?? 1,
        };
      }
      return {
        imageIndex: index < images.value.length ? index : -1,
        offsetX: previous?.offsetX ?? 0,
        offsetY: previous?.offsetY ?? 0,
        zoom: previous?.zoom ?? 1,
      };
    });
  };
  const enforceImageUploadLimit = () => {
    const overflowCount = images.value.length - imageUploadLimit.value;
    if (overflowCount <= 0) return;
    const removedImages = images.value.splice(imageUploadLimit.value);
    revokeImages(removedImages);
    fillCells();
    imageUploadFeedback.value = `已按当前格子上限保留前 ${imageUploadLimit.value} 张图片`;
  };
  const addFiles = async (fileList: FileList | null) => {
    const targetCellIndex = pendingCellIndex.value;
    pendingCellIndex.value = null;
    const { loaded, skippedCount, failedCount, imageFileCount } = await loadImageFiles(fileList, remainingImageSlots.value);
    if (!imageFileCount) {
      imageUploadFeedback.value = '请选择图片文件';
      return;
    }
    if (!loaded.length) {
      setUploadFeedback(0, skippedCount, failedCount);
      return;
    }
    const firstLoadedIndex = images.value.length;
    images.value.push(...loaded);
    fillCells();
    if (targetCellIndex !== null && targetCellIndex >= 0 && targetCellIndex < cellCount.value && loaded.length) {
      const targetCell = cellsState.value[targetCellIndex];
      if (targetCell) targetCell.imageIndex = firstLoadedIndex;
      selectedIndex.value = targetCellIndex;
    } else {
      selectedIndex.value = 0;
    }
    setUploadFeedback(loaded.length, skippedCount, failedCount);
  };
  const openFilePickerForCell = (cellIndex: number) => {
    if (isImageUploadLimitReached.value) return;
    pendingCellIndex.value = clamp(Math.trunc(cellIndex), 0, cellCount.value - 1);
    const input = fileInputRef.value;
    if (input) {
      input.value = '';
      input.click();
    }
  };
  const insertFiles = async (index: number, fileList: FileList | null) => {
    const targetIndex = clamp(Math.trunc(index), 0, images.value.length);
    const { loaded, skippedCount, failedCount, imageFileCount } = await loadImageFiles(fileList, remainingImageSlots.value);
    if (!imageFileCount) {
      imageUploadFeedback.value = '请选择图片文件';
      return;
    }
    if (!loaded.length) {
      setUploadFeedback(0, skippedCount, failedCount);
      return;
    }
    images.value.splice(targetIndex, 0, ...loaded);
    fillCells();
    selectedIndex.value = clamp(selectedIndex.value, 0, Math.max(0, cellCount.value - 1));
    setUploadFeedback(loaded.length, skippedCount, failedCount);
  };
  const replaceImage = async (index: number, fileList: FileList | null) => {
    const targetIndex = Math.trunc(index);
    if (targetIndex < 0 || targetIndex >= images.value.length) return;
    const currentImage = images.value[targetIndex];
    const [file] = getImageFiles(fileList);
    if (!currentImage || !file) {
      imageUploadFeedback.value = '请选择图片文件';
      return;
    }
    try {
      const nextImage = await loadImageFile(file);
      images.value.splice(targetIndex, 1, nextImage);
      URL.revokeObjectURL(currentImage.src);
      fillCells();
      imageUploadFeedback.value = `已替换第 ${targetIndex + 1} 张图片`;
    } catch {
      imageUploadFeedback.value = '图片读取失败';
    }
  };
  const removeImage = (index: number) => {
    const targetIndex = Math.trunc(index);
    if (targetIndex < 0 || targetIndex >= images.value.length) return;
    const [removedImage] = images.value.splice(targetIndex, 1);
    if (!removedImage) return;
    URL.revokeObjectURL(removedImage.src);
    if (images.value.length) fillCells();
    else cellsState.value = [];
    selectedIndex.value = clamp(selectedIndex.value, 0, Math.max(0, cellCount.value - 1));
    stopImageQueueDrag();
    imageUploadFeedback.value = `已移除 ${removedImage.name}`;
  };
  const clearImages = () => {
    revokeImages(images.value);
    images.value = [];
    cellsState.value = [];
    selectedIndex.value = 0;
    imageUploadFeedback.value = '';
    stopImageQueueDrag();
  };
  const swapImages = (indexA: number, indexB: number) => {
    if (indexA === indexB || indexA < 0 || indexB < 0 || indexA >= images.value.length || indexB >= images.value.length) return;
    const nextImages = [...images.value];
    const temp = nextImages[indexA];
    if (nextImages[indexB])
      nextImages[indexA] = nextImages[indexB];
    if (temp)
      nextImages[indexB] = temp;
    images.value = nextImages;
    fillCells();
  };
  const getImageQueueTargetIndex = (clientX: number, clientY: number) => {
    const queueItems = Array.from(document.querySelectorAll<HTMLElement>('[data-image-queue-index]'));
    if (!queueItems.length) return -1;
    return queueItems.reduce((closest, item) => {
      const rect = item.getBoundingClientRect();
      const centerX = rect.left + rect.width / 2;
      const centerY = rect.top + rect.height / 2;
      const distance = Math.hypot(clientX - centerX, clientY - centerY);
      if (distance >= closest.distance) return closest;
      return { index: Number(item.dataset.imageQueueIndex), distance };
    }, { index: -1, distance: Number.POSITIVE_INFINITY }).index;
  };
  const bindWindowImageQueueDrag = () => {
    window.addEventListener('pointermove', handleImageQueueDrag, { passive: false });
    window.addEventListener('pointerup', endImageQueueDrag);
    window.addEventListener('pointercancel', endImageQueueDrag);
  };
  const startImageQueueDrag = (event: PointerEvent, index: number) => {
    const image = images.value[index];
    if (!image) return;
    event.preventDefault();
    event.stopPropagation();
    stopDrag();
    const target = event.currentTarget as HTMLElement | null;
    target?.setPointerCapture?.(event.pointerId);
    Object.assign(imageQueueDragState, {
      active: true,
      imageId: image.id,
      startIndex: index,
      overIndex: index,
      startX: event.clientX,
      startY: event.clientY,
      currentX: event.clientX,
      currentY: event.clientY,
      hasMoved: false,
    });
    bindWindowImageQueueDrag();
  };
  const handleImageQueueDrag = (event: PointerEvent) => {
    if (!imageQueueDragState.active || imageQueueDragState.imageId === null) return;
    event.preventDefault();
    imageQueueDragState.currentX = event.clientX;
    imageQueueDragState.currentY = event.clientY;
    if (!imageQueueDragState.hasMoved) {
      const distance = Math.hypot(event.clientX - imageQueueDragState.startX, event.clientY - imageQueueDragState.startY);
      imageQueueDragState.hasMoved = distance >= QUEUE_DRAG_THRESHOLD;
      if (!imageQueueDragState.hasMoved) return;
    }

    const targetIndex = getImageQueueTargetIndex(event.clientX, event.clientY);
    if (targetIndex < 0) return;
    imageQueueDragState.overIndex = targetIndex;
  };
  const stopImageQueueDrag = () => {
    imageQueueDragState.active = false;
    imageQueueDragState.imageId = null;
    imageQueueDragState.startIndex = -1;
    imageQueueDragState.overIndex = -1;
    imageQueueDragState.hasMoved = false;
    window.removeEventListener('pointermove', handleImageQueueDrag);
    window.removeEventListener('pointerup', endImageQueueDrag);
    window.removeEventListener('pointercancel', endImageQueueDrag);
  };
  const endImageQueueDrag = (event: PointerEvent) => {
    event.preventDefault();
    if (imageQueueDragState.hasMoved && imageQueueDragState.startIndex >= 0 && imageQueueDragState.overIndex >= 0) {
      swapImages(imageQueueDragState.startIndex, imageQueueDragState.overIndex);
    }
    stopImageQueueDrag();
  };
  const selectCell = (index: number) => { selectedIndex.value = index; };
  const setCellZoom = (index: number, zoom: number) => {
    const cell = cellsState.value[index];
    if (!cell) return;
    cell.zoom = clamp(Number.isFinite(zoom) ? zoom : 1, MIN_IMAGE_ZOOM, MAX_IMAGE_ZOOM);
  };
  const setSelectedZoom = (zoom: number) => setCellZoom(selectedIndex.value, zoom);
  const nudgeSelectedZoom = (delta: number) => {
    const cell = selectedCell.value;
    if (!cell) return;
    setSelectedZoom(cell.zoom + delta);
  };
  const resetSelectedCellView = () => {
    const cell = selectedCell.value;
    if (!cell) return;
    cell.offsetX = 0;
    cell.offsetY = 0;
    cell.zoom = 1;
  };
  const zoomCellFromWheel = (event: WheelEvent, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    const cell = cellsState.value[index];
    if (!cell) return;
    selectedIndex.value = index;
    setCellZoom(index, cell.zoom * (event.deltaY > 0 ? 0.92 : 1.08));
  };
  const getImageStyle = (row: number, col: number, cell: CellState, image: PuzzleImage) => {
    const bounds = getBounds(getCellPoints(row, col));
    const cellAspect = (bounds.width * ratio.value) / Math.max(bounds.height, 0.001);
    const imageAspect = image.naturalWidth / image.naturalHeight;
    const width = imageAspect > cellAspect ? (imageAspect / cellAspect) * 100 * cell.zoom : 100 * cell.zoom;
    const height = imageAspect > cellAspect ? 100 * cell.zoom : (cellAspect / imageAspect) * 100 * cell.zoom;

    return {
      left: `${(100 - width) / 2 + cell.offsetX}%`,
      top: `${(100 - height) / 2 + cell.offsetY}%`,
      width: `${width}%`,
      height: `${height}%`,
    };
  };

  /* ---------------------------- Controls ----------------------------- */
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
  /* ----------------------------- Dragging ----------------------------- */
  const getEndpointBounds = (segment: SeamSegment, endpoint: EndpointKey) => {
    if (segment.kind === 'horizontal') {
      let minX = MIN_ENDPOINT_GAP;
      let maxX = 1 - MIN_ENDPOINT_GAP;
      const minY = segment.row / rows.value + MIN_ENDPOINT_GAP;
      const maxY = (segment.row + 2) / rows.value - MIN_ENDPOINT_GAP;

      if (endpoint === 'start' && segment.col === 0) {
        minX = 0;
        maxX = 0;
      } else if (endpoint === 'end' && segment.col === cols.value - 1) {
        minX = 1;
        maxX = 1;
      } else if (endpoint === 'start') {
        maxX = Math.min(maxX, segment.end.x - MIN_ENDPOINT_GAP);
      } else {
        minX = Math.max(minX, segment.start.x + MIN_ENDPOINT_GAP);
      }

      return { minX, maxX, minY, maxY };
    }

    const minX = segment.col / cols.value + MIN_ENDPOINT_GAP;
    const maxX = (segment.col + 2) / cols.value - MIN_ENDPOINT_GAP;
    let minY = MIN_ENDPOINT_GAP;
    let maxY = 1 - MIN_ENDPOINT_GAP;

    if (endpoint === 'start' && segment.row === 0) {
      minY = 0;
      maxY = 0;
    } else if (endpoint === 'end' && segment.row === rows.value - 1) {
      minY = 1;
      maxY = 1;
    } else if (endpoint === 'start') {
      maxY = Math.min(maxY, segment.end.y - MIN_ENDPOINT_GAP);
    } else {
      minY = Math.max(minY, segment.start.y + MIN_ENDPOINT_GAP);
    }

    return { minX, maxX, minY, maxY };
  };
  const getHandleBounds = (snapshot: SeamSnapshot, row: number, col: number) => {
    const bounds = { minX: 0, maxX: 1, minY: 0, maxY: 1 };

    getJunctionConnections(row, col).forEach((connection) => {
      const segment = getSnapshotConnectionSegment(snapshot, connection);
      if (!segment) return;
      const endpointBounds = getEndpointBounds(segment, connection.endpoint);
      bounds.minX = Math.max(bounds.minX, endpointBounds.minX);
      bounds.maxX = Math.min(bounds.maxX, endpointBounds.maxX);
      bounds.minY = Math.max(bounds.minY, endpointBounds.minY);
      bounds.maxY = Math.min(bounds.maxY, endpointBounds.maxY);
    });

    return bounds;
  };
  const setHandlePoint = (snapshot: SeamSnapshot, row: number, col: number, x: number, y: number) => {
    const bounds = getHandleBounds(snapshot, row, col);
    const nextPoint = createPoint(clamp(x, bounds.minX, bounds.maxX), clamp(y, bounds.minY, bounds.maxY));

    getJunctionConnections(row, col).forEach((connection) => {
      const segment = getSnapshotConnectionSegment(snapshot, connection);
      if (!segment) return;
      segment[connection.endpoint].x = nextPoint.x;
      segment[connection.endpoint].y = nextPoint.y;
    });
  };
  const getSegmentHandleCoords = (kind: SegmentKind, row: number, col: number) => {
    if (kind === 'horizontal') return [{ row: row + 1, col }, { row: row + 1, col: col + 1 }];
    return [{ row, col: col + 1 }, { row: row + 1, col: col + 1 }];
  };
  const translateSegmentHandles = (targetSnapshot: SeamSnapshot, sourceSnapshot: SeamSnapshot, kind: SegmentKind, row: number, col: number, dx: number, dy: number) => {
    getSegmentHandleCoords(kind, row, col).forEach((handleCoord) => {
      const startPoint = getHandlePoint(sourceSnapshot, handleCoord.row, handleCoord.col);
      if (!startPoint) return;
      setHandlePoint(
        targetSnapshot,
        handleCoord.row,
        handleCoord.col,
        kind === 'horizontal' ? startPoint.x : startPoint.x + dx,
        kind === 'horizontal' ? startPoint.y + dy : startPoint.y,
      );
    });
  };
  const getPinchGesture = () => {
    const [first, second] = Array.from(activeImagePointers.values());
    if (!first || !second) return undefined;
    const dx = second.x - first.x;
    const dy = second.y - first.y;
    return {
      distance: Math.max(Math.hypot(dx, dy), 1),
      centerX: (first.x + second.x) / 2,
      centerY: (first.y + second.y) / 2,
    };
  };
  const bindWindowDrag = () => {
    window.addEventListener('pointermove', handleDrag, { passive: false });
    window.addEventListener('pointerup', handlePointerEnd);
    window.addEventListener('pointercancel', handlePointerEnd);
  };
  const startImageDrag = (event: PointerEvent, index: number) => {
    const cell = cellsState.value[index];
    if (!cell) return;
    event.preventDefault();
    event.stopPropagation();
    const target = event.currentTarget as HTMLElement | null;
    target?.setPointerCapture?.(event.pointerId);
    const rect = target?.getBoundingClientRect();
    activeImagePointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
    selectedIndex.value = index;
    if (activeImagePointers.size >= 2) {
      const pinch = getPinchGesture();
      if (!pinch) return;
      Object.assign(dragState, {
        active: true,
        mode: 'pinch',
        index,
        startX: pinch.centerX,
        startY: pinch.centerY,
        startCell: { ...cell },
        startDistance: pinch.distance,
        startRectWidth: rect?.width || 1,
        startRectHeight: rect?.height || 1,
      });
      bindWindowDrag();
      return;
    }

    Object.assign(dragState, {
      active: true,
      mode: 'image',
      index,
      startX: event.clientX,
      startY: event.clientY,
      startCell: { ...cell },
      startRectWidth: rect?.width || 1,
      startRectHeight: rect?.height || 1,
    });
    bindWindowDrag();
  };
  const startSegmentDrag = (event: PointerEvent, segment: SeamSegment) => {
    event.preventDefault();
    event.stopPropagation();
    activeImagePointers.clear();
    Object.assign(dragState, {
      active: true,
      mode: 'segment',
      kind: segment.kind,
      row: segment.row,
      col: segment.col,
      startX: event.clientX,
      startY: event.clientY,
      startSeams: cloneSeams(),
    });
    bindWindowDrag();
  };
  const startHandleDrag = (event: PointerEvent, handle: SeamHandle) => {
    event.preventDefault();
    event.stopPropagation();
    activeImagePointers.clear();
    Object.assign(dragState, {
      active: true,
      mode: 'endpoint',
      row: handle.row,
      col: handle.col,
      startX: event.clientX,
      startY: event.clientY,
      startSeams: cloneSeams(),
    });
    bindWindowDrag();
  };
  const handleDrag = (event: PointerEvent) => {
    if (!dragState.active) return;
    event.preventDefault();
    if (activeImagePointers.has(event.pointerId)) activeImagePointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
    const board = boardRef.value;
    if (!board) return;
    const rect = board.getBoundingClientRect();
    const dxPx = event.clientX - dragState.startX;
    const dyPx = event.clientY - dragState.startY;
    if (dragState.mode === 'pinch') {
      const pinch = getPinchGesture();
      const cell = cellsState.value[dragState.index];
      if (!pinch || !cell || !dragState.startDistance) return;
      setCellZoom(dragState.index, dragState.startCell.zoom * (pinch.distance / dragState.startDistance));
      cell.offsetX = clamp(dragState.startCell.offsetX + ((pinch.centerX - dragState.startX) / dragState.startRectWidth) * 100, -IMAGE_OFFSET_LIMIT, IMAGE_OFFSET_LIMIT);
      cell.offsetY = clamp(dragState.startCell.offsetY + ((pinch.centerY - dragState.startY) / dragState.startRectHeight) * 100, -IMAGE_OFFSET_LIMIT, IMAGE_OFFSET_LIMIT);
      return;
    }

    if (dragState.mode === 'image') {
      const cell = cellsState.value[dragState.index];
      if (!cell) return;
      cell.offsetX = clamp(dragState.startCell.offsetX + (dxPx / dragState.startRectWidth) * 100, -IMAGE_OFFSET_LIMIT, IMAGE_OFFSET_LIMIT);
      cell.offsetY = clamp(dragState.startCell.offsetY + (dyPx / dragState.startRectHeight) * 100, -IMAGE_OFFSET_LIMIT, IMAGE_OFFSET_LIMIT);
      return;
    }

    const nextSeams = cloneSeams(dragState.startSeams);
    const dx = dxPx / rect.width;
    const dy = dyPx / rect.height;

    if (dragState.mode === 'segment') translateSegmentHandles(nextSeams, dragState.startSeams, dragState.kind, dragState.row, dragState.col, dx, dy);
    if (dragState.mode === 'endpoint') {
      const startPoint = getHandlePoint(dragState.startSeams, dragState.row, dragState.col);
      if (!startPoint) return;
      setHandlePoint(nextSeams, dragState.row, dragState.col, startPoint.x + dx, startPoint.y + dy);
    }

    horizontalSegments.value = nextSeams.horizontal;
    verticalSegments.value = nextSeams.vertical;
  };
  const handlePointerEnd = (event: PointerEvent) => {
    if (activeImagePointers.has(event.pointerId)) activeImagePointers.delete(event.pointerId);
    if (dragState.mode === 'pinch' && activeImagePointers.size === 1) {
      const [remainingPointer] = Array.from(activeImagePointers.values());
      const cell = cellsState.value[dragState.index];
      if (remainingPointer && cell) {
        Object.assign(dragState, {
          active: true,
          mode: 'image',
          startX: remainingPointer.x,
          startY: remainingPointer.y,
          startCell: { ...cell },
        });
        return;
      }
    }

    stopDrag();
  };
  const stopDrag = () => {
    dragState.active = false;
    activeImagePointers.clear();
    window.removeEventListener('pointermove', handleDrag);
    window.removeEventListener('pointerup', handlePointerEnd);
    window.removeEventListener('pointercancel', handlePointerEnd);
  };
  const loadImageElement = (src: string) => new Promise<HTMLImageElement>((resolve) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.src = src;
  });
  const loadExportImages = async () => {
    const entries = await Promise.all(images.value.map(async (image) => [image.id, await loadImageElement(image.src)] as const));
    return new Map(entries);
  };
  const drawCoverImage = (
    ctx: CanvasRenderingContext2D,
    image: HTMLImageElement,
    imageData: PuzzleImage,
    cell: CellState,
    x: number,
    y: number,
    width: number,
    height: number,
    overscan = 0,
  ) => {
    const baseScale = Math.max(
      (width + overscan * 2) / imageData.naturalWidth,
      (height + overscan * 2) / imageData.naturalHeight,
    ) * cell.zoom;
    const drawWidth = imageData.naturalWidth * baseScale;
    const drawHeight = imageData.naturalHeight * baseScale;
    const drawX = x + (width - drawWidth) / 2 + (cell.offsetX / 100) * width;
    const drawY = y + (height - drawHeight) / 2 + (cell.offsetY / 100) * height;
    ctx.drawImage(image, drawX, drawY, drawWidth, drawHeight);
  };
  const getHardMaskEdges = (row: number, col: number, layerX: number, layerY: number, canvasWidth: number, canvasHeight: number): HardMaskEdges => ({
    top: row === 0 ? -layerY : undefined,
    right: col === cols.value - 1 ? canvasWidth - layerX : undefined,
    bottom: row === rows.value - 1 ? canvasHeight - layerY : undefined,
    left: col === 0 ? -layerX : undefined,
  });
  const hardenMaskEdges = (ctx: CanvasRenderingContext2D, width: number, height: number, points: MeshPoint[], feather: number, edges: HardMaskEdges) => {
    const strip = Math.ceil(feather * 2 + 2);

    ctx.save();
    drawPath(ctx, points);
    ctx.clip();
    ctx.fillStyle = '#000';
    if (edges.top !== undefined) ctx.fillRect(0, clamp(edges.top, 0, height), width, strip);
    if (edges.right !== undefined) ctx.fillRect(clamp(edges.right - strip, 0, width), 0, strip, height);
    if (edges.bottom !== undefined) ctx.fillRect(0, clamp(edges.bottom - strip, 0, height), width, strip);
    if (edges.left !== undefined) ctx.fillRect(clamp(edges.left, 0, width), 0, strip, height);
    ctx.restore();
  };
  const createFeatherMask = (width: number, height: number, points: MeshPoint[], feather: number, hardEdges: HardMaskEdges) => {
    const mask = document.createElement('canvas');
    mask.width = width;
    mask.height = height;
    const maskCtx = mask.getContext('2d');
    if (!maskCtx) return mask;

    maskCtx.imageSmoothingEnabled = true;
    maskCtx.imageSmoothingQuality = 'high';
    maskCtx.filter = feather > 0 ? `blur(${feather}px)` : 'none';
    maskCtx.fillStyle = '#000';
    drawPath(maskCtx, points);
    maskCtx.fill();
    maskCtx.filter = 'none';
    hardenMaskEdges(maskCtx, width, height, points, feather, hardEdges);
    return mask;
  };
  const createFeatheredCellLayer = (
    image: HTMLImageElement,
    imageData: PuzzleImage,
    cell: CellState,
    row: number,
    col: number,
    points: MeshPoint[],
    bounds: Bounds,
    feather: number,
    canvasWidth: number,
    canvasHeight: number,
  ) => {
    const padding = Math.ceil(feather * 2 + 2);
    const layerX = Math.floor(bounds.minX - padding);
    const layerY = Math.floor(bounds.minY - padding);
    const layerWidth = Math.max(1, Math.ceil(bounds.maxX + padding) - layerX);
    const layerHeight = Math.max(1, Math.ceil(bounds.maxY + padding) - layerY);
    const layer = document.createElement('canvas');
    layer.width = layerWidth;
    layer.height = layerHeight;
    const layerCtx = layer.getContext('2d');
    if (!layerCtx) return undefined;

    layerCtx.imageSmoothingEnabled = true;
    layerCtx.imageSmoothingQuality = 'high';
    drawCoverImage(layerCtx, image, imageData, cell, bounds.minX - layerX, bounds.minY - layerY, bounds.width, bounds.height, padding);

    const localPoints = points.map((point) => createPoint(point.x - layerX, point.y - layerY));
    const hardEdges = getHardMaskEdges(row, col, layerX, layerY, canvasWidth, canvasHeight);
    const mask = createFeatherMask(layerWidth, layerHeight, localPoints, feather, hardEdges);
    layerCtx.globalCompositeOperation = 'destination-in';
    layerCtx.drawImage(mask, 0, 0);
    layerCtx.globalCompositeOperation = 'source-over';

    return { canvas: layer, x: layerX, y: layerY };
  };
  const drawFeatherBlend = (
    ctx: CanvasRenderingContext2D,
    renderCells: RenderCell[],
    feather: number,
  ) => {
    const width = ctx.canvas.width;
    const height = ctx.canvas.height;
    const pixelCount = width * height;
    const red = new Uint32Array(pixelCount);
    const green = new Uint32Array(pixelCount);
    const blue = new Uint32Array(pixelCount);
    const alpha = new Uint16Array(pixelCount);

    renderCells.forEach(({ row, col, cell, imageData, image, points, bounds }) => {
      const layer = createFeatheredCellLayer(image, imageData, cell, row, col, points, bounds, feather, width, height);
      if (!layer) return;
      const layerCtx = layer.canvas.getContext('2d');
      if (!layerCtx) return;
      const data = layerCtx.getImageData(0, 0, layer.canvas.width, layer.canvas.height).data;

      for (let y = 0; y < layer.canvas.height; y += 1) {
        const targetY = layer.y + y;
        if (targetY < 0 || targetY >= height) continue;

        for (let x = 0; x < layer.canvas.width; x += 1) {
          const targetX = layer.x + x;
          if (targetX < 0 || targetX >= width) continue;

          const sourceIndex = (y * layer.canvas.width + x) * 4;
          const sourceAlpha = data[sourceIndex + 3] ?? 0;
          if (!sourceAlpha) continue;
          const sourceRed = data[sourceIndex] ?? 0;
          const sourceGreen = data[sourceIndex + 1] ?? 0;
          const sourceBlue = data[sourceIndex + 2] ?? 0;

          const targetIndex = targetY * width + targetX;
          red[targetIndex] = (red[targetIndex] ?? 0) + sourceRed * sourceAlpha;
          green[targetIndex] = (green[targetIndex] ?? 0) + sourceGreen * sourceAlpha;
          blue[targetIndex] = (blue[targetIndex] ?? 0) + sourceBlue * sourceAlpha;
          alpha[targetIndex] = (alpha[targetIndex] ?? 0) + sourceAlpha;
        }
      }
    });

    const output = ctx.createImageData(width, height);
    for (let index = 0; index < pixelCount; index += 1) {
      const targetIndex = index * 4;
      const weight = alpha[index] ?? 0;

      if (!weight) {
        output.data[targetIndex] = 255;
        output.data[targetIndex + 1] = 255;
        output.data[targetIndex + 2] = 255;
        output.data[targetIndex + 3] = 255;
        continue;
      }

      const coverage = Math.min(255, weight) / 255;
      const inverseCoverage = 1 - coverage;
      output.data[targetIndex] = Math.round(((red[index] ?? 0) / weight) * coverage + 255 * inverseCoverage);
      output.data[targetIndex + 1] = Math.round(((green[index] ?? 0) / weight) * coverage + 255 * inverseCoverage);
      output.data[targetIndex + 2] = Math.round(((blue[index] ?? 0) / weight) * coverage + 255 * inverseCoverage);
      output.data[targetIndex + 3] = 255;
    }

    ctx.putImageData(output, 0, 0);
  };
  const drawHardCell = (
    ctx: CanvasRenderingContext2D,
    image: HTMLImageElement,
    imageData: PuzzleImage,
    cell: CellState,
    points: MeshPoint[],
    bounds: Bounds,
  ) => {
    ctx.save();
    drawPath(ctx, points);
    ctx.clip();
    drawCoverImage(ctx, image, imageData, cell, bounds.minX, bounds.minY, bounds.width, bounds.height);
    ctx.restore();
  };
  const drawLineSeparators = (ctx: CanvasRenderingContext2D, scale: number) => {
    ctx.strokeStyle = lineColor.value;
    ctx.lineWidth = Math.max(1, lineWidth.value * scale);
    ctx.lineCap = 'round';

    allLineSegments.value.forEach((segment) => {
      ctx.beginPath();
      ctx.moveTo(segment.start.x * ctx.canvas.width, segment.start.y * ctx.canvas.height);
      ctx.lineTo(segment.end.x * ctx.canvas.width, segment.end.y * ctx.canvas.height);
      ctx.stroke();
    });
  };
  const encodeCanvas = (canvas: HTMLCanvasElement) => {
    const format = selectedExportFormat.value;
    const dataUrl = format.supportsQuality
      ? canvas.toDataURL(format.mimeType, quality.value)
      : canvas.toDataURL(format.mimeType);

    if (dataUrl.startsWith(`data:${format.mimeType}`)) {
      return { dataUrl, extension: format.extension };
    }

    return {
      dataUrl: canvas.toDataURL('image/png'),
      extension: 'png',
    };
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

    const boardWidth = boardRef.value?.clientWidth || FALLBACK_BOARD_WIDTH;
    const exportScale = canvas.width / boardWidth;
    const feather = Math.max(1, featherSize.value * exportScale);
    const exportImages = await loadExportImages();
    const renderCells = cells.value.flatMap((cellInfo) => {
      if (!cellInfo.cell || !cellInfo.image) return [];
      const image = exportImages.get(cellInfo.image.id);
      if (!image) return [];

      const points = getCellPoints(cellInfo.row, cellInfo.col).map((point) => ({ x: point.x * canvas.width, y: point.y * canvas.height }));
      const bounds = getBounds(points);
      if (bounds.width <= 0 || bounds.height <= 0) return [];

      return [{ row: cellInfo.row, col: cellInfo.col, cell: cellInfo.cell, imageData: cellInfo.image, image, points, bounds }];
    });

    if (gapStyle.value === 'feather') {
      drawFeatherBlend(ctx, renderCells, feather);
    } else {
      renderCells.forEach(({ cell, imageData, image, points, bounds }) => drawHardCell(ctx, image, imageData, cell, points, bounds));
    }

    if (gapStyle.value === 'line') drawLineSeparators(ctx, exportScale);

    const encoded = encodeCanvas(canvas);
    const link = document.createElement('a');
    link.download = `puzzle-${rows.value}x${cols.value}.${encoded.extension}`;
    link.href = encoded.dataUrl;
    link.click();
  };

  watch([rows, cols], () => {
    rows.value = clamp(rows.value, 1, 10);
    cols.value = clamp(cols.value, 1, 10);
    enforceImageUploadLimit();
    resetSeams();
    fillCells();
    selectedIndex.value = clamp(selectedIndex.value, 0, Math.max(0, cellCount.value - 1));
  }, { immediate: true });
  onMounted(() => { updateScreenRatio(); window.addEventListener('resize', updateScreenRatio); });
  onUnmounted(() => {
    images.value.forEach((image) => URL.revokeObjectURL(image.src));
    window.removeEventListener('resize', updateScreenRatio);
    window.removeEventListener('pointermove', handleDrag);
    window.removeEventListener('pointerup', handlePointerEnd);
    window.removeEventListener('pointercancel', handlePointerEnd);
    window.removeEventListener('pointermove', handleImageQueueDrag);
    window.removeEventListener('pointerup', endImageQueueDrag);
    window.removeEventListener('pointercancel', endImageQueueDrag);
  });

  return {
    presets,
    ratios,
    MIN_IMAGE_ZOOM,
    MAX_IMAGE_ZOOM,
    IMAGE_ZOOM_STEP,
    fileInputRef,
    boardRef,
    rows,
    cols,
    gapStyle,
    lineColor,
    lineWidth,
    featherSize,
    ratioKey,
    exportFormats,
    exportFormat,
    quality,
    selectedIndex,
    images,
    imageUploadLimit,
    remainingImageSlots,
    isImageUploadLimitReached,
    imageUploadStatus,
    imageUploadFeedback,
    imageQueueDragState,
    draggedImageId,
    cellsState,
    ratio,
    cellCount,
    selectedCell,
    selectedImage,
    selectedExportFormat,
    exportQualityEnabled,
    selectedZoomPercent,
    activePresetIndex,
    maxOutputWidth,
    maxOutputHeight,
    boardWrapStyle,
    getRangeStyle,
    cells,
    horizontalLineSegments,
    verticalLineSegments,
    allLineSegments,
    seamHandles,
    getCellStyle,
    getLineStyle,
    getHandleStyle,
    addFiles,
    insertFiles,
    replaceImage,
    removeImage,
    clearImages,
    startImageQueueDrag,
    selectCell,
    setCellZoom,
    setSelectedZoom,
    nudgeSelectedZoom,
    resetSelectedCellView,
    zoomCellFromWheel,
    getImageStyle,
    applyPreset,
    applyPresetByIndex,
    updateScreenRatio,
    startImageDrag,
    startSegmentDrag,
    startHandleDrag,
    exportPuzzle,
    openFilePickerForCell,
  };
};
