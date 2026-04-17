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

const RECALL_WINDOW_MS = 5 * 60 * 1000

const canReply = computed(() => !props.message.isRecalled)
const isRecallWithinTimeLimit = computed(() => {
    const createdAtTimestamp = Date.parse(props.message.createdAt)
    if (Number.isNaN(createdAtTimestamp)) return false
    return Date.now() - createdAtTimestamp <= RECALL_WINDOW_MS
})
const canRecall = computed(() => props.isMine && !props.message.isRecalled && isRecallWithinTimeLimit.value)
const recalledText = computed(() => getRecalledMessageText(props.message))

function handleJumpToReply() {
    if (!props.replyTargetId) return
    emit('jumpToMessage', props.replyTargetId)
}
</script>

<template>
    <div v-if="props.message.isRecalled"
        :class="['msg-item', 'system-notice-item', { highlighted: props.highlighted }]">
        <div class="recalled-message system-notice-text">
            {{ recalledText }}
        </div>
    </div>
    <div v-else :class="['msg-item', { mine: props.isMine, highlighted: props.highlighted }]">
        <el-avatar class="message-avatar" :src="props.src || undefined" :size="40" shape="square">
            {{ (props.message.senderNickName || String(props.message.senderUserId)).slice(0, 1) }}
        </el-avatar>
        <div class="content">
            <div class="meta header">
                <span>
                    #{{ props.message.id }} · {{ props.message.senderNickName || props.message.senderUserId }} ·
                    {{ props.message.messageType }}
                </span>
                <el-button v-if="canReply" link color="#111827" size="small" class="reply-action"
                    @click="emit('reply', props.message)">
                    引用
                </el-button>
                <el-button v-if="canRecall" link color="#dc2659" size="small" class="reply-action"
                    @click="emit('recall', props.message)">
                    撤回
                </el-button>
            </div>
            <!-- 根据消息类型渲染不同内容 -->
            <button v-if="props.replyTarget || props.replyTargetId" class="reply-card" type="button"
                @click="handleJumpToReply">
                <div class="reply-card-sender">
                    {{ props.replyTarget ? getMessageSenderName(props.replyTarget) : `消息 #${props.replyTargetId}` }}
                </div>
                <div class="reply-card-preview">
                    {{ props.replyTarget ? getMessagePreview(props.replyTarget) : '原消息不可用' }}
                </div>
            </button>
            <div class="bubble-wrap">
                <!-- 文件类消息（图片、视频、音频、文档等） -->
                <template v-if="['image', 'video', 'audio', 'file'].includes(props.message.messageType)">
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

.reply-action+.reply-action {
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
    border-radius: 6px;
    border-bottom-left-radius: 0;
    border-top-left-radius: 0;
    color: inherit;
    text-align: left;
    cursor: pointer;
    background: rgba(229, 231, 235, 0.3);
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

.system-notice-item {
    display: flex;
    justify-content: center;
    max-width: 100%;
    width: 100%;
}

.system-notice-text {
    padding: 2px 10px;
    border-radius: 999px;
    font-size: 12px;
    line-height: 18px;
    text-align: center;
    background: rgba(107, 114, 128, 0.12);
}

.msg-item.mine .reply-card {
    border-left: 0px solid #9ca3af;
    border-right: 3px solid #111827;
    background: rgba(230, 240, 255, 0.5);
    border-top-left-radius: 6px;
    border-bottom-left-radius: 6px;
    border-top-right-radius: 0;
    border-bottom-right-radius: 0;
}

.msg-item.mine .recalled-message {
    background: #e7f0ff;
}

.msg-item.highlighted :deep(.video-message),
.msg-item.highlighted :deep(.audio-message),
.msg-item.highlighted :deep(.document-message),
.msg-item.highlighted :deep(.image-message),
.msg-item.highlighted .bubble,
.msg-item.highlighted .reply-card,
.msg-item.highlighted .recalled-message {
    box-shadow: 0 0 0px 0px rgb(200 176 19 / 28%);
    animation: highlighted-blink 1.1s ease-in-out infinite alternate;
}

.msg-item.highlighted .bubble,
.msg-item.highlighted .reply-card,
.msg-item.highlighted .recalled-message {
    background-color: #fff8d9;
}

@keyframes highlighted-blink {
    from {
        box-shadow: 0 0 1px 1px rgb(200 176 19 / 18%);
        filter: brightness(1);
    }

    to {
        box-shadow: 0 0 2px 2px rgb(200 176 19 / 42%);
        filter: brightness(1.06);
    }
}
</style>
