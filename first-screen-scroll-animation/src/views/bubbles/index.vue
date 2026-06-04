<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';
const n = 6;
interface Bubble {
    x: string,
    s: string,
    d: string,
}
const bubbleList = ref<Bubble[]>([])




// 创建气泡
const vw = window?.innerWidth
function createBubbles() {
    const list = []
    for (let i = 0; i <= n; i++) {
        const s = Math.random() * (vw / 10) + 50;
        const x = Math.random() * (vw - s);
        const d = Math.random() * 2 + 1;
        list.push({
            s: `${s}px`,
            x: `${x}px`,
            d: `${d}s`,
        })
    }
    bubbleList.value.push(...list)
}
onMounted(() => {
    createBubbles()
    setInterval(createBubbles, 500)
})
</script>

<template>
    <div class="footer">
        <div class="bubbles" ref="bubbles" @animationend="(e) => {
            e.target?.remove()
        }">
            <div v-for="(bubble, index) in bubbleList" :key="index" class="bubble" :style="{
                '--s': bubble.s, '--x': bubble.x, '--d': bubble.d,
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
.footer {
    position: fixed;
    bottom: 0px;
    width: 100%;
    height: 50%;
    background: red;

    .bubbles {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 1rem;
        filter: url(#blob);
        background: inherit;

        .bubble {
            position: absolute;
            --x: 100px;
            --s: 50px;
            --d: 2s;
            width: var(--s);
            height: var(--s);
            left: var(--x);
            border-radius: 50%;
            background: inherit;
            top: 30px;
            animation: bubbling var(--d) ease-in forwards;
        }
    }
}



@keyframes bubbling {
    75% {
        transform: scale(1);
    }

    to {
        transform: scale(0);
        top: -200px;
    }
}
</style>
