<script setup lang="ts">
import { gsap } from 'gsap';
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import type { ComponentPublicInstance } from 'vue';

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
    // 每条消息独立的 SVG filter id，避免多条消息共用同一个 filter 状态导致互相影响。
    filterId: string;
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

// 固定动画时长：
// - introDuration：入场阶段耗时，也是文字跳动开始的时间点；
// - exitDuration：退场上移淡出耗时；
// - 停留时长不固定，来自每条消息的 duration/d。
const introDuration = 1.5;
const exitDuration = 0.45;
// componentId 用来给 filter 生成实例级前缀，避免页面上多个 Message 组件 id 冲突。
const componentId = `message-${Math.random().toString(36).slice(2)}`;

// messages 是消息列表容器，用于在 nextTick 后查询每条消息的真实 DOM。
const messages = ref<HTMLElement | null>(null);
// localMessageList 保存 addMessage 追加的消息；props.items 不会写入这里。
const localMessageList = ref<MessageOptions[]>([]);
// 用来区分“组件初始默认消息”和“用户已经开始管理本地消息但当前为空”。
const localMessagesStarted = ref(false);
// 按消息 id 记录 DOM、timeline 和动画输入快照：
// - messageElementMap：函数式 ref 收集到的真实 message 根元素；
// - messageTimelines：每条消息正在运行或暂停的 GSAP timeline；
// - activeMessageKeys：上一次动画使用的关键输入，用于判断是否需要重建动画。
const messageElementMap = new Map<string, HTMLElement>();
const messageTimelines = new Map<string, gsap.core.Timeline>();
const activeMessageKeys = new Map<string, string>();
// localMessageIndex 只用于给同一毫秒内创建的本地消息补充递增序号。
let localMessageIndex = 0;
// watch 可能早于 DOM 挂载触发，isMounted 用来避免在未挂载时启动动画。
let isMounted = false;

// SVG blob 初始滤镜状态：
// - blur 决定融合模糊强度；
// - alpha/offset 是 feColorMatrix 的 alpha 通道参数，用来制造“粘连”效果。
const filterState = {
    blur: 10,
    alpha: 20,
    offset: -9,
};

// 生成 feColorMatrix values。
// 当 alpha=1 且 offset=0 时，这个矩阵等价于不改变透明度，也就是“无滤镜”状态的一部分。
const getMatrixValues = (alpha: number, offset: number): string => `
    1 0 0 0 0
    0 1 0 0 0
    0 0 1 0 0
    0 0 0 ${alpha.toFixed(3)} ${offset.toFixed(3)}
`;

const initialMatrixValues = getMatrixValues(filterState.alpha, filterState.offset);

// 使用 Array.from 而不是 split('')，可以更好地处理部分 Unicode 字符。
const splitMessageContent = (content: string) => Array.from(content);

// DOM id 只能安全包含有限字符；消息 id 可能来自外部，所以先做一层清洗。
const normalizeDomId = (value: string) => value.replace(/[^a-zA-Z0-9_-]/g, '-');

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
        filterId: `${componentId}-${normalizeDomId(id)}`,
        content: getMessageContent(message),
        x: message.x ?? '50%',
        y: message.y ?? `${60 + index * 72}px`,
        s: message.s ?? '50px',
        color: message.color ?? 'red',
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

// Vue 的函数式 ref 回调类型允许传入 Element、组件实例或 null。
// 模板中 ref 挂在 div 上，运行时我们只需要 HTMLElement；其他情况都视为该 id 的 DOM 不可用。
const setMessageRef = (id: string) => (element: Element | ComponentPublicInstance | null) => {
    if (element instanceof HTMLElement) {
        messageElementMap.set(id, element);
        return;
    }

    messageElementMap.delete(id);
};

// 用消息关键字段生成动画快照。
// 如果这些字段没有变化，就不重建 timeline，避免 watch 触发时打断正在播放的动画。
const getMessageAnimationKey = (message: RuntimeMessage) => JSON.stringify({
    content: message.content,
    x: message.x,
    y: message.y,
    s: message.s,
    color: message.color,
    duration: message.duration,
    delay: message.delay,
});

// 清理单条消息动画，常用于消息删除、重建或组件卸载。
// kill 后还要删除 activeMessageKeys，否则 sync 时会误判旧动画仍然有效。
const clearMessageAnimation = (id: string) => {
    messageTimelines.get(id)?.kill();
    messageTimelines.delete(id);
    activeMessageKeys.delete(id);
};

// local 消息是组件内部维护的列表，动画结束或外部 removeMessage 时会从这里移除。
const removeLocalMessage = (id: string) => {
    localMessageList.value = localMessageList.value.filter((message) => String(message.id) !== id);
};

// 将当前 JS 状态写回对应 SVG filter 节点。
// 每条消息有独立的 blur/matrix 元素，所以多条消息可以同时播放不同进度的滤镜动画。
const applyMessageFilterState = (message: RuntimeMessage, state: typeof filterState) => {
    const blurElement = document.getElementById(`${message.filterId}-blur`);
    const matrixElement = document.getElementById(`${message.filterId}-matrix`);

    blurElement?.setAttribute('stdDeviation', state.blur.toFixed(3));
    matrixElement?.setAttribute('values', getMatrixValues(state.alpha, state.offset));
};

