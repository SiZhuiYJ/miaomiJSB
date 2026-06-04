<script setup lang="ts">
import { gsap } from 'gsap';
import { nextTick, onMounted, onUnmounted, ref } from 'vue';

const n = 6;

interface Bubble {
    id: number;
    x: string;
    s: string;
}

interface BubbleAnimation {
    id: number;
    duration: number;
    rise: number;
    swayA: number;
    swayB: number;
    frequencyA: number;
    frequencyB: number;
    phaseA: number;
    phaseB: number;
    drift: number;
    spin: number;
}

const bubbleList = ref<Bubble[]>([])
const activeTweens = new Map<number, gsap.core.Tween>();
let bubbleId = 0;
let timer: ReturnType<typeof setInterval> | null = null;

function randomBetween(min: number, max: number) {
    return Math.random() * (max - min) + min
}

function clamp(value: number, min: number, max: number) {
    return Math.min(Math.max(value, min), max)
}

function createBubbleAnimation(id: number, size: number, vh: number): BubbleAnimation {
    const sway = Math.min(Math.max(size * randomBetween(0.35, 0.95), 28), 120);

    return {
        id,
        duration: randomBetween(1.8, 3.6),
        rise: randomBetween(vh * 0.6, vh + size),
        swayA: randomBetween(sway * 0.45, sway),
        swayB: randomBetween(sway * 0.12, sway * 0.36),
        frequencyA: randomBetween(0.85, 1.45),
        frequencyB: randomBetween(1.8, 3.2),
        phaseA: randomBetween(0, Math.PI * 2),
        phaseB: randomBetween(0, Math.PI * 2),
        drift: randomBetween(-sway * 0.35, sway * 0.35),
        spin: randomBetween(-12, 12),
    }
}

function removeBubble(id: number) {
    activeTweens.get(id)?.kill()
    activeTweens.delete(id)
    bubbleList.value = bubbleList.value.filter((bubble) => bubble.id !== id)
}

function animateBubble(animation: BubbleAnimation) {
    const { id, duration, rise, swayA, swayB, frequencyA, frequencyB, phaseA, phaseB, drift, spin } = animation;
    const el = document.querySelector<HTMLElement>(`.bubble[data-id="${id}"]`);

    if (!el) return

    gsap.set(el, {
        x: 0,
        y: 0,
        scale: 1,
        rotation: 0,
        opacity: 1,
        force3D: true,
        transformOrigin: 'center center',
    })

    const setX = gsap.quickSetter(el, 'x', 'px');
    const setY = gsap.quickSetter(el, 'y', 'px');
    const setScale = gsap.quickSetter(el, 'scale');
    const setRotation = gsap.quickSetter(el, 'rotation', 'deg');
    const setOpacity = gsap.quickSetter(el, 'opacity');
    const state = { progress: 0 };

    const tween = gsap.to(state, {
        progress: 1,
        duration,
        ease: 'none',
        onUpdate: () => {
            const progress = state.progress;
            const wave = Math.PI * 2 * progress;
            const x = Math.sin(wave * frequencyA + phaseA) * swayA
                + Math.sin(wave * frequencyB + phaseB) * swayB
                + drift * progress;
            const y = -rise * (1 - Math.pow(1 - progress, 1.25));
            const fade = clamp((progress - 0.78) / 0.22, 0, 1);
            const scale = (1 + Math.sin(progress * Math.PI) * 0.05) * (1 - fade);

            setX(x)
            setY(y)
            setScale(scale)
            setRotation(spin * progress)
            setOpacity(1 - fade)
        },
        onComplete: () => removeBubble(id),
    })

    activeTweens.set(id, tween)
}

function createBubbles() {
    const vw = window.innerWidth
    const vh = window.innerHeight
    const list: Bubble[] = []
    const animations: BubbleAnimation[] = []

    for (let i = 0; i <= n; i++) {
        const id = bubbleId++;
        const s = randomBetween(50, vw / 10 + 50);
        const x = randomBetween(0, vw - s);

        list.push({
            id,
            s: `${s}px`,
            x: `${x}px`,
        })
        animations.push({
            ...createBubbleAnimation(id, s, vh),
        })
    }

    bubbleList.value.push(...list)
    void nextTick().then(() => {
        animations.forEach(animateBubble)
    })
}

onMounted(() => {
    createBubbles()
    timer = setInterval(createBubbles, 500)
})

onUnmounted(() => {
    if (timer) clearInterval(timer)
    activeTweens.forEach((tween) => tween.kill())
    activeTweens.clear()
})
</script>

<template>
    <div class="footer">
        <div class="bubbles" ref="bubbles">
            <div v-for="bubble in bubbleList" :key="bubble.id" class="bubble" :data-id="bubble.id" :style="{
                '--s': bubble.s,
                '--x': bubble.x,
            }">
            </div>
        </div>
    </div>
    <svg style="display: none;">
        <defs>
            <filter id="blob" x="-50%" y="-50%" width="200%" height="200%">
                <feGaussianBlur in="SourceGraphic" stdDeviation="10" result="blur" />
                <feColorMatrix in="blur" mode="matrix" values="
                1 0 0 0 0
                0 1 0 0 0
                0 0 1 0 0
                0 0 0 20 -9" />
            </filter>
        </defs>
    </svg>
</template>

<style scoped lang="scss">
$parent-filter: url(#blob);

.footer {
    position: fixed;
    bottom: 0px;
    width: 100%;
    height: 10%;
    background: red;

    .bubbles {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 1rem;
        filter: $parent-filter;
        background: inherit;

        .bubble {
            position: absolute;
            --x: 100px;
            --s: 50px;
            width: var(--s);
            height: var(--s);
            left: var(--x);
            top: 0;
            border-radius: 50%;
            background: inherit;
            will-change: transform, opacity;
        }
    }
}
</style>
