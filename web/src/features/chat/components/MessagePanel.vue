<!-- MessagePanel.vue -->
<script setup lang="ts">
import { nextTick, watch, useTemplateRef, computed, provide, ref } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia';
import { Document, UploadFilled } from '@element-plus/icons-vue';
import MessageItem from './MessageItem.vue';
import FileUploadButton from './FileUploadButton.vue';
import ConversationDetailDialog from './ConversationDetailDialog.vue';
import type { ConversationDetail, MessageSummary, MessageReadStatus, SendMessagePayload } from '../types';
import {
  formatDateSeparator,
  getMemberAvatarBySender,
  getConversationDisplayTitle,
} from '../utils/chat';
import {
  formatFileSize,
  getFilePreviewType,
  prepareFilesForMessageUpload,
  uploadFileForMessage,
  validateFile,
} from '../utils/fileHelper';

const model = defineModel<string>({ default: '' });
const currentConversation = defineModel<ConversationDetail>('conversationDetail');

import FilePreview from '@/components/FilePreview/index.vue';

interface GalleryFile {
  name: string;
  url: string;
  type: string;
  path?: string;
  messageId?: number;
}

// 全局画廊状态
const galleryVisible = ref(false);
const galleryFileList = ref<GalleryFile[]>([]);
const galleryIndex = ref(0);

// 注册文件到画廊
function registerFileToGallery(file: GalleryFile) {
  const existingIndex = galleryFileList.value.findIndex(f => f.url === file.url);
  if (existingIndex >= 0) {
    const merged = { ...galleryFileList.value[existingIndex], ...file };
    galleryFileList.value.splice(existingIndex, 1);
    insertGalleryFile(merged);
    return;
  }
  insertGalleryFile(file);
}

function insertGalleryFile(file: GalleryFile) {
  const targetMessageId = file.messageId ?? -1;
  const insertAt = galleryFileList.value.findIndex((item) => (item.messageId ?? -1) < targetMessageId);
  if (insertAt === -1) {
    galleryFileList.value.push(file);
    return;
  }
  galleryFileList.value.splice(insertAt, 0, file);
}

// 打开画廊并定位到指定文件
function openGallery(fileUrl?: string) {
  if (fileUrl) {
    const index = galleryFileList.value.findIndex(f => f.url === fileUrl);
    galleryIndex.value = index >= 0 ? index : 0;
  }
  galleryVisible.value = true;
}

// 通过 provide 让子组件（FileMessage）能够调用注册和打开方法
provide('gallery', {
  register: registerFileToGallery,
  open: openGallery,
});

const props = defineProps<{
  messages: MessageSummary[];
  meUserId?: number;
  loading: boolean;
  showBackToList?: boolean;
  messageReadStatus?: Map<number, MessageReadStatus>;
  readInfoMap?: Map<number, { readText: string; readColor: string }>; // 由 useChatCore 提供
}>();

const emit = defineEmits<{
  loadMore: [];
  sendTextMessage: [];
  sendMessage: [payload: SendMessagePayload];
  markRead: [];
  backToList: [];
  updateConversation: [];
  loadMessageReadStatus: [messageId: number];
}>();

const { user } = storeToRefs(useAuthStore());
const scrollbarRef = useTemplateRef('scrollbarRef');
const isChatDetail = ref(false);
const pendingAutoScroll = ref(true);
const uploadingFiles = ref(false);
const isFileDragActive = ref(false);
const dropUploadDialogVisible = ref(false);
const droppedFiles = ref<File[]>([]);
const preparingDroppedFiles = ref(false);

// 会话标题
const conversationHeaderTitle = computed(() =>
  currentConversation.value
    ? getConversationDisplayTitle(currentConversation.value, user.value?.userId)
    : ''
);

// 新增：按日期分组的消息列表
interface MessageGroup {
  dateLabel: string;
  messages: MessageSummary[];
}

interface DroppedFileItem {
  file: File;
  name: string;
  sizeText: string;
  typeText: string;
  valid: boolean;
  error?: string;
}