// 创建并启动单条消息的完整 timeline。
// 这里不依赖 CSS keyframes，是为了让入场、停留、跳字、退场都能在一个时间轴里精确对齐。
const runMessageAnimation = (message: RuntimeMessage) => {
    const messageElement = messageElementMap.get(message.id);
    const bodyElement = messageElement?.querySelector<HTMLElement>('.message-body');
    const charElements = Array.from(messageElement?.querySelectorAll<HTMLElement>('.message-char') ?? []);

    if (!messageElement || !bodyElement) {
        return;
    }

    clearMessageAnimation(message.id);

    // state 是这条消息自己的滤镜动画状态，不能复用全局 filterState 对象。
    const state = { ...filterState };
    // 入场起点和退场距离按标签真实高度计算，字号变大时动画距离也会自然增大。
    const introStartY = -Math.max(bodyElement.offsetHeight * 1.6, 64);
    const exitY = -Math.max(bodyElement.offsetHeight * 0.8, 24);
    const timeline = gsap.timeline({
        // props.autoplay=false 时仍创建 timeline，但保持暂停，方便外部手动 play。
        delay: props.autoplay ? message.delay : 0,
        paused: !props.autoplay,
        onComplete: () => {
            // 通过 addMessage 创建的本地消息播放结束后自动移除；
            // props.items 属于父组件数据，不在这里主动删除。
            if (message.source === 'local') {
                removeLocalMessage(message.id);
            }
        },
    });

    // 先把 SVG filter 恢复到初始 blob 状态，避免复用 DOM 时继承上次 identity 状态。
    applyMessageFilterState(message, state);

    // 初始化气泡滤镜和标签位置，确保重复播放时不会继承上一次的终态。
    timeline.set(messageElement, {
        filter: `url(#${message.filterId})`,
    });

    timeline.set(bodyElement, {
        // xPercent 由 GSAP 维护，与后续 y 动画合并到同一个 transform，避免覆盖水平居中。
        xPercent: -50,
        y: introStartY,
        opacity: 1,
    });

    timeline.set(charElements, {
        y: 0,
        rotation: 0,
        transformOrigin: 'bottom center',
    });

    // 出场动画：标签从上方落到目标位置。
    timeline.to(bodyElement, {
        y: 0,
        duration: introDuration,
        ease: 'power2.out',
    }, 0);

    // SVG filter 不能从 url() 平滑过渡到 none，所以先把滤镜参数动到 identity。
    // 0.5s 开始收束，和入场动画重叠，视觉上是气泡落下后逐渐变清晰。
    timeline.to(state, {
        blur: 0,
        alpha: 1,
        offset: 0,
        duration: 0.9,
        ease: 'power2.out',
        onUpdate: () => applyMessageFilterState(message, state),
        onComplete: () => {
            // 参数到达 identity 后再切换 filter:none，此时视觉上已经没有跳变。
            applyMessageFilterState(message, state);
            gsap.set(messageElement, { filter: 'none' });
        },
    }, 0.5);

    if (charElements.length) {
        // 出场结束后触发一次文字跳动，不设置 repeat。
        // 每个字符都是 inline-block，才能独立接收 y/rotation transform。
        timeline.to(charElements, {
            keyframes: [
                { y: -8, rotation: 5, duration: 0.35, ease: 'power2.out' },
                { y: 0, rotation: 0, duration: 0.35, ease: 'power2.inOut' },
            ],
            stagger: {
                each: 0.08,
                from: 'start',
            },
        }, introDuration);
    }

    // 停留时间从出场结束后开始计算，结束后标签上移并淡出。
    // 因此退场开始时间是 introDuration + message.duration，而不是组件挂载后的 duration。
    timeline.to(bodyElement, {
        y: exitY,
        opacity: 0,
        duration: exitDuration,
        ease: 'power2.in',
    }, introDuration + message.duration);

    messageTimelines.set(message.id, timeline);
    activeMessageKeys.set(message.id, getMessageAnimationKey(message));
};

// 同步渲染列表和动画列表：
// 1. 等待 nextTick，确保 v-for 新增的 DOM 已经创建；
// 2. 清理已经不存在的消息 timeline；
// 3. 对新增或关键字段变化的消息重建 timeline。
const syncMessageAnimations = async () => {
    if (!isMounted) {
        return;
    }

    await nextTick();

    const currentIds = new Set(messageList.value.map((message) => message.id));

    // 清理已从 messageList 移除的消息，避免 timeline 和 DOM 引用泄漏。
    Array.from(activeMessageKeys.keys()).forEach((id) => {
        if (!currentIds.has(id)) {
            clearMessageAnimation(id);
            messageElementMap.delete(id);
        }
    });

    // 对每条当前消息检查动画输入是否变化。
    // 变化包括内容、位置、尺寸、颜色、停留时间和延迟。
    messageList.value.forEach((message) => {
        const animationKey = getMessageAnimationKey(message);

        if (activeMessageKeys.get(message.id) === animationKey) {
            return;
        }

        runMessageAnimation(message);
    });
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

    clearMessageAnimation(messageId);
    removeLocalMessage(messageId);
};

