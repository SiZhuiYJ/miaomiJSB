<script setup lang="ts">
import type { MessageSummary } from '../types'
const props = defineProps<{
    message: MessageSummary;
    meUserId: number;
    src: string;
    isMine: boolean;
    readText: string;
    readColor: string;
    createdAt: string;
}>();
function formatChatTime(value: string) {
    return new Date(value).toLocaleString('zh-CN', {
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
    });
}
</script>
<template>
    <div :class="['msg-item', { mine: props.isMine }]">
        <el-avatar class="message-avatar" :src="props.src" :size="40" shape="square">
            {{ (props.message.senderNickName || String(props.message.senderUserId)).slice(0, 1) }}
        </el-avatar>
        <div class="content">
            <div class="meta header">
                <span>#{{ props.message.id }} · {{ props.message.senderNickName || props.message.senderUserId }} · {{
                    props.message.messageType }}</span>
            </div>
            <div class="bubble-wrap">
                <div class="bubble">{{ props.message.content || props.message.extra || '[空消息]' }}</div>
                <div class="meta foot">
                    <span>{{ formatChatTime(props.message.createdAt) }}</span>
                    <template v-if="props.meUserId && props.message.senderUserId === props.meUserId">
                        <el-icon>
                            <CircleCheck :color="props.readColor" />
                        </el-icon>
                        <span v-if="props.readText" class="read-count">
                            {{ props.readText }}
                        </span>
                    </template>
                </div>
            </div>
        </div>
    </div>
</template>