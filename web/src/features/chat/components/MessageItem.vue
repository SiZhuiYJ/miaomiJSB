<script setup lang="ts">
import type { MessageSummary } from '../types'
import { computed } from 'vue'

const props = defineProps<{
    message: MessageSummary
    meUserId: number
    src: string
    isMine: boolean
    readText: string
    readColor: string
    createdAt: string
}>()

const extraData = computed(() => {
    if (!props.message.extra) return null
    try {
        return JSON.parse(props.message.extra)
    } catch {
        return null
    }
})

function formatChatTime(value: string) {
    return new Date(value).toLocaleString('zh-CN', {
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    })
}

function formatFileSize(bytes?: number) {
    if (!bytes) return ''
    const units = ['B', 'KB', 'MB', 'GB']
    let size = bytes
    let unitIndex = 0
    while (size >= 1024 && unitIndex < units.length - 1) {
        size /= 1024
        unitIndex++
    }
    return `${size.toFixed(1)} ${units[unitIndex]}`
}
</script>

<template>
    <div :class="['msg-item', { mine: props.isMine }]">
        <el-avatar class="message-avatar" :src="props.src" :size="40" shape="square">
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
                <div class="bubble">
                    <!-- 文本消息 -->
                    <template v-if="props.message.messageType === 'text'">
                        {{ props.message.content || '[空消息]' }}
                    </template>

                    <!-- 图片消息 -->
                    <template v-else-if="props.message.messageType === 'image' && extraData">
                        <div class="media-message">
                            <img :src="extraData.thumbnailUrl || extraData.fileUrl" :alt="extraData.fileName || '图片'"
                                class="thumbnail" />
                            <span class="media-label">📷 图片</span>
                        </div>
                    </template>

                    <!-- 视频消息 -->
                    <template v-else-if="props.message.messageType === 'video' && extraData">
                        <div class="media-message">
                            <div class="video-thumb">
                                <img :src="extraData.thumbnailUrl || ''" :alt="extraData.fileName || '视频'"
                                    class="thumbnail" />
                                <span class="play-icon">▶</span>
                            </div>
                            <span class="media-label">
                                🎬 视频 {{ extraData.duration ? `${Math.floor(extraData.duration /
                                    60)}:${String(extraData.duration % 60).padStart(2, '0')}` : '' }}
                            </span>
                        </div>
                    </template>

                    <!-- 文件消息 -->
                    <template v-else-if="props.message.messageType === 'file' && extraData">
                        <div class="file-message">
                            <span class="file-icon">📄</span>
                            <div class="file-info">
                                <span class="file-name">{{ extraData.fileName || '未知文件' }}</span>
                                <span class="file-size">{{ formatFileSize(extraData.fileSize) }}</span>
                            </div>
                        </div>
                    </template>

                    <!-- 系统消息 -->
                    <template v-else-if="props.message.messageType === 'system'">
                        <span class="system-message">{{ props.message.content || '[系统通知]' }}</span>
                    </template>

                    <!-- 未知类型回退 -->
                    <template v-else>
                        {{ props.message.content || props.message.extra || '[未知消息类型]' }}
                    </template>
                </div>

                <!-- 底部元信息（时间、已读状态） -->
                <div class="meta foot">
                    <span>{{ formatChatTime(props.message.createdAt) }}</span>
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
/* 原有样式 + 新增多类型样式 */
// .msg-item {
//     /* ... */
// }

// .media-message {
//     display: flex;
//     flex-direction: column;
//     gap: 4px;
//     max-width: 200px;

//     .thumbnail {
//         max-width: 100%;
//         max-height: 120px;
//         border-radius: 8px;
//         object-fit: cover;
//     }

//     .video-thumb {
//         position: relative;

//         .play-icon {
//             position: absolute;
//             top: 50%;
//             left: 50%;
//             transform: translate(-50%, -50%);
//             background: rgba(0, 0, 0, 0.6);
//             color: white;
//             width: 32px;
//             height: 32px;
//             border-radius: 50%;
//             display: flex;
//             align-items: center;
//             justify-content: center;
//         }
//     }

//     .media-label {
//         font-size: 12px;
//         color: #666;
//     }
// }

// .file-message {
//     display: flex;
//     align-items: center;
//     gap: 10px;
//     padding: 8px 12px;
//     background: #f5f7fa;
//     border-radius: 8px;

//     .file-icon {
//         font-size: 24px;
//     }

//     .file-info {
//         display: flex;
//         flex-direction: column;

//         .file-name {
//             font-weight: 500;
//             word-break: break-all;
//         }

//         .file-size {
//             font-size: 12px;
//             color: #909399;
//         }
//     }
// }

// .system-message {
//     color: #909399;
//     font-style: italic;
// }
</style>
