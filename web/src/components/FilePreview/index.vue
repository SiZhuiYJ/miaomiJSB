<template>
  <Teleport to="body">
    <Transition name="preview-fade">
      <div v-if="visible" class="file-preview-overlay" @click.self="handleClose">
        <!-- 顶部工具栏 -->
        <div class="preview-toolbar">
          <div class="toolbar-left">
            <span class="file-name">{{ currentFile?.name }}</span>
          </div>
          <div class="toolbar-right">
            <button class="toolbar-btn" @click="handleZoomIn" title="放大">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor"
                  d="M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z" />
                <path fill="currentColor" d="M12 10h-2v2H9v-2H7V9h2V7h1v2h2v1z" />
              </svg>
            </button>
            <button class="toolbar-btn" @click="handleZoomOut" title="缩小">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor"
                  d="M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z" />
                <path fill="currentColor" d="M7 9h5v1H7z" />
              </svg>
            </button>
            <button class="toolbar-btn" @click="handleResetZoom" title="重置缩放">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor"
                  d="M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z" />
              </svg>
            </button>
            <button class="toolbar-btn" @click="handleRotate" title="旋转">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor"
                  d="M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z" />
              </svg>
            </button>
            <button class="toolbar-btn" @click="handleDownload" title="下载">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor" d="M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z" />
              </svg>
            </button>
            <button class="toolbar-btn close-btn" @click="handleClose" title="关闭">
              <svg viewBox="0 0 24 24" width="20" height="20">
                <path fill="currentColor"
                  d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z" />
              </svg>
            </button>
          </div>
        </div>

        <!-- 文件列表缩略图 -->
        <div v-if="fileList.length > 1" class="thumbnail-list">
          <button v-for="(file, index) in fileList" :key="index" class="thumbnail-item"
            :class="{ active: currentIndex === index }" @click="handleSwitchFile(index)">
            <img v-if="isImage(file)" :src="file.url || file.path" :alt="file.name" />
            <div v-else class="thumbnail-icon">
              <svg viewBox="0 0 24 24" width="24" height="24">
                <path fill="currentColor"
                  d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6zm4 18H6V4h7v5h5v11z" />
              </svg>
            </div>
          </button>
        </div>

        <!-- 上一张/下一张按钮 -->
        <button v-if="fileList.length > 1 && currentIndex > 0" class="nav-btn prev-btn" @click="handlePrev">
          <svg viewBox="0 0 24 24" width="32" height="32">
            <path fill="currentColor" d="M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z" />
          </svg>
        </button>
        <button v-if="fileList.length > 1 && currentIndex < fileList.length - 1" class="nav-btn next-btn"
          @click="handleNext">
          <svg viewBox="0 0 24 24" width="32" height="32">
            <path fill="currentColor" d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z" />
          </svg>
        </button>

        <!-- 主预览区域 -->
        <div class="preview-content" :style="contentStyle">
          <!-- 图片预览 -->
          <div v-if="currentFileType === 'image'" class="image-preview">
            <img ref="imageRef" :key="currentFile?.url" :src="currentFile?.url || currentFile?.path" :alt="currentFile?.name"
              :style="imageStyle" @load="handleImageLoad" />
          </div>

          <!-- 视频预览 -->
          <div v-else-if="currentFileType === 'video'" class="video-preview">
            <video ref="videoRef" :key="currentFile?.url" :src="currentFile?.url || currentFile?.path" controls autoplay preload="metadata"
              @loadeddata="handleVideoLoaded">
              您的浏览器不支持视频播放
            </video>
          </div>

          <!-- 音频预览 -->
          <div v-else-if="currentFileType === 'audio'" class="audio-preview">
            <MiniAudioPlayer v-if="currentFile?.url" :key="currentFile.url" :url="currentFile.url" :title="currentFile?.name"
              :cover-url="coverUrl" @loaded="handleAudioLoaded" />
          </div>

          <!-- PDF预览 -->
          <div v-else-if="currentFileType === 'pdf'" class="pdf-preview">
            <iframe :src="currentFile?.url || currentFile?.path" frameborder="0" class="pdf-frame"></iframe>
          </div>

          <!-- Office文档预览 (使用Microsoft Online Viewer或Google Docs Viewer) -->
          <div v-else-if="currentFileType === 'document'" class="document-preview">
            <iframe :src="getDocumentViewerUrl(currentFile?.url || currentFile?.path || '')" frameborder="0"
              class="document-frame"></iframe>
          </div>

          <!-- 不支持的文件类型 -->
          <div v-else class="unsupported-preview">
            <div class="unsupported-icon">
              <svg viewBox="0 0 24 24" width="120" height="120">
                <path fill="currentColor"
                  d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 15h8v2H8v-2zm0-4h8v2H8v-2z" />
              </svg>
            </div>
            <p>该文件类型暂不支持在线预览</p>
            <button class="download-btn" @click="handleDownload">
              下载文件
            </button>
          </div>
        </div>

        <!-- 加载状态 -->
        <div v-if="loading" class="loading-overlay">
          <div class="loading-spinner"></div>
          <p>加载中...</p>
        </div>

        <!-- 错误提示 -->
        <div v-if="error" class="error-overlay">
          <div class="error-icon">
            <svg viewBox="0 0 24 24" width="64" height="64">
              <path fill="currentColor"
                d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z" />
            </svg>
          </div>
          <p>{{ error }}</p>
          <button class="retry-btn" @click="handleRetry">重试</button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { MiniAudioPlayer } from '../MiniAudioPlayer/index'