const FILE_TYPE_LABELS: Record<ReturnType<typeof getFilePreviewType>, string> = {
  image: '图片',
  video: '视频',
  audio: '音频',
  word: 'Word 文档',
  excel: 'Excel 表格',
  pptx: 'PPT 演示',
  pdf: 'PDF',
  text: '文本',
  doc: 'Word 文档',
  xls: 'Excel 表格',
  ppt: 'PPT 演示',
  archive: '压缩包',
  unknown: '文件',
};

const groupedMessages = computed<MessageGroup[]>(() => {
  const groups: MessageGroup[] = [];
  let currentDateLabel = '';
  let currentGroup: MessageSummary[] = [];

  for (const msg of props.messages) {
    const msgDate = new Date(msg.createdAt);
    const dateLabel = formatDateSeparator(msgDate);

    if (dateLabel !== currentDateLabel) {
      if (currentGroup.length > 0) {
        groups.push({ dateLabel: currentDateLabel, messages: currentGroup });
      }
      currentDateLabel = dateLabel;
      currentGroup = [msg];
    } else {
      currentGroup.push(msg);
    }
  }

  if (currentGroup.length > 0) {
    groups.push({ dateLabel: currentDateLabel, messages: currentGroup });
  }

  return groups;
});

const showDropOverlay = computed(() =>
  Boolean(currentConversation.value) &&
  isFileDragActive.value &&
  !dropUploadDialogVisible.value
);

const droppedFileItems = computed<DroppedFileItem[]>(() =>
  droppedFiles.value.map((file) => {
    const validation = validateFile(file);
    const previewType = getFilePreviewType({
      fileName: file.name,
      mimeType: file.type,
    });

    return {
      file,
      name: file.name,
      sizeText: formatFileSize(file.size),
      typeText: FILE_TYPE_LABELS[previewType] || '文件',
      valid: validation.valid,
      error: validation.error,
    };
  })
);

const validDroppedFiles = computed(() =>
  droppedFileItems.value.filter((item) => item.valid).map((item) => item.file)
);

const invalidDroppedFileCount = computed(() =>
  droppedFileItems.value.filter((item) => !item.valid).length
);

const droppedFileTotalSize = computed(() =>
  formatFileSize(droppedFiles.value.reduce((total, file) => total + file.size, 0))
);

const dropUploadBusy = computed(() =>
  preparingDroppedFiles.value || uploadingFiles.value
);

const canConfirmDropUpload = computed(() =>
  validDroppedFiles.value.length > 0 && !dropUploadBusy.value && !props.loading
);


function getScrollWrap() {
  return (scrollbarRef.value?.wrapRef as HTMLElement | undefined) ?? null;
}

function isLatestMessageInView(threshold = 24) {
  const wrap = getScrollWrap();
  if (!wrap) return true;
  return wrap.scrollHeight - (wrap.scrollTop + wrap.clientHeight) <= threshold;
}

function scrollToBottom(behavior: ScrollBehavior = 'auto') {
  const wrap = getScrollWrap();
  if (!wrap) return;
  wrap.scrollTo({ top: wrap.scrollHeight, behavior });
}

// 监听消息变化，自动滚动
watch(
  () => props.messages.length,
  async (newLength, oldLength) => {
    if (newLength > oldLength) {
      pendingAutoScroll.value = isLatestMessageInView();
    }
    await nextTick();
    if (newLength > oldLength && pendingAutoScroll.value) {
      scrollToBottom('smooth');
    }
  }
);

watch(
  () => currentConversation.value?.id,
  async () => {
    dropUploadDialogVisible.value = false;
    droppedFiles.value = [];
    resetFileDragState();
    await nextTick();
    scrollToBottom();
  }
);

// 消息进入视图时加载已读状态
function onMessageVisible(entry: IntersectionObserverEntry) {
  const messageId = Number((entry.target as HTMLElement).dataset.messageId);
  if (!Number.isFinite(messageId)) return;
  if (!props.messageReadStatus?.has(messageId)) {
    emit('loadMessageReadStatus', messageId);
  }
}

