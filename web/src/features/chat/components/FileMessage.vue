<template>
  <div class="file-message" @click="handleClick">
    <!-- 图片消息 -->
    <div v-if="category === 'image' && fileExtra" class="image-message">
      <img v-if="imageUrl" :src="imageUrl" :alt="fileExtra.fileName" :style="imageStyle" @error="handleImageError"
        loading="lazy" />
      <div v-else class="image-loading">
        <el-icon class="is-loading">
          <Loading />
        </el-icon>
        <span>加载中...</span>
      </div>
      <div v-if="showDownload && imageUrl" class="image-overlay">
        <el-button size="small" circle @click.stop="downloadFile">
          <el-icon>
            <Download />
          </el-icon>
        </el-button>
      </div>
    </div>

    <!-- 视频消息 -->
    <div v-else-if="category === 'video' && fileExtra" class="video-message">
      <video v-if="mediaUrl" :src="mediaUrl" controls preload="metadata">
        您的浏览器不支持视频播放
      </video>
      <div v-else class="media-loading">
        <el-icon class="is-loading">
          <Loading />
        </el-icon>
        <span>加载视频中...</span>
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
        <audio v-if="mediaUrl" :src="mediaUrl" controls preload="metadata">
          您的浏览器不支持音频播放
        </audio>
        <div v-else class="audio-loading">
          <el-icon class="is-loading">
            <Loading />
          </el-icon>
          <span>加载中...</span>
        </div>
        <div class="file-name">{{ fileExtra.fileName }}</div>
        <div class="file-meta">
          <span>{{ formatFileSize(fileExtra.fileSize) }}</span>
          <span v-if="fileExtra.duration">{{ formatDuration(fileExtra.duration) }}</span>
        </div>
      </div>
    </div>

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
          <span class="file-type">{{ fileExtra.mimeType?.split('/')[1]?.toUpperCase() || 'FILE' }}</span>
        </div>
      </div>
      <el-button v-if="showDownload" size="small" circle class="download-btn" @click.stop="downloadFile">
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
</template>

<script setup lang="ts">
import { computed, ref, watch, onBeforeUnmount } from 'vue';
import { Download, Headset, WarningFilled, Loading } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import type { FileExtra, MessageType } from '../types';
import {
  getFileCategoryByName,
  formatFileSize,
  getFileIcon,
  parseMessageExtra
} from '../utils/fileHelper';
import http from '@/libs/http'; 

const props = defineProps<{
  extra?: string | null;
  showDownload?: boolean;
}>();

const fileExtra = computed<FileExtra | null>(() => {
  // console.log(props.extra)
  return parseMessageExtra(props.extra);
});

const category = computed(() => {
  console.log(fileExtra.value)
  if (!fileExtra.value) return 'unknown';
  console.log(fileExtra.value.mimeType)
  const ext = fileExtra.value.mimeType
    ? fileExtra.value.mimeType.split('/')[0]
    : '';
  console.log(ext)
  return ext as MessageType;
});

// Blob URL管理
const imageUrl = ref<string>('');
const mediaUrl = ref<string>('');
const loadingMedia = ref(false);

// 获取认证头
function getAuthHeaders(): Record<string, string> {
  const token = localStorage.getItem('accessToken');
  return token ? { 'Authorization': `Bearer ${token}` } : {};
}

// 获取不带扩展名的fileKey
function getFileKeyWithoutExt(fileKey: string): string {
  const lastDot = fileKey.lastIndexOf('.');
  return lastDot > 0 ? fileKey.substring(0, lastDot) : fileKey;
}

// 加载图片Blob
async function loadImageBlob() {
  if (category.value !== 'image' || !fileExtra.value?.fileKey) {
    imageUrl.value = '';
    return;
  }

  try {
    loadingMedia.value = true;
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '';
    const normalizedBaseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    const url = `${normalizedBaseUrl}/mm/files/chat/${fileExtra.value.fileKey}`;

    const response = await http.get(url, {
      responseType: 'blob',
      headers: getAuthHeaders()
    });

    const blob = new Blob([response.data as BlobPart]);
    imageUrl.value = URL.createObjectURL(blob);
  } catch (error) {
    console.error('加载图片失败:', error);
    imageUrl.value = '';
    ElMessage.error('图片加载失败');
  } finally {
    loadingMedia.value = false;
  }
}

// 加载媒体Blob（视频/音频）
async function loadMediaBlob() {
  if (!['video', 'audio'].includes(category.value) || !fileExtra.value?.fileKey) {
    mediaUrl.value = '';
    return;
  }

  try {
    loadingMedia.value = true;
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '';
    const normalizedBaseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    // const cleanFileKey = getFileKeyWithoutExt(fileExtra.value.fileKey);
    const url = `${normalizedBaseUrl}/mm/files/chat/${fileExtra.value.fileKey}`;

    const response = await http.get(url, {
      responseType: 'blob',
      headers: getAuthHeaders()
    });

    const blob = new Blob([response.data as BlobPart]);
    mediaUrl.value = URL.createObjectURL(blob);
  } catch (error) {
    console.error('加载媒体失败:', error);
    mediaUrl.value = '';
    ElMessage.error('媒体加载失败');
  } finally {
    loadingMedia.value = false;
  }
}