interface FileItem {
  name: string
  url?: string
  path?: string
  type?: string
}

interface Props {
  modelValue: boolean
  fileList: FileItem[]
  currentIndex?: number
  coverUrl?: string
}

const props = withDefaults(defineProps<Props>(), {
  currentIndex: 0,
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'update:currentIndex': [index: number]
  close: []
}>()

// 状态管理
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const currentIndex = computed({
  get: () => props.currentIndex,
  set: (index) => emit('update:currentIndex', index)
})

const currentFile = computed(() => props.fileList[currentIndex.value])
const loading = ref(false)
const error = ref('')

// 缩放和旋转状态
const scale = ref(1)
const rotate = ref(0)

// 元素引用
const imageRef = ref<HTMLImageElement>()
const videoRef = ref<HTMLVideoElement>()

// 获取文件类型
const getFileType = (file: FileItem): string => {
  if (file.type) return file.type

  const ext = file.name.split('.').pop()?.toLowerCase() || ''

  const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp', 'svg', 'ico']
  const videoExts = ['mp4', 'webm', 'ogg', 'mov', 'avi', 'mkv', 'flv']
  const audioExts = ['mp3', 'wav', 'ogg', 'aac', 'flac', 'wma']
  const pdfExts = ['pdf']
  const docExts = ['doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt', 'csv']

  if (imageExts.includes(ext)) return 'image'
  if (videoExts.includes(ext)) return 'video'
  if (audioExts.includes(ext)) return 'audio'
  if (pdfExts.includes(ext)) return 'pdf'
  if (docExts.includes(ext)) return 'document'

  return 'unknown'
}

const currentFileType = computed(() => {
  if (!currentFile.value) return 'unknown'
  return getFileType(currentFile.value)
})

// 判断是否为图片
const isImage = (file: FileItem) => getFileType(file) === 'image'

// 获取文档查看器URL
const getDocumentViewerUrl = (url: string) => {
  // 使用Microsoft Office Online Viewer
  return `https://view.officeapps.live.com/op/embed.aspx?src=${encodeURIComponent(url)}`
}

// 图片样式
const imageStyle = computed(() => ({
  transform: `scale(${scale.value}) rotate(${rotate.value}deg)`,
  transition: 'transform 0.3s ease'
}))

// 内容区域样式
const contentStyle = computed(() => ({
  cursor: scale.value > 1 ? 'grab' : 'default'
}))

// 处理图片加载
const handleImageLoad = () => {
  loading.value = false
}

// 处理音频加载完成
const handleAudioLoaded = () => {
  loading.value = false
}

// 处理视频加载完成
const handleVideoLoaded = () => {
  loading.value = false
}

// 缩放控制
const handleZoomIn = () => {
  scale.value = Math.min(scale.value + 0.2, 5)
}

const handleZoomOut = () => {
  scale.value = Math.max(scale.value - 0.2, 0.2)
}

const handleResetZoom = () => {
  scale.value = 1
  rotate.value = 0
}

// 旋转控制
const handleRotate = () => {
  rotate.value = (rotate.value + 90) % 360
}

// 导航控制
const handlePrev = () => {
  if (currentIndex.value > 0) {
    currentIndex.value--
    resetPreview()
  }
}

const handleNext = () => {
  if (currentIndex.value < props.fileList.length - 1) {
    currentIndex.value++
    resetPreview()
  }
}

const handleSwitchFile = (index: number) => {
  currentIndex.value = index
  resetPreview()
}

// 重置预览状态
const resetPreview = () => {
  scale.value = 1
  rotate.value = 0
  loading.value = true
  error.value = ''
}

// 关闭预览
const handleClose = () => {
  visible.value = false
  emit('close')
  // 暂停视频播放
  videoRef.value?.pause()
}

// 下载文件
const handleDownload = () => {
  if (!currentFile.value) return

  const url = currentFile.value.url || currentFile.value.path
  if (!url) return

  const link = document.createElement('a')
  link.href = url
  link.download = currentFile.value.name
  link.click()
}

// 重试
const handleRetry = () => {
  error.value = ''
  loading.value = true
  // 重新加载当前文件
  if (imageRef.value) {
    imageRef.value.src = ''
    setTimeout(() => {
      if (imageRef.value && currentFile.value) {
        imageRef.value.src = currentFile.value.url || currentFile.value.path || ''
      }
    }, 100)
  }
}

// 键盘事件处理
const handleKeyDown = (e: KeyboardEvent) => {
  if (!visible.value) return

  switch (e.key) {
    case 'Escape':
      handleClose()
      break
    case 'ArrowLeft':
      handlePrev()
      break
    case 'ArrowRight':
      handleNext()
      break
    case '+':
    case '=':
      handleZoomIn()
      break
    case '-':
      handleZoomOut()
      break
    case '0':
      handleResetZoom()
      break
    case 'r':
    case 'R':
      handleRotate()
      break
  }
}

// 监听文件变化
watch(() => props.currentIndex, () => {
  resetPreview()
})

// 监听当前文件变化，确保 loading 状态正确
watch(currentFile, (newFile) => {
  if (newFile) {
    // 对于音频和视频，需要等待加载完成事件
    const type = getFileType(newFile)
    if (type === 'audio' || type === 'video') {
      loading.value = true
    } else if (type === 'image') {
      loading.value = true
    } else {
      // 其他类型（PDF、文档等）立即隐藏 loading
      loading.value = false
    }
  }
})

// 生命周期
onMounted(() => {
  document.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeyDown)
})
</script>

