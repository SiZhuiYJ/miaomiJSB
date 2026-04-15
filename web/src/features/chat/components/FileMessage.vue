<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { Download, Headset, WarningFilled, Loading } from '@element-plus/icons-vue';
import FilePreview from '@/components/FilePreview/index.vue';
import type { FileExtra, MessageType, MessageSummary } from '../types';
import {
  formatFileSize,
  getFileIcon,
  parseMessageExtra,
} from '../utils/fileHelper';
import { useFileDownloader } from '../composables/useFileDownloader';

const props = defineProps<{
  message?: MessageSummary | null;
  showDownload?: boolean;
  src: string;
  isNewMessage?: boolean;
}>();

const fileExtra = computed<FileExtra | null>(() => {
  return parseMessageExtra(props.message?.extra);
});

const showFile = ref(false);
const hasError = ref(false);

const docType = (mimeType: string) => {
  if (mimeType === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet') return 'excel';
  else if (mimeType === 'application/vnd.ms-excel') return 'xls'
  else if (mimeType === 'application/vnd.openxmlformats-officedocument.wordprocessingml.document') return 'document';
  else if (mimeType === 'application/msword') return 'doc'
  else if (mimeType === 'application/vnd.openxmlformats-officedocument.presentationml.presentation') return 'presentation';
  else if (mimeType === 'application/vnd.ms-powerpoint') return 'ppt'
  else if (mimeType === 'application/pdf') return 'pdf'
  else if (mimeType === 'text/markdown') return 'markdown'
  else if (mimeType === 'text/plain') return 'text';
  else if (mimeType === 'text/html') return 'html';
  else if (mimeType === 'application/json') return 'json';
  else if (mimeType === 'application/xml') return 'xml';
  else if (mimeType === 'application/css') return 'css';
  else if (mimeType === 'application/javascript') return 'javascript';
  else return 'unknown';
}

const category = computed(() => {
  if (!fileExtra.value) return 'unknown';
  const ext = fileExtra.value.mimeType ? fileExtra.value.mimeType.split('/')[0] : '';
  if (fileExtra.value.mimeType) {
    if (ext === 'application') {
      const dt = docType(fileExtra.value.mimeType);
      if (dt !== 'unknown') return dt;
      if (fileExtra.value.mimeType === 'application/zip' || fileExtra.value.mimeType === 'application/x-rar-compressed') {
        return 'archive';
      }
    } else if (ext === 'text') {
      return 'text';
    }
  }
  return ext as MessageType;
});

const imageUrl = ref<string>('');
const mediaUrl = ref<string>('');
const docUrl = ref<string>('');
const textUrl = ref<string>('');
const loading = ref(false);

const { requestDownload, downloadAndSaveFile } = useFileDownloader();

const previewFiles = computed(() => {
  if (!fileExtra.value) return [];
  const url = category.value === 'image' ? imageUrl.value :
    ['video', 'audio'].includes(category.value) ? mediaUrl.value :
      category.value === 'text' ? textUrl.value :
        docUrl.value;
  return [{
    name: fileExtra.value.fileName,
    url: url,
    path: imageUrl.value,
    type: category.value
  }];
});

watch(() => fileExtra.value?.fileKey, (fileKey) => {
  if (!fileKey || !fileExtra.value) {
    return;
  }

  loading.value = true;
  hasError.value = false;
  imageUrl.value = '';
  mediaUrl.value = '';
  docUrl.value = '';
  textUrl.value = '';

  const item = {
    fileKey,
    category: category.value,
    thumbnailUrl: fileExtra.value.thumbnailUrl,
    src: props.src,
    onComplete: (blobUrl: string) => {
      if (category.value === 'image') imageUrl.value = blobUrl;
      else if (['video', 'audio'].includes(category.value)) mediaUrl.value = blobUrl;
      else if (['excel', 'document', 'presentation', 'pdf'].includes(category.value)) docUrl.value = blobUrl;
      else if (category.value === 'text') textUrl.value = blobUrl;
      loading.value = false;
    },
    onThumbnailComplete: (blobUrl: string) => {
      imageUrl.value = blobUrl;
    },
    onError: (error: Error) => {
      loading.value = false;
      hasError.value = true;
      console.error(`FileMessage: Failed to load ${fileKey}`, error);
    }
  };

  requestDownload(item, props.isNewMessage ?? false);

}, { immediate: true });

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

// 💡 This function now uses the central downloader.
async function handleDownload() {
  if (!fileExtra.value || !fileExtra.value.fileKey) return;
  downloadAndSaveFile(fileExtra.value.fileKey, fileExtra.value.fileName);
}

function handleClick() {
  // Only show preview for successfully loaded media/docs
  if (loading.value || hasError.value) return;
  // if (category.value === 'image' || category.value === 'video' || category.value === 'document' || category.value === 'pdf' || category.value === 'text') {
  //   showFile.value = true;
  // }
  if (category.value != 'unknown')
    showFile.value = true;
}
</script>

<template>
  <div class="file-message" @click="handleClick">
    <!-- 图片消息 -->
    <div v-if="category === 'image' && fileExtra" class="image-message">
      <img v-if="imageUrl && !hasError" :src="imageUrl" :alt="fileExtra.fileName" :style="imageStyle"
        @error="handleImageError" loading="lazy" />
      <div v-else-if="loading" class="image-loading">
        <el-icon class="is-loading">
          <Loading />
        </el-icon>
        <span>加载中...</span>
      </div>
      <!-- 💡 Added explicit error state -->
      <div v-else class="image-error-placeholder">
        <el-icon>
          <WarningFilled />
        </el-icon>
        <span>图片加载失败</span>
      </div>
      <div v-if="showDownload && imageUrl && !hasError" class="image-overlay">
        <el-button size="small" circle @click.stop="handleDownload">
          <el-icon>
            <Download />
          </el-icon>
        </el-button>
      </div>
    </div>

    <!-- 视频消息 -->
    <div v-else-if="category === 'video' && fileExtra" class="video-message">
      <video v-if="mediaUrl && !hasError" :src="mediaUrl" controls :poster="imageUrl" preload="metadata">
        您的浏览器不支持视频播放
      </video>
      <div v-else-if="loading" class="media-loading">
        <el-icon class="is-loading">
          <Loading />
        </el-icon>
        <span>加载视频中...</span>
      </div>
      <!-- 💡 Added explicit error state -->
      <div v-else class="media-error-placeholder">
        <el-icon>
          <WarningFilled />
        </el-icon>
        <span>视频加载失败</span>
      </div>
      <div class="file-info">
        <div class="file-name">{{ fileExtra.fileName }}</div>
        <div class="file-meta">
          <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
          <span v-if="fileExtra.duration">{{ formatDuration(fileExtra.duration) }}</span>
        </div>
      </div>
    </div>

    <!-- 音频消息 -->
    <div v-else-if="category === 'audio' && fileExtra" class="audio-message">
      <div class="audio-icon">
        <el-icon :size="32">
          <Headset />
        </el-icon>
      </div>
      <div class="audio-content">
        <audio v-if="mediaUrl && !hasError" :src="mediaUrl" controls preload="metadata">
          您的浏览器不支持音频播放
        </audio>
        <div v-else-if="loading" class="audio-loading">
          <el-icon class="is-loading">
            <Loading />
          </el-icon>
          <span>加载中...</span>
        </div>
        <!-- 💡 Added explicit error state -->
        <div v-else class="audio-error-placeholder">
          <el-icon>
            <WarningFilled />
          </el-icon>
          <span>音频加载失败</span>
        </div>
        <div class="file-name">{{ fileExtra.fileName }}</div>
        <div class="file-meta">
          <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
          <span v-if="fileExtra.duration">{{ formatDuration(fileExtra.duration) }}</span>
        </div>
      </div>
    </div>

    <!-- 文件消息 -->
    <!-- <div v-else-if="category === 'text' && fileExtra" class="text-message">
    </div> -->

    <!-- 文档和压缩包消息 -->
    <div v-else-if="fileExtra" class="document-message">
      <div class="document-icon" :class="category">
        <span class="icon">{{ getFileIcon(category) }}</span>
      </div>
      <div class="document-info">
        <div class="file-name" :title="fileExtra.fileName">
          {{ fileExtra.fileName }}
        </div>
        <div class="file-meta">
          <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
          <span v-if="fileExtra.mimeType" class="file-type">{{ docType(fileExtra.mimeType) || 'FILE' }}</span>
        </div>
      </div>
      <el-button v-if="showDownload" size="small" circle class="download-btn" @click.stop="handleDownload">
        <el-icon>
          <Download />
        </el-icon>
      </el-button>
    </div>

    <!-- 无效文件信息 -->
    <div v-else class="file-error">
      <el-icon :size="24">
        <WarningFilled />
      </el-icon>
      <span>文件信息无效</span>
    </div>
  </div>
  <FilePreview v-model="showFile" :current-index="0" :file-list="previewFiles" :cover-url="previewFiles[0]?.path" />
</template>

<style scoped lang="scss">
.file-message {
  cursor: pointer;
  user-select: none;
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

  video {
    width: 100%;
    display: block;
    // max-height: 300px;
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
  min-width: 280px;

  .audio-icon {
    color: #409eff;
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

.document-message {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: #f5f5f5;
  border-radius: 8px;
  width: 280px;
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
