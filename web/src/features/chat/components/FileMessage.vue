<script setup lang="ts">
import { computed, inject, ref, watch } from 'vue';
import type { FileExtra, MessageSummary } from '../types';
import {
  formatFileSize,
  getDocumentIconType,
  getFilePreviewType,
  getFileTypeLabel,
  isPreviewableFileType,
  parseMessageExtra,
} from '../utils/fileHelper';
import { useFileDownloader } from '../composables/useFileDownloader';
import SvgIcon from '@/components/SvgIcon/index.vue'
import { notifyError } from '@/utils/notification';

const props = defineProps<{
  message?: MessageSummary | null;
  showDownload?: boolean;
  src: string;
  isNewMessage?: boolean;
}>();

const fileExtra = computed<FileExtra | null>(() => {
  return parseMessageExtra(props.message?.extra);
});

interface GalleryFile {
  name: string;
  url: string;
  type: string;
  path?: string;
  messageId?: number;
}

const gallery = inject<{
  register: (file: GalleryFile) => void;
  open: (url?: string) => void;
}>('gallery');

const PREVIEW_SIZE_LIMIT = 15 * 1024 * 1024;

const previewUrl = ref('');
const thumbnailUrl = ref('');
const previewLoading = ref(false);
const thumbnailLoading = ref(false);
const hasError = ref(false);
const loadProgress = ref(0);
const loadStatus = ref<'idle' | 'loading' | 'ready' | 'error'>('idle');

const { requestBlobUrl, requestDownload, downloadAndSaveFile } = useFileDownloader();

const category = computed(() => getFilePreviewType(fileExtra.value));
const documentIconName = computed(() => `document-${getDocumentIconType(category.value, fileExtra.value)}`);
const displayType = computed(() => getFileTypeLabel(category.value));
const isPreviewable = computed(() => isPreviewableFileType(category.value));
const isLargeForPreview = computed(() => (fileExtra.value?.fileSize ?? 0) > PREVIEW_SIZE_LIMIT);

watch(
  () => ({
    fileKey: fileExtra.value?.fileKey,
    thumbnailKey: fileExtra.value?.thumbnailUrl,
    category: category.value,
  }),
  () => {
    previewUrl.value = '';
    thumbnailUrl.value = '';
    previewLoading.value = false;
    thumbnailLoading.value = false;
    hasError.value = false;
    loadProgress.value = 0;
    loadStatus.value = 'idle';
    loadInlineMedia();
  },
  { immediate: true },
);

function registerToGlobalGallery(url: string) {
  if (!gallery || !fileExtra.value) return;
  gallery.register({
    name: fileExtra.value.fileName,
    url,
    type: category.value,
    path: category.value == "image" ? url : thumbnailUrl.value || props.src,
    messageId: props.message?.id,
  });
}

function openGlobalPreview(url = previewUrl.value) {
  if (!url) return;
  gallery?.open(url);
}

function openCachedPreview() {
  if (!previewUrl.value) return false;
  loadProgress.value = 100;
  loadStatus.value = 'ready';
  registerToGlobalGallery(previewUrl.value);
  openGlobalPreview(previewUrl.value);
  return true;
}

function setPreviewUrl(blobUrl: string) {
  loadProgress.value = 100;
  loadStatus.value = 'ready';
  previewUrl.value = blobUrl;
  registerToGlobalGallery(blobUrl);
  openGlobalPreview(blobUrl);
}

function loadInlineMedia() {
  const currentFile = fileExtra.value;
  if (!currentFile?.fileKey) return;

  if (category.value === 'image') {
    const fileKey = currentFile.fileKey;
    previewLoading.value = true;
    requestBlobUrl(fileKey, {
      isNew: props.isNewMessage ?? false,
      priorityId: props.message?.id,
      onComplete: (blobUrl: string) => {
        if (fileExtra.value?.fileKey !== fileKey) return;
        previewUrl.value = blobUrl;
        previewLoading.value = false;
        registerToGlobalGallery(blobUrl);
      },
      onError: (error: Error) => {
        if (fileExtra.value?.fileKey !== fileKey) return;
        handlePreviewError(error);
      },
    });
    return;
  }

  if (category.value === 'video' && currentFile.thumbnailUrl) {
    const thumbnailKey = currentFile.thumbnailUrl;
    thumbnailLoading.value = true;
    requestBlobUrl(thumbnailKey, {
      isNew: props.isNewMessage ?? false,
      priorityId: props.message?.id,
      onComplete: (blobUrl: string) => {
        if (fileExtra.value?.thumbnailUrl !== thumbnailKey) return;
        thumbnailUrl.value = blobUrl;
        thumbnailLoading.value = false;
      },
      onError: (error: Error) => {
        if (fileExtra.value?.thumbnailUrl !== thumbnailKey) return;
        console.error('FileMessage thumbnail failed:', error);
        thumbnailLoading.value = false;
      },
    });
  }
}

