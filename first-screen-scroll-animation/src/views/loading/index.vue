<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';
import gsap from 'gsap';
import TextView from '../text/index.vue';

type LoadingPhase = 'idle' | 'covering' | 'textEntering' | 'covered' | 'erasing';
type TextViewControls = {
    prepareIntro: () => void;
    playTextAnimation: () => void;
    pauseTextAnimation: () => void;
    resetTextAnimation: () => void;
};

const loadingRef = ref<HTMLElement | null>(null);
const coverPathRef = ref<SVGPathElement | null>(null);
const erasePathRef = ref<SVGPathElement | null>(null);
const textLayerRef = ref<HTMLElement | null>(null);
const textViewRef = ref<TextViewControls | null>(null);
const loadingPhase = ref<LoadingPhase>('idle');
const COVER_DURATION = 2.1; // 封面动画时长
const ERASE_DURATION = 2.1; // 擦除动画时长
const TEXT_DROP_DURATION = 0.85;
const TEXT_DROP_STAGGER = 0.08;
const TEXT_PREVIEW_DURATION = 1.4;
const FOLD_COUNT = 7; // 屏幕内可见折返拐点数，数字越大折返越密
const PATH_STROKE_WIDTH = 48; // 路径宽度，边角露底时可以适当调大
const PATH_START_X = -70; // 封面路径起始点 X 坐标
const PATH_END_X = 170; // 封面路径结束点 X 坐标
const PATH_START_Y = 0; // 屏幕内路径顶部 Y 坐标
const PATH_END_Y = 100; // 屏幕内路径底部 Y 坐标
const triggerLabel = computed(() => {
    const labelMap: Record<LoadingPhase, string> = {
        idle: '开始加载',
        covering: '加载中',
        textEntering: '文字入场',
        covered: '完成加载',
        erasing: '结束中',
    };

    return labelMap[loadingPhase.value];
});
const isTriggerDisabled = computed(() => (
    loadingPhase.value === 'covering' ||
    loadingPhase.value === 'textEntering' ||
    loadingPhase.value === 'erasing'
));

const buildZigZagPath = (foldCount: number) => {
    const safeFoldCount = Math.max(0, Math.floor(foldCount));
    const visibleSegmentCount = safeFoldCount + 1;

    return Array.from({ length: visibleSegmentCount + 1 }, (_, index) => {
        const command = index === 0 ? 'M' : 'L';
        const x = index % 2 === 0 ? PATH_START_X : PATH_END_X;
        const progress = index / visibleSegmentCount;
        const y = PATH_START_Y + (PATH_END_Y - PATH_START_Y) * progress;

        return `${command} ${x} ${Number(y.toFixed(2))}`;
    }).join(' ');
};

const zigZagPath = buildZigZagPath(FOLD_COUNT);

let loadingTimeline: gsap.core.Timeline | null = null;
let shouldEraseAfterCover = false;

const getTextChars = () => {
    return Array.from(textLayerRef.value?.querySelectorAll<HTMLElement>('.char') ?? []);
};

const prepareLoadingText = () => {
    textViewRef.value?.prepareIntro();
    gsap.set(textLayerRef.value, {
        autoAlpha: 0,
    });
};

const resetLoadingText = () => {
    textViewRef.value?.resetTextAnimation();
    gsap.set(textLayerRef.value, {
        autoAlpha: 0,
    });
};

const resetPaths = () => {
    const coverPath = coverPathRef.value;
    const erasePath = erasePathRef.value;

    if (!coverPath || !erasePath) return;

    const pathLength = coverPath.getTotalLength();

    gsap.set([coverPath, erasePath], {
        strokeDasharray: pathLength,
        strokeDashoffset: pathLength,
    });
};

const playEraseAnimation = () => {
    const loading = loadingRef.value;
    const erasePath = erasePathRef.value;

    if (!loading || !erasePath) return;

    loadingTimeline?.kill();
    shouldEraseAfterCover = false;
    loadingPhase.value = 'erasing';
    textViewRef.value?.pauseTextAnimation();

    loadingTimeline = gsap.timeline({
        defaults: {
            ease: 'power2.inOut',
        },
        onComplete: () => {
            loadingPhase.value = 'idle';
            gsap.set(loading, { autoAlpha: 0 });
            resetPaths();
            resetLoadingText();
        },
    });

    loadingTimeline
        .to(erasePath, {
            strokeDashoffset: 0,
            duration: ERASE_DURATION,
            ease: 'power2.inOut',
        }, 0);
};

