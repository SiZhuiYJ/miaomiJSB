<!-- MessagePanel.vue -->
<script setup lang="ts">
import { nextTick, watch, useTemplateRef, computed, ref } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia';
import { ElMessage } from 'element-plus';
import MessageItem from './MessageItem.vue';
import FileUploadButton from './FileUploadButton.vue';
import ConversationDetailDialog from './ConversationDetailDialog.vue';
import type { ConversationDetail, MessageSummary, MessageReadStatus, SendMessagePayload } from '../types';
import {
  formatChatTimeShort,
  formatDateSeparator,
  getMemberAvatarBySender,
  getConversationDisplayTitle,
} from '../utils/chat';
import { uploadFileForMessage } from '../utils/fileHelper';

const model = defineModel<string>({ default: '' });
const currentConversation = defineModel<ConversationDetail>('conversationDetail');

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
    await nextTick();
    scrollToBottom();
  }
);

// 消息进入视图时加载已读状态
function onMessageVisible(messageId: number) {
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

// 处理文件选择
async function handleFileSelected(files: File[]) {
  if (!currentConversation.value?.id) {
    ElMessage.error('请先选择一个会话');
    return;
  }

  uploadingFiles.value = true;

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
      } catch (error: any) {
        console.error(`Failed to upload file ${file.name}:`, error);
        ElMessage.error(`${file.name}: ${error.message || '上传失败'}`);
      }
    }
    ElMessage.success('文件发送成功');
  } catch (error: any) {
    console.error('File upload error:', error);
    ElMessage.error('文件发送失败');
  } finally {
    uploadingFiles.value = false;
  }
}
</script>

<template>
  <main class="main-panel">
    <div v-if="currentConversation" class="conversation-detail">
      <el-scrollbar ref="scrollbarRef" view-class="message-list-container">
        <div class="message-list" v-if="props.meUserId">
          <!-- 遍历分组 -->
          <template v-for="group in groupedMessages" :key="group.dateLabel">
            <!-- 日期分隔线 -->

            <div class="date-separator"> <el-divider border-style="dashed">{{ group.dateLabel }}</el-divider></div>
            <!-- 分组内的消息 -->
            <MessageItem v-for="msg in group.messages" :key="msg.id" @vue:mounted="onMessageVisible(msg.id)"
              :message="msg" :src="getMemberAvatarBySender(currentConversation, msg.senderUserId)"
              :meUserId="props.meUserId" :createdAt="formatChatTimeShort(msg.createdAt)"
              :is-mine="msg.senderUserId === props.meUserId" v-bind="getReadDisplay(msg.id)" />
          </template>
        </div>
      </el-scrollbar>

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

/* 移动端适配 */
@media (max-width: 768px) {
  .date-separator {
    margin: 0 6px;
  }
}
</style>