<script setup lang="ts">
import { ref, onMounted } from "vue";

interface TimeSlot {
    id: number;
    slotName: string;
    startTime: string;
    endTime: string;
}

interface CheckinDetail {
    id: number;
    status: number;
    timeSlotId?: number;
    note?: string;
    imageUrls: string[];
}

interface Props {
    planId: number;
    date: Date;
    // 改为接收完整的打卡详情数组，而不是单一状态
    checkinDetails: CheckinDetail[];
    timeSlots: TimeSlot[];
    note?: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
    (e: "open"): void;
}>();

const isOpen = ref(false);

const statusTextMap = {
    1: "已打卡",
    2: "已补签",
    null: "未打卡"
};

const getStatusText = (status: number | null): string => {
    return statusTextMap[status as keyof typeof statusTextMap] || "未知状态";
};

const getStatusClass = (status: number | null): string => {
    if (status === 1) return "success";
    if (status === 2) return "retro";
    return "missed";
};

const formatDateOnly = (date: Date): string => {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, "0");
    const d = String(date.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
};

// 根据时间段ID查找对应的打卡状态（处理字符串ID）
const getSlotStatus = (slotId: number | string): number | null => {
    const targetId = typeof slotId === 'string' ? Number(slotId) : slotId;
    const detail = props.checkinDetails.find(item =>
        item.timeSlotId !== undefined &&
        Number(item.timeSlotId) === targetId
    );
    return detail ? detail.status : null;
};
onMounted(() => {
    console.log("props", props);
});
</script>

<template>
    <div class="time-slot-card" @click="isOpen = !isOpen">
        <div class="card-header">
            <div class="header-left">
                <span class="date">{{ formatDateOnly(props.date) }}</span>
                <span v-if="props.timeSlots.length > 0" class="time-slot">时间段</span>
            </div>
            <div class="header-right">
                <!-- 整体状态显示（可选） -->
                <span v-if="props.checkinDetails.length > 0"
                    :class="['status-badge', getStatusClass(props.checkinDetails.some(d => d.status === 1) ? 1 : (props.checkinDetails.some(d => d.status === 2) ? 2 : null))]">
                    {{getStatusText(props.checkinDetails.some(d => d.status === 1) ? 1 : (props.checkinDetails.some(d =>
                        d.status === 2) ? 2 : null))}}
                </span>
                <span class="chevron" :class="{ 'rotated': isOpen }">▼</span>
            </div>
        </div>

        <div v-if="isOpen" class="card-content">
            <div class="content-section">
                <div v-if="props.note" class="note">
                    <span class="label">备注：</span>
                    <span class="value">{{ props.note }}</span>
                </div>

                <div v-if="props.timeSlots.length > 0" class="time-slot-list">
                    <div v-for="slot in props.timeSlots" :key="slot.id" class="time-slot-item">
                        <div class="slot-info">
                            <span class="slot-name">{{ slot.slotName }}</span>
                            <span class="slot-time">{{ slot.startTime.slice(0, 5) }} - {{ slot.endTime.slice(0, 5)
                                }}</span>
                        </div>
                        <span :class="['status-tag', getStatusClass(getSlotStatus(slot.id))]"
                            v-text="getStatusText(getSlotStatus(slot.id))"></span>
                    </div>
                </div>
            </div>

            <div class="action-buttons">
                <button
                    v-if="!props.checkinDetails.some(d => d.status === 1) && !props.checkinDetails.some(d => d.status === 2)"
                    class="primary-btn" @click.stop="emit('open')">
                    打卡/补签
                </button>
                <button v-else class="detail-btn" @click.stop="emit('open')">
                    查看详情
                </button>
            </div>
        </div>
    </div>
</template>