// 获取消息的已读展示信息（优先使用预计算的 readInfoMap）
function getReadDisplay(messageId: number) {
  if (props.readInfoMap) {
    return props.readInfoMap.get(messageId) || { readText: '', readColor: '#909399' };
  }
  // 降级处理（兼容旧用法）
  const status = props.messageReadStatus?.get(messageId);
  if (!status || status.totalRecipients === 0) return { readText: '', readColor: '#909399' };
  const isAllRead = status.readCount >= status.totalRecipients;
  return {
    readText: `${status.readCount}/${status.totalRecipients} 已读`,
    readColor: isAllRead ? '#67C23A' : '#909399',
  };
}

function hasDraggedFiles(event: DragEvent) {
  return Array.from(event.dataTransfer?.types || []).includes('Files');
}

function resetFileDragState() {
  isFileDragActive.value = false;
}

function handleDragEnter(event: DragEvent) {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  event.stopPropagation();
  if (!currentConversation.value || dropUploadDialogVisible.value) return;
  isFileDragActive.value = true;
}

function handleDragOver(event: DragEvent) {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  event.stopPropagation();
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'copy';
  }
  if (currentConversation.value && !dropUploadDialogVisible.value) {
    isFileDragActive.value = true;
  }
}

function handleDragLeave(event: DragEvent) {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  event.stopPropagation();
  if (!currentConversation.value) return;

  const currentTarget = event.currentTarget as HTMLElement | null;
  const nextTarget = event.relatedTarget as Node | null;
  if (currentTarget && nextTarget && currentTarget.contains(nextTarget)) return;

  resetFileDragState();
}

function handleDrop(event: DragEvent) {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  event.stopPropagation();
  resetFileDragState();

  if (!currentConversation.value?.id) {
    ElMessage.warning('请先选择一个会话');
    return;
  }

  if (uploadingFiles.value || props.loading) {
    ElMessage.warning('正在发送文件，请稍后再试');
    return;
  }

  const files = Array.from(event.dataTransfer?.files || []);
  if (files.length === 0) return;

  droppedFiles.value = files;
  dropUploadDialogVisible.value = true;
}

function closeDropUploadDialog() {
  if (dropUploadBusy.value) return;
  dropUploadDialogVisible.value = false;
}

function clearDroppedFiles() {
  if (dropUploadBusy.value) return;
  droppedFiles.value = [];
}

async function confirmDropUpload() {
  if (!currentConversation.value?.id) {
    ElMessage.warning('请先选择一个会话');
    return;
  }

  if (validDroppedFiles.value.length === 0) {
    ElMessage.warning('没有可上传的文件');
    return;
  }

  preparingDroppedFiles.value = true;
  try {
    const {
      files,
      invalidFiles,
      failedFiles,
    } = await prepareFilesForMessageUpload(validDroppedFiles.value);

    invalidFiles.forEach(({ name, error }) => {
      ElMessage.warning(`${name}: ${error}`);
    });
    failedFiles.forEach(({ name }) => {
      ElMessage.error(`${name}: 处理失败`);
    });

    if (files.length === 0) {
      ElMessage.warning('没有可上传的文件');
      return;
    }

    dropUploadDialogVisible.value = false;
    droppedFiles.value = [];
    await handleFileSelected(files);
  } finally {
    preparingDroppedFiles.value = false;
  }
}

// 处理文件选择
async function handleFileSelected(files: File[]) {
  if (!currentConversation.value?.id) {
    ElMessage.error('请先选择一个会话');
    return;
  }

  uploadingFiles.value = true;
  let successCount = 0;
  let failedCount = 0;

  try {
    for (const file of files) {
      try {
        const { extra, messageType } = await uploadFileForMessage(
          currentConversation.value.id,
          file
        );

        // 发送消息
        const payload: SendMessagePayload = {
          messageType: messageType,
          extra: extra
        };

        emit('sendMessage', payload);
        successCount++;
      } catch (error: unknown) {
        failedCount++;
        console.error(`Failed to upload file ${file.name}:`, error);
        const message = error instanceof Error ? error.message : '上传失败';
        ElMessage.error(`${file.name}: ${message}`);
      }
    }
    if (successCount > 0 && failedCount === 0) {
      ElMessage.success(files.length === 1 ? '文件发送成功' : `${successCount} 个文件发送成功`);
    } else if (successCount > 0) {
      ElMessage.warning(`${successCount} 个文件发送成功，${failedCount} 个失败`);
    }
  } catch (error: unknown) {
    console.error('File upload error:', error);
    ElMessage.error('文件发送失败');
  } finally {
    uploadingFiles.value = false;
  }
}
</script>

