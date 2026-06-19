<script setup lang="ts">
import { gsap } from 'gsap';
import { computed, nextTick, onMounted, onUnmounted, ref, watch, useTemplateRef } from 'vue';

interface MessageItemModel {
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
    message: MessageItemModel;
    autoplay?: boolean;
}>(), {
    autoplay: true,
});

const emit = defineEmits<{
    complete: [message: MessageItemModel];
}>();

// 固定动画时长：
// - introDuration：入场阶段耗时，也是文字跳动开始的时间点；
// - exitDuration：退场上移淡出耗时；
// - 停留时长不固定，来自每条消息的 duration/d。
const introDuration = 1.5;
const exitDuration = 0.45;

// 每条消息组件独立生成 SVG filter id，避免多个 Message 组件实例互相影响。
const filterId = `message-item-${Math.random().toString(36).slice(2)}`;

const messageElement = useTemplateRef('messageElement');
let timeline: gsap.core.Timeline | null = null;
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

// 用消息关键字段生成动画快照。
// 如果这些字段没有变化，就不重建 timeline，避免父组件列表刷新时打断正在播放的动画。
const animationKey = computed(() => JSON.stringify({
    content: props.message.content,
    x: props.message.x,
    y: props.message.y,
    s: props.message.s,
    color: props.message.color,
    duration: props.message.duration,
    delay: props.message.delay,
}));

// 使用 Array.from 而不是 split('')，可以更好地处理部分 Unicode 字符。
const splitMessageContent = (content: string) => Array.from(content);

// 将当前 JS 状态写回对应 SVG filter 节点。
const applyMessageFilterState = (state: typeof filterState) => {
    const blurElement = document.getElementById(`${filterId}-blur`);
    const matrixElement = document.getElementById(`${filterId}-matrix`);

    blurElement?.setAttribute('stdDeviation', state.blur.toFixed(3));
    matrixElement?.setAttribute('values', getMatrixValues(state.alpha, state.offset));
};

const clearAnimation = () => {
    timeline?.kill();
    timeline = null;

    if (messageElement.value) {
        gsap.set(messageElement.value, { filter: 'none' });
    }
};

// 创建并启动单条消息的完整 timeline。
// 这里不依赖 CSS keyframes，是为了让入场、停留、跳字、退场都能在一个时间轴里精确对齐。
const restartAnimation = async () => {
    if (!isMounted) {
        return;
    }

    await nextTick();

    const rootElement = messageElement.value;
    const bodyElement = rootElement?.querySelector<HTMLElement>('.message-body');
    const charElements = Array.from(rootElement?.querySelectorAll<HTMLElement>('.message-char') ?? []);

    if (!rootElement || !bodyElement) {
        return;
    }

    clearAnimation();

    // state 是这条消息自己的滤镜动画状态，不能复用全局 filterState 对象。
    const state = { ...filterState };
    // 入场起点和退场距离按标签真实高度计算，字号变大时动画距离也会自然增大。
    const introStartY = -Math.max(bodyElement.offsetHeight * 1.6, 64);
    const exitY = -Math.max(bodyElement.offsetHeight * 0.8, 24);

    // 先把 SVG filter 恢复到初始 blob 状态，避免复用 DOM 时继承上次 identity 状态。
    applyMessageFilterState(state);

    timeline = gsap.timeline({
        // autoplay=false 时仍创建 timeline，但保持暂停，方便父组件手动 play。
        delay: props.autoplay ? props.message.delay : 0,
        paused: !props.autoplay,
        onComplete: () => {
            emit('complete', props.message);
        },
    });

    // 初始化气泡滤镜和标签位置，确保重复播放时不会继承上一次的终态。
    timeline.set(rootElement, {
        filter: `url(#${filterId})`,
    });

    timeline.set(bodyElement, {
        // xPercent 由 GSAP 维护，与后续 y 动画合并到同一个 transform，避免覆盖水平居中。
        scale: 0.7,
        xPercent: -50,
        y: introStartY,
        opacity: 1,
    });

    timeline.set(charElements, {
        y: 0,
        rotation: 0,
        transformOrigin: 'bottom center',
    });

    // 入场动画：标签从上方落到目标位置。
    timeline.to(bodyElement, {
        y: 0,
        scale: 1,
        duration: introDuration,
        ease: 'back.out(3)',
    }, 0);

    // SVG filter 不能从 url() 平滑过渡到 none，所以先把滤镜参数动到 identity。
    // 0.5s 开始收束，和入场动画重叠，视觉上是气泡落下后逐渐变清晰。
    timeline.to(state, {
        blur: 0,
        alpha: 1,
        offset: 0,
        duration: 0.5,
        ease: 'power2.out',
        onUpdate: () => applyMessageFilterState(state),
        onComplete: () => {
            // 参数到达 identity 后再切换 filter:none，此时视觉上已经没有跳变。
            applyMessageFilterState(state);
            gsap.set(rootElement, { filter: 'none' });
        },
    }, 0.5);

    if (charElements.length) {
        // 入场结束后触发一次文字跳动，不设置 repeat。
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

    // 停留时间从入场结束后开始计算，结束后标签上移并淡出。
    timeline.to(bodyElement, {
        y: exitY,
        opacity: 0,
        scale: 0.7,
        duration: exitDuration,
        ease: 'power2.in',
    }, introDuration + props.message.duration);
};

const play = () => {
    timeline?.play();
};

const pause = () => {
    timeline?.pause();
};

watch(animationKey, () => {
    void restartAnimation();
}, {
    flush: 'post',
});

onMounted(() => {
    isMounted = true;
    void restartAnimation();
});

onUnmounted(() => {
    clearAnimation();
    isMounted = false;
});

defineExpose({
    play,
    pause,
    clearAnimation,
    restartAnimation,
});
</script>

<template>
    <div ref="messageElement" class="message" :data-id="props.message.id" :style="{
        '--s': props.message.s,
        '--x': props.message.x,
        '--y': props.message.y,
        '--d': `${props.message.duration}s`,
        background: props.message.color,
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
                <span v-for="(char, index) in splitMessageContent(props.message.content)"
                    :key="`${props.message.id}-${index}`" class="message-char">
                    {{ char }}
                </span>
            </span>
        </div>
    </div>
    <!--
        SVG filter 放在隐藏 svg 中供 CSS filter:url(#id) 引用。
        每条消息各自拥有一组 feGaussianBlur/feColorMatrix，
        因为它们的参数会被 GSAP 逐帧修改，不能共用同一组节点。
    -->
    <svg style="display: none;">
        <defs>
            <filter :id="filterId" x="-50%" y="-50%" width="200%" height="200%">
                <feGaussianBlur :id="`${filterId}-blur`" in="SourceGraphic" stdDeviation="10" result="blur" />
                <feColorMatrix :id="`${filterId}-matrix`" in="blur" mode="matrix" :values="initialMatrixValues" />
            </filter>
        </defs>
    </svg>
</template>

<style scoped lang="scss">
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
</style>