const startLoading = async () => {
    if (loadingPhase.value !== 'idle') return;

    await nextTick();

    const loading = loadingRef.value;
    const coverPath = coverPathRef.value;
    const textChars = getTextChars();

    if (!loading || !coverPath || !textChars.length) return;

    loadingTimeline?.kill();
    shouldEraseAfterCover = false;
    resetPaths();
    resetLoadingText();
    prepareLoadingText();

    loadingPhase.value = 'covering';

    loadingTimeline = gsap.timeline({
        defaults: {
            ease: 'power2.inOut',
        },
        onComplete: () => {
            if (shouldEraseAfterCover) {
                playEraseAnimation();
                return;
            }

            loadingPhase.value = 'covered';
        },
    });

    loadingTimeline
        .set(loading, { autoAlpha: 1 })
        .to(coverPath, {
            strokeDashoffset: 0,
            duration: COVER_DURATION,
            ease: 'power2.out',
        })
        .call(() => {
            loadingPhase.value = 'textEntering';
            gsap.set(textLayerRef.value, { autoAlpha: 1 });
        })
        .to(textChars, {
            autoAlpha: 1,
            y: 0,
            rotation: 0,
            duration: TEXT_DROP_DURATION,
            ease: 'bounce.out',
            stagger: {
                each: TEXT_DROP_STAGGER,
                from: 'start',
            },
        })
        .call(() => {
            textViewRef.value?.playTextAnimation();
        })
        .to({}, { duration: TEXT_PREVIEW_DURATION });
};

const finishLoading = () => {
    if (loadingPhase.value === 'idle' || loadingPhase.value === 'erasing') return;

    if (loadingPhase.value === 'covering' || loadingPhase.value === 'textEntering') {
        shouldEraseAfterCover = true;
        return;
    }

    playEraseAnimation();
};

const handleTriggerClick = () => {
    if (loadingPhase.value === 'idle') {
        startLoading();
        return;
    }

    if (loadingPhase.value === 'covered') {
        finishLoading();
    }
};

onMounted(async () => {
    await nextTick();
    gsap.set(loadingRef.value, { autoAlpha: 0 });
    resetPaths();
    resetLoadingText();
});

onUnmounted(() => {
    loadingTimeline?.kill();
    loadingTimeline = null;
});

defineExpose({
    startLoading,
    finishLoading,
});
</script>

<template>
    <button class="loading-trigger" :disabled="isTriggerDisabled" @click="handleTriggerClick">
        {{ triggerLabel }}
    </button>

    <Teleport to="body">
        <div id="Loading" ref="loadingRef" aria-hidden="true">
            <svg class="loading-svg" viewBox="0 0 100 100" preserveAspectRatio="none">
                <defs>
                    <mask id="loading-zigzag-mask" maskUnits="userSpaceOnUse" x="-100" y="-100" width="300"
                        height="300">
                        <rect x="-100" y="-100" width="300" height="300" fill="black" />
                        <path ref="coverPathRef" class="loading-path loading-path-cover" :d="zigZagPath"
                            :stroke-width="PATH_STROKE_WIDTH" />
                        <path ref="erasePathRef" class="loading-path loading-path-erase" :d="zigZagPath"
                            :stroke-width="PATH_STROKE_WIDTH" />
                    </mask>
                </defs>

                <g mask="url(#loading-zigzag-mask)">
                    <rect class="loading-fill" x="-100" y="-100" width="300" height="300" />
                    <foreignObject class="loading-text-object" x="0" y="0" width="100" height="100">
                        <div xmlns="http://www.w3.org/1999/xhtml" ref="textLayerRef" class="loading-text-layer">
                            <TextView ref="textViewRef" :autoplay="false" />
                        </div>
                    </foreignObject>
                </g>
            </svg>
        </div>
    </Teleport>
</template>

<style lang="scss" scoped>
.loading-trigger {
    position: fixed;
    right: 24px;
    bottom: 24px;
    z-index: 10000;
    border: 0;
    border-radius: 6px;
    padding: 10px 16px;
    color: #111;
    background: #fff;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.18);
    font-size: 14px;
    line-height: 1;
    cursor: pointer;

    &:disabled {
        cursor: not-allowed;
        opacity: 0.72;
    }
}

#Loading {
    position: fixed;
    inset: 0;
    z-index: 9999;
    width: 100vw;
    height: 100vh;
    pointer-events: none;
    visibility: hidden;
    opacity: 0;
}

.loading-svg {
    position: absolute;
    inset: 0;
    display: block;
    width: 100%;
    height: 100%;
}

.loading-text-object {
    overflow: visible;
}

.loading-text-layer {
    --kaomoji-font-size: 6px;
    --kaomoji-padding: 0;
    --kaomoji-radius: 0;
    --kaomoji-stroke-width: 0.45px;
    --kaomoji-shadow: drop-shadow(0 0.55px 0.45px rgba(0, 0, 0, 0.3));

    width: 100%;
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    visibility: hidden;
    opacity: 0;
    will-change: opacity;
}

.loading-fill {
    fill: #fff;
}

.loading-path {
    fill: none;
    stroke-linecap: square;
    stroke-linejoin: bevel;
}

.loading-path-cover {
    stroke: #fff;
}

.loading-path-erase {
    stroke: #000;
}
</style>