<template>
  <main class="main-panel" @dragenter="handleDragEnter" @dragover="handleDragOver" @dragleave="handleDragLeave"
    @drop="handleDrop">
    <div v-if="currentConversation" class="conversation-detail">
      <el-scrollbar ref="scrollbarRef" view-class="message-list-container">
        <div class="message-list" v-if="props.meUserId">
          <!-- 遍历分组 -->
          <template v-for="group in groupedMessages" :key="group.dateLabel">
            <!-- 日期分隔线 -->

            <div class="date-separator"> <el-divider border-style="dashed">{{ group.dateLabel }}</el-divider></div>
            <!-- 分组内的消息 -->
            <MessageItem v-for="msg in group.messages" :key="msg.id" v-viewport="onMessageVisible"
              :data-message-id="msg.id" :message="msg"
              :src="getMemberAvatarBySender(currentConversation, msg.senderUserId)" :meUserId="props.meUserId"
              :is-mine="msg.senderUserId === props.meUserId" v-bind="getReadDisplay(msg.id)" />
          </template>
        </div>
      </el-scrollbar>

      <div v-if="showDropOverlay" class="drop-upload-overlay">
        <div class="drop-upload-indicator">
          <el-icon :size="44">
            <UploadFilled />
          </el-icon>
          <div class="drop-upload-title">松开发送文件</div>
        </div>
      </div>

      <div class="chat-header">
        <div class="chat-header-main">
          <el-button v-if="props.showBackToList" color="#111827" class="back-to-list" @click="emit('backToList')">
            会话列表
          </el-button>
          <h3>{{ conversationHeaderTitle }}</h3>
        </div>
        <el-button color="#111827" class="open-detail" icon="Document" @click="isChatDetail = true">
          详情
        </el-button>
      </div>

      <div class="composer">
        <FileUploadButton :conversation-id="currentConversation?.id" :disabled="uploadingFiles || props.loading"
          @file-selected="handleFileSelected" />
        <el-input v-model="model" clearable placeholder="输入消息" @keyup.enter="emit('sendTextMessage')" />
        <el-button color="#111827" :disabled="props.loading || uploadingFiles" @click="emit('sendTextMessage')">
          发送
        </el-button>
        <el-button color="#111827" :disabled="props.loading" @click="emit('markRead')">
          标为已读
        </el-button>
      </div>

      <ConversationDetailDialog v-model="isChatDetail" :conversation="currentConversation"
        @update:conversation="emit('updateConversation')" @load-more="emit('loadMore')" />
      
        <!-- 全局唯一 FilePreview 组件 -->
      <FilePreview v-model="galleryVisible" :file-list="galleryFileList" v-model:current-index="galleryIndex" />

      <el-dialog v-model="dropUploadDialogVisible" title="确认上传文件" width="560px" :close-on-click-modal="!dropUploadBusy"
        :close-on-press-escape="!dropUploadBusy" :show-close="!dropUploadBusy" @closed="clearDroppedFiles">
        <div class="drop-file-dialog">
          <div class="drop-file-question">是否上传这些文件到当前会话？</div>
          <div class="drop-file-summary">
            <span>共 {{ droppedFileItems.length }} 个文件，{{ droppedFileTotalSize }}</span>
            <span v-if="invalidDroppedFileCount > 0">其中 {{ invalidDroppedFileCount }} 个不可上传，将跳过</span>
          </div>

          <div class="drop-file-list">
            <div v-for="item in droppedFileItems" :key="`${item.name}-${item.file.lastModified}-${item.file.size}`"
              class="drop-file-row" :class="{ invalid: !item.valid }">
              <div class="drop-file-icon">
                <el-icon :size="22">
                  <Document />
                </el-icon>
              </div>
              <div class="drop-file-info">
                <div class="drop-file-name" :title="item.name">{{ item.name }}</div>
                <div class="drop-file-meta">
                  <span>{{ item.typeText }}</span>
                  <span>{{ item.sizeText }}</span>
                </div>
              </div>
              <span class="drop-file-status" :class="{ valid: item.valid, invalid: !item.valid }">
                {{ item.valid ? '可上传' : item.error }}
              </span>
            </div>
          </div>
        </div>

        <template #footer>
          <div class="drop-dialog-footer">
            <el-button :disabled="dropUploadBusy" @click="closeDropUploadDialog">取消</el-button>
            <el-button color="#111827" :loading="dropUploadBusy" :disabled="!canConfirmDropUpload"
              @click="confirmDropUpload">
              上传 {{ validDroppedFiles.length }} 个文件
            </el-button>
          </div>
        </template>
      </el-dialog>
    </div>

    <div v-else class="empty">请选择一个会话开始聊天。
    </div>
  </main>