function handlePreviewError(error: Error) {
  console.error('FileMessage preview failed:', error);
  previewLoading.value = false;
  hasError.value = true;
  loadStatus.value = 'error';
  notifyError('文件加载失败，请稍后重试');
}

async function triggerPreview() {
  if (!fileExtra.value?.fileKey || previewLoading.value) return;

  if (!isPreviewable.value) {
    previewLoading.value = true;
    hasError.value = false;
    loadProgress.value = 0;
    loadStatus.value = 'loading';
    requestDownload({
      fileKey: fileExtra.value.fileKey,
      category: category.value,
      priorityId: props.message?.id,
      onProgress: (progress: number) => {
        loadProgress.value = progress;
      },
      onComplete: () => {
        previewLoading.value = false;
        loadProgress.value = 100;
        loadStatus.value = 'ready';
        void handleDownload();
      },
      onError: handlePreviewError,
    }, props.isNewMessage ?? true);
    return;
  }

  if (isLargeForPreview.value) {
    notifyError('文件过大，请下载后查看');
    return;
  }

  if (openCachedPreview()) return;

  previewLoading.value = true;
  hasError.value = false;
  loadProgress.value = 0;
  loadStatus.value = 'loading';

  requestDownload({
    fileKey: fileExtra.value.fileKey,
    category: category.value,
    priorityId: props.message?.id,
    thumbnailUrl: category.value === 'video' ? fileExtra.value.thumbnailUrl : undefined,
    onComplete: (blobUrl: string) => {
      previewLoading.value = false;
      setPreviewUrl(blobUrl);
    },
    onThumbnailComplete: (blobUrl: string) => {
      thumbnailUrl.value = blobUrl;
    },
    onProgress: (progress: number) => {
      loadProgress.value = progress;
    },
    onError: handlePreviewError,
  }, props.isNewMessage ?? true);
}

async function handleDownload() {
  if (!fileExtra.value?.fileKey) return;
  await downloadAndSaveFile(fileExtra.value.fileKey, fileExtra.value.fileName);
}

function handleClick() {
  if (!fileExtra.value) return;
  void triggerPreview();
}

const imageStyle = computed(() => {
  if (!fileExtra.value) return {};
  const maxWidth = 300;
  const maxHeight = 300;
  if (fileExtra.value.width && fileExtra.value.height) {
    const ratio = Math.min(maxWidth / fileExtra.value.width, maxHeight / fileExtra.value.height, 1);
    return { width: `${fileExtra.value.width * ratio}px`, height: `${fileExtra.value.height * ratio}px` };
  }
  return { maxWidth: `${maxWidth}px`, maxHeight: `${maxHeight}px` };
});

function handleImageError(event: Event) {
  hasError.value = true;
  const img = event.target as HTMLImageElement;
  img.src = 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="100" height="100"%3E%3Crect fill="%23ddd" width="100" height="100"/%3E%3Ctext x="50%25" y="50%25" text-anchor="middle" dy=".3em" fill="%23999"%3E加载失败%3C/text%3E%3C/svg%3E';
}

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}
</script>

