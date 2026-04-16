<script setup lang="ts">
import { computed } from 'vue'
import type { MessageReference, MessageSummary } from '../types'
import {
  formatChatTimeShort,
  getMessagePreview,
  getMessageSenderName,
  getRecalledMessageText,
} from '../utils/chat'
import FileMessage from './FileMessage.vue'

const props = defineProps<{
  message: MessageSummary
  meUserId: number
  src: string
  isMine: boolean
  readText: string
  readColor: string
  replyTarget?: MessageSummary | MessageReference | null
  replyTargetId?: number | null
  highlighted?: boolean
}>()

const emit = defineEmits<{
  reply: [message: MessageSummary]
  recall: [message: MessageSummary]
  jumpToMessage: [messageId: number]
}>()

const canReply = computed(() => !props.message.isRecalled)
const canRecall = computed(() => props.isMine && !props.message.isRecalled)
const recalledText = computed(() => getRecalledMessageText(props.message))

function handleJumpToReply() {
  if (!props.replyTargetId) return
  emit('jumpToMessage', props.replyTargetId)
}
</script>

<template>
    <div :class="['msg-item', { mine: props.isMine, highlighted: props.highlighted }]">
        <el-avatar class="message-avatar" :src="props.src || undefined" :size="40" shape="square">
            {{ (props.message.senderNickName || String(props.message.senderUserId)).slice(0, 1) }}
        </el-avatar>
        <div class="content">
            <div class="meta header">
                <span>
                    #{{ props.message.id }} · {{ props.message.senderNickName || props.message.senderUserId }} ·
                    {{ props.message.messageType }}
                </span>
                <el-button v-if="canReply" text color="#111827" class="reply-action" @click="emit('reply', props.message)">
                    引用
                </el-button>
                <el-button v-if="canRecall" text color="#dc2626" class="reply-action" @click="emit('recall', props.message)">
                    撤回
                </el-button>
            </div>
            <div class="bubble-wrap">
                <button v-if="props.replyTarget || props.replyTargetId" class="reply-card" type="button" @click="handleJumpToReply">
                    <div class="reply-card-sender">
                        {{ props.replyTarget ? getMessageSenderName(props.replyTarget) : `消息 #${props.replyTargetId}` }}
                    </div>
                    <div class="reply-card-preview">
                        {{ props.replyTarget ? getMessagePreview(props.replyTarget) : '原消息不可用' }}
                    </div>
                </button>
                <!-- 根据消息类型渲染不同内容 -->

                <!-- 文件类消息（图片、视频、音频、文档等） -->
                <div v-if="props.message.isRecalled" class="recalled-message">
                    {{ recalledText }}
                </div>
                <template v-else-if="['image', 'video', 'audio', 'file'].includes(props.message.messageType)">
                    <FileMessage :message="props.message" :show-download="true" :src />
                </template>

                <!-- 文本消息 -->
                <div class="bubble" v-else-if="props.message.messageType === 'text'">
                    {{ props.message.content || '[空消息]' }}
                </div>

                <!-- 系统消息 -->
                <template v-else-if="props.message.messageType === 'system'">
                    <span class="system-message">{{ props.message.content || '[系统通知]' }}</span>
                </template>

                <!-- 未知类型回退 -->
                <template v-else>
                    {{ props.message.content || props.message.extra || '[未知消息类型]' }}
                </template>

                <!-- 底部元信息（时间、已读状态） -->
                <div class="meta foot">
                    <span>{{ formatChatTimeShort(props.message.createdAt) }}</span>
                    <template v-if="props.meUserId && props.message.senderUserId === props.meUserId">
                        <el-icon>
                            <CircleCheck :color="props.readColor" />
                        </el-icon>
                        <span v-if="props.readText" class="read-count">{{ props.readText }}</span>
                    </template>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped lang="scss">
.content {
    min-width: 0;
}

.header {
    justify-content: space-between;
}

.reply-action {
    padding: 0;
}

.reply-action + .reply-action {
    margin-left: 8px;
}

.bubble-wrap {
    min-width: 0;
}

.reply-card {
    display: flex;
    flex-direction: column;
    gap: 2px;
    max-width: 100%;
    margin-bottom: 6px;
    padding: 8px 10px;
    border: 0;
    border-left: 3px solid #9ca3af;
    border-radius: 8px;
    color: inherit;
    text-align: left;
    cursor: pointer;
    background: rgba(229, 231, 235, 0.7);
}

.reply-card-sender {
    color: #111827;
    font-size: 12px;
    font-weight: 600;
    line-height: 18px;
}

.reply-card-preview {
    overflow: hidden;
    color: #4b5563;
    font-size: 12px;
    line-height: 18px;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.recalled-message {
    padding: 12px 14px;
    border-radius: 10px;
    color: #6b7280;
    font-size: 14px;
    line-height: 20px;
    background: #f3f4f6;
}

.msg-item.mine .reply-card {
    border-left-color: #111827;
    background: rgba(230, 240, 255, 0.9);
}

.msg-item.mine .recalled-message {
    background: #e7f0ff;
}

.msg-item.highlighted :deep(.file-message),
.msg-item.highlighted .bubble,
.msg-item.highlighted .reply-card,
.msg-item.highlighted .recalled-message {
    box-shadow: 0 0 0 2px rgba(17, 24, 39, 0.12);
}

.msg-item.highlighted .bubble,
.msg-item.highlighted .reply-card,
.msg-item.highlighted .recalled-message {
    background-color: #fff8d9;
}
</style>
