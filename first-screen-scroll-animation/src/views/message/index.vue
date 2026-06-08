<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue';
import type { ComponentPublicInstance } from 'vue';
import MessageItem from './messageItem.vue';

// Message 组件负责在页面顶部弹出一条或多条消息气泡。
// 每条消息的动画生命周期为：
// 1. 入场：气泡从上方落到指定位置；
// 2. 滤镜过渡：SVG blob 滤镜逐步过渡到等效无滤镜；
// 3. 文字跳动：文字逐字跳动一次，不循环；
// 4. 停留：从入场结束后开始按 duration/d 计时；
// 5. 退场：气泡上移并透明，local 消息结束后从列表移除。
type DurationValue = string | number;

// 外部消息配置：
// - props.items 可以传入一组固定消息；
// - addMessage 可以运行时追加一条临时消息；
// - duration 和 d 都表示“入场完成后的停留时长”，duration 优先级更高。
interface MessageOptions {
    // 不传 id 时会按来源和索引自动生成；传 id 可以让外部稳定更新或移除某条消息。
    id?: string | number;
    // text 与 content 都可以作为展示内容，content 优先级更高。
    text?: string;
    content?: string;
    // x/y/s 直接进入 CSS 变量，支持 px、%、rem 等合法 CSS 长度。
    x?: string;
    y?: string;
    s?: string;
    // 气泡背景色，传给内联 background，内容区域继承这个背景。
    color?: string;
    d?: DurationValue;
    duration?: DurationValue;
    // delay 是整条 timeline 开始前的延迟，仅 autoplay=true 时生效。
    delay?: DurationValue;
}

// 运行时消息结构：
// 模板和动画逻辑只消费 RuntimeMessage，所有默认值和时长格式都在 normalizeMessage 里完成。
interface RuntimeMessage {
    // 字符串 id 用作 Map key、data-id 和 v-for key。
    id: string;
    content: string;
    x: string;
    y: string;
    s: string;
    color: string;
    duration: number;
    delay: number;
    source: 'default' | 'prop' | 'local';
}

const props = withDefaults(defineProps<{
    // 没有 items 且还没调用 addMessage 时，使用 text 创建一条默认消息。
    text?: string;
    // 外部受控消息列表，适合父组件自己管理消息生命周期。
    items?: MessageOptions[];
    // false 时 timeline 会创建但暂停，父组件可通过 expose 的 play/pause 控制。
    autoplay?: boolean;
}>(), {
    text: '!!!(づ￣ 3￣)づ╭❤～',
    autoplay: true,
});

// localMessageList 保存 addMessage 追加的消息；props.items 不会写入这里。
const localMessageList = ref<MessageOptions[]>([]);
// 用来区分“组件初始默认消息”和“用户已经开始管理本地消息但当前为空”。
const localMessagesStarted = ref(false);
// localMessageIndex 只用于给同一毫秒内创建的本地消息补充递增序号。
let localMessageIndex = 0;

interface MessageItemControls {
    play: () => void;
    pause: () => void;
    clearAnimation: () => void;
}

const messageItemMap = new Map<string, MessageItemControls>();

// 支持 2、'2s'、'500ms' 三种常见时长写法，统一转成 GSAP 使用的秒。
const parseDurationSeconds = (duration: DurationValue | undefined, fallback = 0) => {
    if (typeof duration === 'number') {
        return Number.isFinite(duration) ? duration : fallback;
    }

    if (!duration) {
        return fallback;
    }

    const value = duration.trim();
    const durationValue = Number.parseFloat(value);

    if (!Number.isFinite(durationValue)) {
        return fallback;
    }

    return value.endsWith('ms') ? durationValue / 1000 : durationValue;
};

const getMessageContent = (message: MessageOptions) => message.content ?? message.text ?? props.text;

// 将外部配置转换成 RuntimeMessage：
// 这里集中处理所有默认值，后续模板和动画代码就不需要再判断字段是否存在。
const normalizeMessage = (
    message: MessageOptions,
    index: number,
    source: RuntimeMessage['source'],
): RuntimeMessage => {
    const id = String(message.id ?? `${source}-${index}`);

    return {
        id,
        content: getMessageContent(message),
        x: message.x ?? '50%',
        y: message.y ?? `${30 + index * 50}px`,
        s: message.s ?? '40px',
        color: message.color ?? 'lawngreen',
        duration: parseDurationSeconds(message.duration ?? message.d, 5),
        delay: parseDurationSeconds(message.delay, 0),
        source,
    };
};

