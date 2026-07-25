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
const COVER_DURATION = 1.2; // 封面动画时长
const ERASE_DURATION = 1.2; // 擦除动画时长
const TEXT_DROP_DURATION = 0.85;
const TEXT_DROP_STAGGER = 0.08;
const TEXT_PREVIEW_DURATION = 1.4;
const TEXT_ENTER_START_RATIO = 0.78; // 可见区域基本填满后就开始文字入场，不等待屏幕外路径画完
const FOLD_COUNT = 5; // 屏幕内可见折返拐点数，数字越大折返越密
const PATH_STROKE_WIDTH_RATIO = 1.45; // 路径宽度倍率，边角露底时可以适当调大
const PATH_X_OVERSCAN = 0.7;
const viewportSize = ref({ width: 100, height: 100 });
const visibleSegmentCount = computed(() => Math.max(1, Math.floor(FOLD_COUNT) + 1));
const segmentHeight = computed(() => viewportSize.value.height / visibleSegmentCount.value);
const pathStrokeWidth = computed(() => Math.ceil(segmentHeight.value * PATH_STROKE_WIDTH_RATIO));
const svgViewBox = computed(() => `0 0 ${viewportSize.value.width} ${viewportSize.value.height}`);
const maskBounds = computed(() => {
    const paddingX = viewportSize.value.width * PATH_X_OVERSCAN;
    const paddingY = pathStrokeWidth.value + segmentHeight.value;

    return {
        x: -paddingX,
        y: -paddingY,
        width: viewportSize.value.width + paddingX * 2,
        height: viewportSize.value.height + paddingY * 2,
    };
});
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

const buildZigZagPath = (foldCount: number, width: number, height: number) => {
    const safeFoldCount = Math.max(0, Math.floor(foldCount));
    const visibleSegmentCount = safeFoldCount + 1;
    const segmentHeight = height / visibleSegmentCount;
    const startX = -width * PATH_X_OVERSCAN;
    const endX = width * (1 + PATH_X_OVERSCAN);
    const startY = -segmentHeight;

    return Array.from({ length: visibleSegmentCount + 3 }, (_, index) => {
        const command = index === 0 ? 'M' : 'L';
        const x = index % 2 === 0 ? startX : endX;
        const y = startY + segmentHeight * index;

        return `${command} ${x} ${Number(y.toFixed(2))}`;
    }).join(' ');
};

const zigZagPath = computed(() => buildZigZagPath(
    FOLD_COUNT,
    viewportSize.value.width,
    viewportSize.value.height,
));

let loadingTimeline: gsap.core.Timeline | null = null;
let shouldEraseAfterCover = false;

const updateViewportSize = () => {
    viewportSize.value = {
        width: Math.max(window.innerWidth, 1),
        height: Math.max(window.innerHeight, 1),
    };

    void nextTick(resetPaths);
};

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
        }, 0)
        .call(() => {
            loadingPhase.value = 'textEntering';
            gsap.set(textLayerRef.value, { autoAlpha: 1 });
        }, undefined, COVER_DURATION * TEXT_ENTER_START_RATIO)
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
        }, COVER_DURATION * TEXT_ENTER_START_RATIO)
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
    updateViewportSize();
    window.addEventListener('resize', updateViewportSize);
    gsap.set(loadingRef.value, { autoAlpha: 0 });
    resetPaths();
    resetLoadingText();
});

onUnmounted(() => {
    window.removeEventListener('resize', updateViewportSize);
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
            <svg class="loading-svg" :viewBox="svgViewBox">
                <defs>
                    <mask id="loading-zigzag-mask" maskUnits="userSpaceOnUse" :x="maskBounds.x" :y="maskBounds.y"
                        :width="maskBounds.width" :height="maskBounds.height">
                        <rect :x="maskBounds.x" :y="maskBounds.y" :width="maskBounds.width"
                            :height="maskBounds.height" fill="black" />
                        <path ref="coverPathRef" class="loading-path loading-path-cover" :d="zigZagPath"
                            :stroke-width="pathStrokeWidth" />
                        <path ref="erasePathRef" class="loading-path loading-path-erase" :d="zigZagPath"
                            :stroke-width="pathStrokeWidth" />
                    </mask>
                </defs>

                <g mask="url(#loading-zigzag-mask)">
                    <rect class="loading-fill" x="0" y="0" :width="viewportSize.width" :height="viewportSize.height" />
                    <foreignObject class="loading-text-object" x="0" y="0" :width="viewportSize.width"
                        :height="viewportSize.height">
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
    display: block;
    inset: 0;
    width: 100%;
    height: 100%;
    // 测试标记
    // scale: 0.3;
}

.loading-text-object {
    overflow: visible;
}

.loading-text-layer {
    --kaomoji-font-size: clamp(28px, 4vw, 56px);
    --kaomoji-padding: 0;
    --kaomoji-radius: 0;
    --kaomoji-stroke-width: 3px;
    --kaomoji-shadow: drop-shadow(0 4px 3px rgba(0, 0, 0, 0.3));

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