<style scoped lang="scss">
.file-preview-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 9999;
  background-color: rgba(0, 0, 0, 0.9);
  display: flex;
  flex-direction: column;
}

// 顶部工具栏
.preview-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 20px;
  background-color: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(10px);
  color: #fff;
  z-index: 10;

  .toolbar-left {
    .file-name {
      font-size: 14px;
      max-width: 400px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
  }

  .toolbar-right {
    display: flex;
    gap: 8px;
  }

  .toolbar-btn {
    width: 36px;
    height: 36px;
    border: none;
    background-color: rgba(255, 255, 255, 0.1);
    color: #fff;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.2s;

    &:hover {
      background-color: rgba(255, 255, 255, 0.2);
      transform: scale(1.05);
    }

    &.close-btn:hover {
      background-color: rgba(255, 0, 0, 0.3);
    }
  }
}

// 缩略图列表
.thumbnail-list {
  display: flex;
  gap: 8px;
  padding: 10px 20px;
  background-color: rgba(0, 0, 0, 0.5);
  overflow-x: auto;
  z-index: 10;

  &::-webkit-scrollbar {
    height: 6px;
  }

  &::-webkit-scrollbar-thumb {
    background-color: rgba(255, 255, 255, 0.3);
    border-radius: 3px;
  }

  .thumbnail-item {
    flex-shrink: 0;
    width: 60px;
    height: 60px;
    border: 2px solid transparent;
    border-radius: 4px;
    overflow: hidden;
    cursor: pointer;
    transition: all 0.2s;
    background-color: rgba(255, 255, 255, 0.1);

    &:hover {
      border-color: rgba(255, 255, 255, 0.5);
      transform: scale(1.05);
    }

    &.active {
      border-color: #409eff;
    }

    img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .thumbnail-icon {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: rgba(255, 255, 255, 0.6);
    }
  }
}