// 最终渲染列表由两部分合成：
// - props.items：外部受控消息；
// - localMessageList：通过 addMessage 追加的本地消息。
// 当两者都为空且还没有开始本地消息流程时，渲染一条默认消息，便于组件单独预览。
const messageList = computed<RuntimeMessage[]>(() => {
    const propItems = props.items ?? [];

    if (!propItems.length && !localMessageList.value.length && !localMessagesStarted.value) {
        return [normalizeMessage({ id: 'default', content: props.text }, 0, 'default')];
    }

    return [
        ...propItems.map((message, index) => normalizeMessage(message, index, 'prop')),
        ...localMessageList.value.map((message, index) => normalizeMessage(message, propItems.length + index, 'local')),
    ];
});

const isMessageItemControls = (value: unknown): value is MessageItemControls => {
    const item = value as Partial<MessageItemControls> | null;

    return !!item
        && typeof item.play === 'function'
        && typeof item.pause === 'function'
        && typeof item.clearAnimation === 'function';
};

// ref 挂在 MessageItem 组件上，用于把对外 play/pause 等控制转发到每条消息。
const setMessageItemRef = (id: string) => (component: Element | ComponentPublicInstance | null) => {
    if (isMessageItemControls(component)) {
        messageItemMap.set(id, component);
        return;
    }

    messageItemMap.delete(id);
};

const clearMessageItemAnimations = () => {
    messageItemMap.forEach((messageItem) => messageItem.clearAnimation());
};

// local 消息是组件内部维护的列表，动画结束或外部 removeMessage 时会从这里移除。
const removeLocalMessage = (id: string) => {
    localMessageList.value = localMessageList.value.filter((message) => String(message.id) !== id);
};

const handleMessageComplete = (message: RuntimeMessage) => {
    if (message.source === 'local') {
        removeLocalMessage(message.id);
    }
};

// 对外暴露：追加一条本地消息。
// 返回生成后的 id，父组件可以后续调用 removeMessage(id) 主动移除。
const addMessage = (message: string | MessageOptions) => {
    const nextMessage = typeof message === 'string' ? { content: message } : message;
    const id = String(nextMessage.id ?? `local-${Date.now()}-${localMessageIndex++}`);

    localMessagesStarted.value = true;
    localMessageList.value = [
        ...localMessageList.value,
        {
            ...nextMessage,
            id,
        },
    ];

    return id;
};

// 对外暴露：按 id 主动移除一条本地消息，并同步杀掉它的 timeline。
const removeMessage = (id: string | number) => {
    const messageId = String(id);

    messageItemMap.get(messageId)?.clearAnimation();
    removeLocalMessage(messageId);
};

// 对外暴露：清空本地消息和所有动画缓存。
// 注意：props.items 是父组件传入的数据，clearMessages 不会直接修改父组件数组。
const clearMessages = () => {
    clearMessageItemAnimations();
    localMessagesStarted.value = true;
    localMessageList.value = [];
};

// 对外暴露：继续播放所有已创建的 timeline。
const play = () => {
    messageItemMap.forEach((messageItem) => messageItem.play());
};

// 对外暴露：暂停所有已创建的 timeline。
const pause = () => {
    messageItemMap.forEach((messageItem) => messageItem.pause());
};

// 卸载时统一清理子组件公开的动画句柄。
onUnmounted(() => {
    clearMessageItemAnimations();
    messageItemMap.clear();
});

// 暴露给父组件的命令式控制接口。
defineExpose({
    addMessage,
    removeMessage,
    clearMessages,
    play,
    pause,
});
</script>

<template>
    <!--
        使用 Teleport 到 body：
        消息层通常需要脱离父组件的 overflow、transform 和 z-index 影响，
        直接挂到 body 可以保证它作为全局浮层展示。
    -->
    <Teleport to="body">
        <div class="message-container">
            <div class="messages">
                <!--
                    MessageItem 负责单条消息的 DOM、SVG filter 和 GSAP timeline。
                    父组件只保存组件控制句柄，用于对外暴露 play/pause/clearMessages。
                -->
                <MessageItem v-for="message in messageList" :key="message.id" :ref="setMessageItemRef(message.id)"
                    :message="message" :autoplay="props.autoplay" @complete="handleMessageComplete" />
            </div>
        </div>
    </Teleport>
</template>

<style scoped lang="scss">
.message-container {
    // 固定在视口顶部外侧，消息本体通过 y 坐标进入可视区域。
    position: fixed;
    top: -2rem;
    // top: 0;
    width: 100%;
    height: 2rem;
    background: red;
    // 浮层只展示动画，不拦截页面点击或滚动。
    pointer-events: none;
    z-index: 9999;

    .messages {
        // 作为所有 message 的定位上下文，尺寸继承顶部容器。
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: inherit;
    }
}
</style>