// 监听fileExtra变化，加载对应的Blob
watch([() => fileExtra.value?.fileKey, category], () => {
  if (category.value === 'image') {
    loadImageBlob();
  } else if (['video', 'audio'].includes(category.value)) {
    loadMediaBlob();
  }
}, { immediate: true });

// 组件卸载时清理Blob URL
onBeforeUnmount(() => {
  if (imageUrl.value) {
    URL.revokeObjectURL(imageUrl.value);
  }
  if (mediaUrl.value) {
    URL.revokeObjectURL(mediaUrl.value);
  }
});

const imageStyle = computed(() => {
  if (!fileExtra.value) return {};

  const maxWidth = 300;
  const maxHeight = 300;

  if (fileExtra.value.width && fileExtra.value.height) {
    const ratio = Math.min(
      maxWidth / fileExtra.value.width,
      maxHeight / fileExtra.value.height,
      1
    );
    return {
      width: `${fileExtra.value.width * ratio}px`,
      height: `${fileExtra.value.height * ratio}px`
    };
  }

  return {
    maxWidth: `${maxWidth}px`,
    maxHeight: `${maxHeight}px`
  };
});

function handleImageError(event: Event) {
  const img = event.target as HTMLImageElement;
  img.src = 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="100" height="100"%3E%3Crect fill="%23ddd" width="100" height="100"/%3E%3Ctext x="50%25" y="50%25" text-anchor="middle" dy=".3em" fill="%23999"%3E加载失败%3C/text%3E%3C/svg%3E';
}

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

async function downloadFile() {
  if (!fileExtra.value || !fileExtra.value.fileKey) return;

  try {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '';
    const normalizedBaseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    const cleanFileKey = getFileKeyWithoutExt(fileExtra.value.fileKey);
    const url = `${normalizedBaseUrl}/mm/files/chat/${cleanFileKey}`;

    // 方案: 直接使用 window.open 或创建隐藏 iframe 下载
    // 这样可以避免创建 blob URL,彻底消除混合内容警告
    const token = localStorage.getItem('accessToken');

    // 创建一个隐藏的 form 来提交下载请求
    const form = document.createElement('form');
    form.method = 'GET';
    form.action = url;
    form.style.display = 'none';

    // 如果有认证token,通过 URL 参数传递(如果后端支持)
    // 或者使用 fetch + blob 方式
    if (token) {
      // 使用 fetch 获取文件并触发下载
      const response = await fetch(url, {
        method: 'GET',
        headers: getAuthHeaders(),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      // 从响应头获取文件名和类型
      const contentDisposition = response.headers.get('Content-Disposition');
      let fileName = fileExtra.value.fileName;
      if (contentDisposition) {
        const filenameMatch = contentDisposition.match(/filename\*?=(?:UTF-8'')?([^;]+)/i);
        if (filenameMatch && filenameMatch[1]) {
          fileName = decodeURIComponent(filenameMatch[1].trim());
        }
      }

      const blob = await response.blob();

      // 检查浏览器是否支持 msSaveOrOpenBlob (IE/Edge)
      if ((window.navigator as any).msSaveOrOpenBlob) {
        (window.navigator as any).msSaveOrOpenBlob(blob, fileName);
        ElMessage.success('下载成功');
        return;
      }

      // 现代浏览器:创建临时 Object URL
      const objectUrl = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = objectUrl;
      link.download = fileName;
      link.style.display = 'none';
      document.body.appendChild(link);

      // 同步触发点击
      link.click();

      // 异步清理资源
      setTimeout(() => {
        document.body.removeChild(link);
        // 给浏览器足够时间开始下载后再释放 URL
        setTimeout(() => {
          URL.revokeObjectURL(objectUrl);
        }, 1000);
      }, 100);
    } else {
      // 无认证:直接打开链接
      window.open(url, '_blank');
    }

    ElMessage.success('下载成功');
  } catch (error) {
    console.error('Download error:', error);
    ElMessage.error(`下载失败: ${(error as Error).message}`);
  }
}

function handleClick() {
  // 可以在这里添加点击预览功能
  if (category.value === 'image') {
    // TODO: 实现图片大图预览
  }
}
</script>

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

.video-message {
  max-width: 400px;
  border-radius: 8px;
  overflow: hidden;
  background: #f5f5f5;

  video {
    width: 100%;
    display: block;
    max-height: 300px;
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

    .audio-loading {
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
  min-width: 280px;
  max-width: 400px;
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