<template>
  <div class="file-message" @click="handleClick">
    <template v-if="fileExtra">
      <div class="file-item-wrap">
        <!-- 图片消息 -->
        <div v-if="category === 'image'" class="image-message">
          <img v-if="previewUrl && !hasError" :src="previewUrl" :alt="fileExtra.fileName" :style="imageStyle"
            @error="handleImageError" loading="lazy" />
          <div v-else-if="previewLoading" class="image-loading">
            <el-icon class="is-loading">
              <Loading />
            </el-icon>
            <span>加载中...</span>
          </div>
          <div v-else-if="hasError" class="image-error-placeholder">
            <el-icon>
              <WarningFilled />
            </el-icon>
            <span>图片加载失败</span>
          </div>
          <div v-else class="image-placeholder">
            <svg-icon icon-class="document-png" size="48px" />
            <span>{{ fileExtra.fileName }}</span>
            <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
          </div>
          <div v-if="showDownload && previewUrl && !hasError" class="image-overlay">
            <el-button size="small" circle @click.stop="handleDownload">
              <el-icon>
                <Download />
              </el-icon>
            </el-button>
          </div>
        </div>

        <!-- 视频消息 -->
        <div v-else-if="category === 'video' && fileExtra" class="video-message">
          <div v-if="thumbnailUrl" class="video-cover">
            <img :src="thumbnailUrl" :alt="fileExtra.fileName" loading="lazy" />
            <div class="video-cover-overlay">
              <el-icon v-if="previewLoading" class="is-loading" :size="28">
                <Loading />
              </el-icon>
              <svg-icon v-else icon-class="general-play" size="32px" />
            </div>
          </div>
          <div v-else-if="thumbnailLoading" class="media-loading">
            <el-icon class="is-loading">
              <Loading />
            </el-icon>
            <span>加载封面中...</span>
          </div>
          <div v-else-if="hasError" class="media-error-placeholder">
            <el-icon>
              <WarningFilled />
            </el-icon>
            <span>视频加载失败</span>
          </div>
          <div v-else class="media-placeholder">
            <svg-icon icon-class="document-video" size="48px" />
            <span>{{ previewLoading ? '加载视频中...' : '点击加载视频' }}</span>
          </div>
          <div class="file-info">
            <div class="file-name">{{ fileExtra.fileName }}</div>
            <div class="file-meta">
              <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
              <span v-if="fileExtra.duration">{{ formatDuration(fileExtra.duration) }}</span>
              <span v-if="loadStatus !== 'idle'" class="status-text" :class="{
                ready: loadStatus === 'ready',
                error: loadStatus === 'error',
              }">
                {{ loadStatus === 'ready' ? '准备就绪' : loadStatus === 'error' ? '加载失败' : `加载中 ${loadProgress}%` }}
              </span>
            </div>
          </div>
        </div>

        <!-- 音频消息 -->
        <div v-else-if="category === 'audio' && fileExtra" class="audio-message">
          <div class="audio-icon">
            <el-icon v-if="previewLoading" class="is-loading" :size="24">
              <Loading />
            </el-icon>
            <el-icon v-else-if="hasError" :size="24">
              <WarningFilled />
            </el-icon>
            <svg-icon v-else icon-class="document-voice" size="48px" />
          </div>
          <div class="audio-content">
            <div class="file-name">{{ fileExtra.fileName }}</div>
            <div class="file-meta">
              <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
              <span v-if="fileExtra.duration">{{ formatDuration(fileExtra.duration) }}</span>
              <span v-if="loadStatus !== 'idle'" class="status-text" :class="{
                ready: loadStatus === 'ready',
                error: loadStatus === 'error',
              }">
                {{ loadStatus === 'ready' ? '准备就绪' : loadStatus === 'error' ? '加载失败' : `加载中 ${loadProgress}%` }}
              </span>
            </div>
          </div>

          <el-button v-if="showDownload" size="small" circle class="download-btn" @click.stop="handleDownload">
            <el-icon>
              <Download />
            </el-icon>
          </el-button>
        </div>

        <!-- 文档和压缩包消息 -->
        <div v-else-if="fileExtra" class="document-message">
          <div class="document-icon" :class="category">
            <svg-icon :icon-class="documentIconName" size="48px" />
          </div>
          <div class="document-info">
            <div class="file-name" :title="fileExtra.fileName">
              {{ fileExtra.fileName }}
            </div>
            <div class="file-meta">
              <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
              <span class="file-type">{{ displayType }}</span>
              <span v-if="loadStatus !== 'idle'" class="status-text" :class="{
                ready: loadStatus === 'ready',
                error: loadStatus === 'error',
              }">
                {{ loadStatus === 'ready' ? '准备就绪' : loadStatus === 'error' ? '加载失败' : `加载中 ${loadProgress}%` }}
              </span>
            </div>
          </div>
          <el-button v-if="showDownload" size="small" circle class="download-btn" @click.stop="handleDownload">
            <el-icon>
              <Download />
            </el-icon>
          </el-button>
        </div>

        <!-- 文件加载进度-->
        <el-progress v-if="loadStatus !== 'idle' && loadStatus === 'loading'" class="file-load-status"
          :percentage="loadProgress" :stroke-width="6" :show-text="false" />
      </div>
    </template>

    <!-- 无效文件信息 -->
    <div v-else class="file-error">
      <el-icon :size="24">
        <WarningFilled />
      </el-icon>
      <span>文件信息无效</span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.file-message {
  max-width: 300px;
  cursor: pointer;
  user-select: none;
}