</template>

<style scoped lang="scss">
/* 日期分隔线样式 */
.date-separator {
  margin: 0 12px;
}

.drop-upload-overlay {
  position: absolute;
  inset: 0;
  z-index: 20;
  display: grid;
  place-items: center;
  background: rgba(17, 24, 39, 0.18);
  backdrop-filter: blur(2px);
  pointer-events: none;
}

.drop-upload-indicator {
  display: grid;
  place-items: center;
  gap: 12px;
  min-width: 180px;
  padding: 28px 32px;
  border: 1px solid rgba(255, 255, 255, 0.65);
  border-radius: 8px;
  color: #111827;
  background: rgba(255, 255, 255, 0.94);
  box-shadow: 0 20px 48px rgba(17, 24, 39, 0.2);
}

.drop-upload-title {
  font-size: 16px;
  font-weight: 600;
  line-height: 22px;
}

.drop-file-dialog {
  display: grid;
  gap: 12px;
}

.drop-file-question {
  color: #111827;
  font-size: 15px;
  font-weight: 600;
  line-height: 22px;
}

.drop-file-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 12px;
  color: #6b7280;
  font-size: 13px;
  line-height: 18px;
}

.drop-file-list {
  display: grid;
  gap: 8px;
  max-height: 320px;
  overflow: auto;
  padding-right: 2px;
}

.drop-file-row {
  display: grid;
  grid-template-columns: 42px minmax(0, 1fr) auto;
  align-items: center;
  gap: 12px;
  padding: 10px;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  background: #fff;

  &.invalid {
    border-color: #fecaca;
    background: #fff7f7;
  }
}

.drop-file-icon {
  width: 42px;
  height: 42px;
  display: grid;
  place-items: center;
  border-radius: 8px;
  color: #374151;
  background: #f3f4f6;
}

.drop-file-info {
  min-width: 0;
}

.drop-file-name {
  overflow: hidden;
  color: #111827;
  font-size: 14px;
  font-weight: 600;
  line-height: 20px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.drop-file-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  color: #6b7280;
  font-size: 12px;
  line-height: 18px;
}

.drop-file-status {
  display: inline-flex;
  align-items: center;
  min-height: 24px;
  max-width: 150px;
  padding: 0 8px;
  border: 1px solid transparent;
  border-radius: 4px;
  font-size: 12px;
  line-height: 18px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;

  &.valid {
    border-color: #bbf7d0;
    color: #15803d;
    background: #f0fdf4;
  }

  &.invalid {
    border-color: #fecaca;
    color: #b91c1c;
    background: #fef2f2;
  }
}

.drop-dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

/* 移动端适配 */
@media (max-width: 768px) {
  .date-separator {
    margin: 0 6px;
  }

  .drop-upload-indicator {
    min-width: 150px;
    padding: 22px 24px;
  }

  .drop-file-row {
    grid-template-columns: 38px minmax(0, 1fr);
    gap: 10px;
  }

  .drop-file-icon {
    width: 38px;
    height: 38px;
  }

  .drop-file-status {
    grid-column: 2;
    justify-self: flex-start;
    max-width: 100%;
  }
}
</style>
