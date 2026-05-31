<template>
    <div class="kaomoji-wrapper">
        <div class="kaomoji-container" ref="containerRef">
            <span v-for="(char, index) in chars" :key="index" class="char" :style="{ '--hue-offset': index * 6 }">
                {{ char }}
            </span>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';
import gsap from 'gsap';

const props = withDefaults(defineProps<{
    text?: string;
    autoplay?: boolean;
}>(), {
    text: '!!!(づ￣ 3￣)づ╭❤～',
    autoplay: true,
});

const chars = computed(() => props.text.split(''));
const containerRef = ref<HTMLElement | null>(null);
const colorAnim = { baseHue: 0 };

let colorTween: gsap.core.Tween | null = null;
let waveTimeline: gsap.core.Timeline | null = null;

const getCharElements = () => {
    return Array.from(containerRef.value?.querySelectorAll<HTMLElement>('.char') ?? []);
};

const pauseTextAnimation = () => {
    colorTween?.pause();
    waveTimeline?.pause();
};

const playTextAnimation = () => {
    colorTween?.play();
    waveTimeline?.play();
};

const prepareIntro = () => {
    const charElements = getCharElements();

    pauseTextAnimation();
    gsap.set(charElements, {
        autoAlpha: 0,
        y: -140,
        rotation: -8,
    });
};

const resetTextAnimation = () => {
    const charElements = getCharElements();

    pauseTextAnimation();
    waveTimeline?.pause(0);
    colorTween?.pause(0);
    colorAnim.baseHue = 0;
    containerRef.value?.style.setProperty('--base-hue', '0');

    gsap.set(charElements, {
        autoAlpha: props.autoplay ? 1 : 0,
        y: props.autoplay ? 0 : -140,
        rotation: props.autoplay ? 0 : -8,
    });

    if (props.autoplay) {
        playTextAnimation();
    }
};

const createTextAnimation = () => {
    if (!containerRef.value) return;

    const charElements = getCharElements();

    if (!charElements.length) return;

    colorTween = gsap.to(colorAnim, {
        baseHue: -360,
        duration: 12,
        repeat: -1,
        paused: true,
        ease: 'none',
        onUpdate: () => {
            containerRef.value?.style.setProperty('--base-hue', String(colorAnim.baseHue));
        },
    });

    waveTimeline = gsap.timeline({ repeat: -1, repeatDelay: 0.5, paused: true });

    waveTimeline.to(charElements, {
        keyframes: [
            { y: -8, rotation: 5, duration: 0.35, ease: 'power2.out' },
            { y: 0, rotation: 0, duration: 0.35, ease: 'power2.inOut' },
        ],
        stagger: {
            each: 0.08,
            from: 'start',
        },
    });

    resetTextAnimation();
};

onMounted(async () => {
    await nextTick();
    createTextAnimation();
});

onUnmounted(() => {
    colorTween?.kill();
    waveTimeline?.kill();
    colorTween = null;
    waveTimeline = null;
});

defineExpose({
    prepareIntro,
    playTextAnimation,
    pauseTextAnimation,
    resetTextAnimation,
});
</script>

<style scoped lang="scss">
.kaomoji-wrapper {
    padding: var(--kaomoji-padding, 40px 60px);
    border-radius: var(--kaomoji-radius, 16px);
    display: inline-flex;
    justify-content: center;
    align-items: center;
    overflow: hidden;
    height: 100%;
    width: 100%;
}

.kaomoji-container {
    display: inline-flex;
    --base-hue: 0;
}

.char {
    display: inline-block;
    font-size: var(--kaomoji-font-size, 3.5rem);
    font-weight: 900;
    font-family: "Comic Sans MS", "PingFang SC", "Arial Rounded MT Bold", sans-serif;
    color: hsl(calc(var(--base-hue) + var(--hue-offset)), 85%, 78%);
    -webkit-text-stroke: var(--kaomoji-stroke-width, 4px) white;
    paint-order: stroke fill;
    filter: var(--kaomoji-shadow, drop-shadow(0 4px 3px rgba(0, 0, 0, 0.3)));
    white-space: pre;
    transform-origin: bottom center;
}
</style>
