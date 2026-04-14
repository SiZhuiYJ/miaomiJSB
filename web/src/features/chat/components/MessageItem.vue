<script setup lang="ts">
import type { MessageSummary } from '../types'
import { formatChatTimeShort } from '../utils/chat'
import FileMessage from './FileMessage.vue'

const props = defineProps<{
    message: MessageSummary
    meUserId: number
    src: string
    isMine: boolean
    readText: string
    readColor: string
    createdAt: string
}>()
</script>

<template>
    <div :class="['msg-item', { mine: props.isMine }]">
        <el-avatar class="message-avatar" :src="props.src || undefined" :size="40" shape="square">
            {{ (props.message.senderNickName || String(props.message.senderUserId)).slice(0, 1) }}
        </el-avatar>
        <div class="content">
            <div class="meta header">
                <span>
                    #{{ props.message.id }} · {{ props.message.senderNickName || props.message.senderUserId }} ·
                    {{ props.message.messageType }}
                </span>
            </div>
            <div class="bubble-wrap">
                <!-- 根据消息类型渲染不同内容 -->

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

<style scoped lang="scss"></style>