<style scoped lang="scss">
.time-slot-card {
    border: 1px solid var(--border-color);
    border-radius: 8px;
    overflow: hidden;
    background: var(--bg-elevated);
    cursor: pointer;
    transition: all 0.3s ease;

    &:hover {
        border-color: var(--accent-color);
        box-shadow: 0 4px 12px rgba(15, 23, 42, 0.1);
    }

    .card-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 16px;
        background: var(--bg-primary);
        border-bottom: 1px solid var(--border-color);

        .header-left {
            display: flex;
            gap: 12px;
            align-items: center;

            .date {
                font-weight: 600;
                color: var(--text-color);
            }

            .time-slot {
                font-size: 12px;
                color: var(--text-muted);
                background: var(--surface-soft);
                padding: 2px 8px;
                border-radius: 4px;
            }
        }

        .header-right {
            display: flex;
            align-items: center;
            gap: 8px;

            .status-badge {
                padding: 4px 8px;
                border-radius: 4px;
                font-size: 12px;
                font-weight: 500;
                text-transform: uppercase;

                &.success {
                    background: rgba(34, 197, 94, 0.1);
                    color: #166534;
                }

                &.retro {
                    background: rgba(234, 179, 8, 0.1);
                    color: #854d0e;
                }

                &.missed {
                    background: rgba(248, 113, 113, 0.1);
                    color: #b91c1c;
                }
            }

            .chevron {
                width: 16px;
                height: 16px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 12px;
                transition: transform 0.3s ease;
                color: var(--text-muted);

                &.rotated {
                    transform: rotate(180deg);
                }
            }
        }
    }

    .card-content {
        padding: 16px;
        background: var(--bg-primary);

        .content-section {
            margin-bottom: 16px;

            .note {
                margin-bottom: 12px;
                padding: 8px 12px;
                background: var(--surface-soft);
                border-radius: 6px;

                .label {
                    font-weight: 500;
                    color: var(--text-color);
                    margin-right: 8px;
                }

                .value {
                    color: var(--text-muted);
                }
            }

            .time-slot-list {
                display: flex;
                flex-direction: column;
                gap: 8px;

                .time-slot-item {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    padding: 12px;
                    border-radius: 6px;
                    background: var(--bg-primary);
                    border: 1px solid var(--border-color);

                    .slot-info {
                        display: flex;
                        flex-direction: column;

                        .slot-name {
                            font-weight: 600;
                            color: var(--text-color);
                        }

                        .slot-time {
                            font-size: 12px;
                            color: var(--text-muted);

                            &::before {
                                content: "";
                                display: inline-block;
                                width: 4px;
                                height: 4px;
                                background: var(--text-muted);
                                border-radius: 50%;
                                margin: 0 4px;
                                vertical-align: middle;
                            }
                        }
                    }

                    .status-tag {
                        padding: 4px 8px;
                        border-radius: 4px;
                        font-size: 12px;
                        font-weight: 500;
                        text-transform: uppercase;

                        &.success {
                            background: rgba(34, 197, 94, 0.1);
                            color: #166534;
                        }

                        &.retro {
                            background: rgba(234, 179, 8, 0.1);
                            color: #854d0e;
                        }

                        &.missed {
                            background: rgba(248, 113, 113, 0.1);
                            color: #b91c1c;
                        }
                    }
                }
            }
        }

        .action-buttons {
            display: flex;
            gap: 8px;

            .primary-btn {
                flex: 1;
                border-radius: 999px;
                border: none;
                padding: 8px 0;
                background: linear-gradient(to right, var(--accent-color), var(--accent-alt));
                color: var(--accent-on);
                font-size: 14px;
                cursor: pointer;

                &:hover {
                    opacity: 0.9;
                }
            }

            .detail-btn {
                flex: 1;
                border-radius: 999px;
                border: 1px solid var(--border-color);
                padding: 8px 0;
                background: transparent;
                color: var(--text-color);
                font-size: 14px;
                cursor: pointer;

                &:hover {
                    background: var(--surface-soft);
                }
            }
        }
    }
}
</style>