// 导航按钮
.nav-btn {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  width: 50px;
  height: 50px;
  border: none;
  background-color: rgba(255, 255, 255, 0.1);
  color: #fff;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  z-index: 10;

  &:hover {
    background-color: rgba(255, 255, 255, 0.2);
    transform: translateY(-50%) scale(1.1);
  }

  &.prev-btn {
    left: 20px;
  }

  &.next-btn {
    right: 20px;
  }
}

// 主预览区域
.preview-content {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  // overflow: auto;
  position: relative;

  // 隐藏滚动条但保留滚动功能
  scrollbar-width: none; // Firefox
  -ms-overflow-style: none; // IE/Edge

  &::-webkit-scrollbar {
    display: none; // Chrome/Safari
  }
}

// 图片预览
.image-preview {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;

  img {
    max-width: 90%;
    max-height: 90%;
    object-fit: contain;
    user-select: none;
  }
}

// 视频预览
.video-preview {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;

  video {
    width: 100%;
    height: 100%;
    outline: none;
  }
}

// 音频预览
.audio-preview {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
}

// PDF预览
.pdf-preview {
  width: 100%;
  height: 100%;

  .pdf-frame {
    width: 100%;
    height: 100%;
    border: none;
  }
}

// 文档预览
.document-preview {
  width: 100%;
  height: 100%;

  .document-frame {
    width: 100%;
    height: 100%;
    border: none;
  }
}

// 不支持的文件类型
.unsupported-preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 20px;
  color: #fff;

  .unsupported-icon {
    color: rgba(255, 255, 255, 0.4);
  }

  p {
    font-size: 16px;
    margin: 0;
  }

  .download-btn {
    padding: 12px 30px;
    border: none;
    background-color: #409eff;
    color: #fff;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    transition: all 0.2s;

    &:hover {
      background-color: #66b1ff;
      transform: translateY(-2px);
    }
  }
}

// 加载状态
.loading-overlay {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  color: #fff;
  z-index: 20;

  .loading-spinner {
    width: 50px;
    height: 50px;
    border: 4px solid rgba(255, 255, 255, 0.2);
    border-top-color: #fff;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }

  p {
    font-size: 14px;
    margin: 0;
  }
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

// 错误提示
.error-overlay {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  color: #fff;
  z-index: 20;

  .error-icon {
    color: #f56c6c;
  }

  p {
    font-size: 14px;
    margin: 0;
  }

  .retry-btn {
    padding: 8px 24px;
    border: none;
    background-color: #409eff;
    color: #fff;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    transition: all 0.2s;

    &:hover {
      background-color: #66b1ff;
    }
  }
}

// 过渡动画
.preview-fade-enter-active,
.preview-fade-leave-active {
  transition: opacity 0.3s ease;
}

.preview-fade-enter-from,
.preview-fade-leave-to {
  opacity: 0;
}
</style>