// 对外暴露：清空本地消息和所有动画缓存。
// 注意：props.items 是父组件传入的数据，clearMessages 不会直接修改父组件数组。
const clearMessages = () => {
    messageTimelines.forEach((timeline) => timeline.kill());
    messageTimelines.clear();
    activeMessageKeys.clear();
    messageElementMap.clear();
    localMessagesStarted.value = true;
    localMessageList.value = [];
};

// 对外暴露：继续播放所有已创建的 timeline。
const play = () => {
    messageTimelines.forEach((timeline) => timeline.play());
};

// 对外暴露：暂停所有已创建的 timeline。
const pause = () => {
    messageTimelines.forEach((timeline) => timeline.pause());
};

// messageList 是 computed，props.items 或 localMessageList 变化都会触发同步。
watch(messageList, () => {
    void syncMessageAnimations();
});

// 挂载后才允许访问真实 DOM 并启动动画。
onMounted(() => {
    isMounted = true;
    void syncMessageAnimations();
});

// 卸载时统一清理 GSAP timeline 和 DOM 引用，防止 Teleport 到 body 后留下悬挂动画。
onUnmounted(() => {
    clearMessages();
    isMounted = false;
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
            <div class="messages" ref="messages">
                <!--
                    每条 message 是一个滤镜容器：
                    - 函数式 ref 收集真实 DOM，GSAP 需要直接操作它；
                    - data-id 便于调试，也和 message.id 保持一致；
                    - CSS 变量把位置、尺寸交给样式层计算。
                -->
                <div v-for="message in messageList" :key="message.id" :ref="setMessageRef(message.id)" class="message"
                    :data-id="message.id" :style="{
                        '--s': message.s,
                        '--x': message.x,
                        '--y': message.y,
                        '--d': `${message.duration}s`,
                        background: message.color,
                    }">
                    <!--
                        message-body 是真正移动和淡出的标签。
                        外层 message 负责 filter，内层 body 负责 transform/opacity，
                        这样退场动画不会干扰 SVG filter 的过渡。
                    -->
                    <div class="message-body">
                        <span class="message-content">
                            <!--
                                拆成逐字 span 后，GSAP 才能对每个字符做 stagger 跳动。
                                white-space: pre 保留空格，避免文本内容被布局折叠。
                            -->
                            <span v-for="(char, index) in splitMessageContent(message.content)"
                                :key="`${message.id}-${index}`" class="message-char">
                                {{ char }}
                            </span>
                        </span>
                    </div>
                </div>
            </div>
        </div>
        <!--
            SVG filter 放在隐藏 svg 中供 CSS filter:url(#id) 引用。
            每条消息各自拥有一组 feGaussianBlur/feColorMatrix，
            因为它们的参数会被 GSAP 逐帧修改，不能共用同一组节点。
        -->
        <svg style="display: none;">
            <defs>
                <filter v-for="message in messageList" :key="message.filterId" :id="message.filterId" x="-50%" y="-50%"
                    width="200%" height="200%">
                    <feGaussianBlur :id="`${message.filterId}-blur`" in="SourceGraphic" stdDeviation="10"
                        result="blur" />
                    <feColorMatrix :id="`${message.filterId}-matrix`" in="blur" mode="matrix"
                        :values="initialMatrixValues" />
                </filter>
            </defs>
        </svg>
    </Teleport>
</template>

<style scoped lang="scss">
.message-container {
    // 固定在视口顶部外侧，消息本体通过 y 坐标进入可视区域。
    position: fixed;
    top: -2rem;
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

        .message {
            // 外层铺满宽度，便于内部标签使用百分比 x 定位。
            position: absolute;
            bottom: 0;
            left: 0;
            width: 100%;
            height: 1rem;
            --x: 100px;
            --y: 100px;
            --s: 50px;
            background: lawngreen;
            // 初始无滤镜；GSAP 在 timeline 开始时切到 url(#filterId)，结束后再切回 none。
            filter: none;
            will-change: filter;

            .message-body {
                // 真正的消息标签。GSAP 会写入 transform，所以这里不直接写 transform。
                position: absolute;
                width: fit-content;
                padding: calc(var(--s) * 0.2);
                height: var(--s);
                left: var(--x);
                top: var(--y);
                display: flex;
                justify-content: center;
                align-items: center;
                border-radius: calc(var(--s) * 0.5);
                background: inherit;
                // 默认隐藏，等待 GSAP 初始化到入场起点后再显示。
                opacity: 0;
                will-change: transform, opacity;

                .message-content {
                    // inline-flex 保证字符作为一行内容参与标签宽度计算。
                    display: inline-flex;
                    align-items: center;
                    font-size: calc(var(--s) * 0.5);
                    color: white;

                    .message-char {
                        // 每个字符都需要独立 transform，才能实现逐字跳动。
                        display: inline-block;
                        white-space: pre;
                        transform-origin: bottom center;
                    }
                }
            }
        }
    }
}
</style>
