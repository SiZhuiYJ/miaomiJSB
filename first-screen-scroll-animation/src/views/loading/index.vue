<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';
import gsap from 'gsap';

type LoadingPhase = 'idle' | 'covering' | 'covered' | 'erasing';

const loadingRef = ref<HTMLElement | null>(null);
const coverPathRef = ref<SVGPathElement | null>(null);
const erasePathRef = ref<SVGPathElement | null>(null);
const loadingPhase = ref<LoadingPhase>('idle');
const COVER_DURATION = 4.6; // 封面动画时长
const ERASE_DURATION = 4.4; // 擦除动画时长
const FOLD_COUNT = 7; // 折叠次数，数字越大折返越密
const PATH_START_X = -30; // 封面路径起始点 X 坐标
const PATH_END_X = 130; // 封面路径结束点 X 坐标
const PATH_START_Y = -24; // 封面路径起始点 Y 坐标
const PATH_END_Y = 136; // 封面路径结束点 Y 坐标
const triggerLabel = computed(() => {
    const labelMap: Record<LoadingPhase, string> = {
        idle: '开始加载',
        covering: '加载中',
        covered: '完成加载',
        erasing: '结束中',
    };

    return labelMap[loadingPhase.value];
});
const isTriggerDisabled = computed(() => loadingPhase.value === 'covering' || loadingPhase.value === 'erasing');

const buildZigZagPath = (foldCount: number) => {
    const safeFoldCount = Math.max(1, Math.floor(foldCount));

    return Array.from({ length: safeFoldCount + 1 }, (_, index) => {
        const command = index === 0 ? 'M' : 'L';
        const x = index % 2 === 0 ? PATH_START_X : PATH_END_X;
        const progress = index / safeFoldCount;
        const y = PATH_START_Y + (PATH_END_Y - PATH_START_Y) * progress;

        return `${command} ${x} ${Number(y.toFixed(2))}`;
    }).join(' ');
};

const zigZagPath = buildZigZagPath(FOLD_COUNT);

let loadingTimeline: gsap.core.Timeline | null = null;
let shouldEraseAfterCover = false;

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

    loadingTimeline = gsap.timeline({
        defaults: {
            ease: 'power2.inOut',
        },
        onComplete: () => {
            loadingPhase.value = 'idle';
            gsap.set(loading, { autoAlpha: 0 });
            resetPaths();
        },
    });

    loadingTimeline.to(erasePath, {
        strokeDashoffset: 0,
        duration: ERASE_DURATION,
        ease: 'power2.inOut',
    });
};

const startLoading = async () => {
    if (loadingPhase.value !== 'idle') return;

    await nextTick();

    const loading = loadingRef.value;
    const coverPath = coverPathRef.value;

    if (!loading || !coverPath) return;

    loadingTimeline?.kill();
    shouldEraseAfterCover = false;
    resetPaths();

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
        });
};

const finishLoading = () => {
    if (loadingPhase.value === 'idle' || loadingPhase.value === 'erasing') return;

    if (loadingPhase.value === 'covering') {
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

onMounted(() => {
    gsap.set(loadingRef.value, { autoAlpha: 0 });
    resetPaths();
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

    <div id="Loading" ref="loadingRef" aria-hidden="true">
        <svg class="loading-svg" viewBox="0 0 100 100" preserveAspectRatio="none">
            <defs>
                <mask id="loading-zigzag-mask" maskUnits="userSpaceOnUse" x="-40" y="-40" width="180" height="180">
                    <rect x="-40" y="-40" width="180" height="180" fill="black" />
                    <path ref="coverPathRef" class="loading-path loading-path-cover" :d="zigZagPath" />
                    <path ref="erasePathRef" class="loading-path loading-path-erase" :d="zigZagPath" />
                </mask>
            </defs>

            <rect class="loading-fill" x="-40" y="-40" width="180" height="180" mask="url(#loading-zigzag-mask)" />
        </svg>
    </div>
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
    display: block;
    width: 100%;
    height: 100%;
}

.loading-fill {
    fill: #fff;
}

.loading-path {
    fill: none;
    stroke-width: 72;
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