.file-item-wrap {
  display: inline-flex;
  flex-direction: column;
  align-items: flex-start;
  position: relative;
}

.file-load-status {
  position: absolute;
  /* 子元素绝对定位 */
  top: 0px;
  /* 向上偏移 10px（覆盖父元素顶部） */
  left: 50%;
  transform: translateX(-50%);
  /* 水平居中 */
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.3);
  padding: 12px;
  border-radius: 8px;
}

.image-message {
  position: relative;
  display: inline-block;
  border-radius: 8px;
  overflow: hidden;

  img {
    display: block;
    border-radius: 8px;
    transition: transform 0.2s;
  }

  &:hover .image-overlay {
    opacity: 1;
  }
}

.image-loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px;
  color: #999;
  font-size: 13px;
}

.image-placeholder,
.media-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 180px;
  min-height: 120px;
  padding: 20px;
  color: #777;
  font-size: 13px;
  background: #f5f5f5;
  border-radius: 8px;
  box-sizing: border-box;

  span {
    max-width: 160px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}

.image-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0;
  transition: opacity 0.2s;
}

.image-error-placeholder,
.media-error-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px;
  color: #999;
  font-size: 13px;
  background: #f5f5f5;
  border-radius: 8px;
}

.video-message {
  max-width: 200px;
  border-radius: 8px;
  overflow: hidden;
  background: #f5f5f5;

  .video-cover,
  video {
    width: 100%;
    display: block;
  }

  .video-cover {
    position: relative;
    aspect-ratio: 16 / 9;
    background: #e5e7eb;

    img {
      width: 100%;
      height: 100%;
      display: block;
      object-fit: cover;
    }
  }

  .video-cover-overlay {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #fff;
    background: rgba(0, 0, 0, 0.22);
  }

  .media-loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 60px 40px;
    color: #999;
    font-size: 13px;
  }

  .file-info {
    padding: 8px 12px;

    .file-name {
      font-size: 13px;
      color: #333;
      margin-bottom: 4px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .file-meta {
      font-size: 12px;
      color: #999;
      display: flex;
      gap: 8px;
    }
  }
}

.audio-message {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 8px;

  .audio-icon {
    width: 48px;
    height: 48px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .audio-content {
    flex: 1;
    min-width: 0;

    audio {
      width: 100%;
      height: 32px;
      margin-bottom: 4px;
    }

    .audio-loading,
    .audio-error-placeholder {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 0;
      color: #999;
      font-size: 13px;
    }

    .file-name {
      font-size: 13px;
      color: #333;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .file-meta {
      font-size: 12px;
      color: #999;
      display: flex;
      gap: 8px;
    }
  }
}

.audio-message,
.document-message {
  width: 300px;
}

.document-message {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 8px;
  transition: background-color 0.2s;

  &:hover {
    background: #e8e8e8;
  }

  .document-icon {
    width: 48px;
    height: 48px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;

    &.document {
      background: #e3f2fd;
    }

    &.archive {
      background: #fff3e0;
    }

    &.unknown {
      background: #f5f5f5;
    }

    .icon {
      font-size: 24px;
    }
  }

  .document-info {
    flex: 1;
    min-width: 0;

    .file-name {
      font-size: 14px;
      color: #333;
      margin-bottom: 4px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .file-meta {
      font-size: 12px;
      color: #999;
      display: flex;
      gap: 8px;
      align-items: center;

      .file-type {
        background: #e0e0e0;
        padding: 1px 6px;
        border-radius: 3px;
        font-size: 11px;
      }
    }
  }

  .download-btn {
    flex-shrink: 0;
  }
}

.file-meta {
  .status-text {
    display: inline-block;
    font-size: 12px;
    color: #909399;

    &.ready {
      color: #67c23a;
    }

    &.error {
      color: #f56c6c;
    }
  }
}

.file-error {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 16px;
  color: #f56c6c;
  background: #fef0f0;
  border-radius: 8px;
}
</style>
