<script setup lang="ts">
import { onMounted, onUnmounted, useTemplateRef } from 'vue';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { ScrollSmoother } from 'gsap/ScrollSmoother';

gsap.registerPlugin(ScrollTrigger, ScrollSmoother);

type DemoTarget = {
    key: string;
    label: string;
    selector: string;
    position?: string;
};

const main = useTemplateRef('mainRef');
const video18 = useTemplateRef('video18Ref');


const demoTargets: DemoTarget[] = [
    { key: 'top', label: '回到顶部', selector: '#smooth-content', position: 'top top' },
    { key: 'a', label: '跳转到 A', selector: '.box-a', position: 'center center' },
    { key: 'b', label: '跳转到 B', selector: '.box-b', position: 'center center' },
    { key: 'c', label: '跳转到 C', selector: '.box-c', position: 'center center' },
];

let smoother: ScrollSmoother | null = null;
let ctx: gsap.Context | null = null;

const scrollTo = (selector: string, position: string = 'center center') => {
    if (!smoother)
        return;
    smoother.scrollTo(selector, true, position);
};

onMounted(() => {
    if (!main.value)
        return;

    ctx = gsap.context(() => {
        // create the smooth scroller FIRST!
        smoother = ScrollSmoother.create({
            smooth: 2, // seconds it takes to "catch up" to native scroll position
            effects: true, // look for data-speed and data-lag attributes on elements and animate accordingly
        });
        ScrollTrigger.create({
            trigger: '.box-a',
            start: 'center center',
            end: '+=600',
            scrub: true,
            animation:
                gsap.timeline()
                    .fromTo('.box-img', { scale: 1 }, { scale: 0.8 })
                    .fromTo('.box-video', { width: '80%', height: '80vh', }, { width: '100%', height: '100vh', }, '<')
        });

        ScrollTrigger.create({
            trigger: '.box-b',
            start: 'center center',
            end: '+=5000',
            scrub: true,
            pin: true,
            onUpdate(self) {
                const video = video18.value;
                if (!video) return;
                const dur = video.duration;
                if (isFinite(dur) && dur > 0) {
                    let target = self.progress * dur;
                    target = Math.min(Math.max(target, 0), dur);
                    video.currentTime = target;
                }
            },
            animation: gsap.timeline()
                .to('.text-1', { top: '50%', opacity: 1 })
                .to('.text-1', { top: '0', opacity: 0 })
                .to('.text-2', { top: '60%', opacity: 1 })
                .to('.text-2', { top: '40%', opacity: 0 })
        });

        ScrollTrigger.create({
            trigger: '.box-c',
            pin: true,
            start: 'center center',
            end: '+=600',
            markers: true,
        });

        ScrollTrigger.create({
            trigger: '.box-d',
            pin: true,
            start: 'center center',
            end: '+=600',
            markers: true,
        });
    }, main.value);
});

onUnmounted(() => {
    ctx?.revert();
    ctx = null;
    smoother = null;
});
</script>

<template>
    <div id="smooth-wrapper" ref="mainRef">
        <div id="smooth-content">
            <header class="header">
                <h1 class="title">GreenSock ScrollSmoother on a Vue3 App</h1>
                <div class="button-group">
                    <button v-for="item in demoTargets" :key="item.key" class="button"
                        @click="scrollTo(item.selector, item.position)">
                        {{ item.label }}
                    </button>
                </div>
                <p>示例：点击上面的按钮体验「锚点跳转 + 平滑滚动 + ScrollTrigger 固定动画」。</p>
            </header>
            <div class="box box-a gradient-purple">
                <div class="box-img">
                    <img src="/gsap/cyy/18.jpg" class="image-18" alt="">
                </div>
            </div>
            <div class="box box-b gradient-green">
                <div class="box-video">
                    <video src="/gsap/cyy/4.mp4" ref="video18Ref" class="video-3">
                        您的浏览器不支持视频播放
                    </video>
                    <p class="text-1">
                        怎么看！<br>
                        怎么萌~
                    </p>
                    <p class="text-2">
                        绝世萌妹~
                    </p>
                </div>
            </div>
            <div class="box box-c gradient-orange">
                <div class="box-content">c</div>
            </div>
            <div class="box box-d gradient-red">
                <div class="box-content">d</div>
            </div>
            <div class="line"></div>
        </div>
    </div>
</template>
<style scoped lang="scss">
#smooth-wrapper {
    --color-shockingly-green: #0ae448;
    --color-just-black: #0e100f;
    --color-surface-white: #fffce1;
    --color-pink: #fec5fb;
    --color-shockingly-pink: #f100cb;
    --color-orangey: #ff8709;
    --color-lilac: #9d95ff;
    --color-lt-green: #abff84;
    --color-blue: #00bae2;
    --color-grey: gray;
    --color-surface75: #bbbaa6;
    --color-surface50: #7c7c6f;
    --color-surface25: #42433d;
    --gradient-macha: linear-gradient(114.41deg,
            var(--color-shockingly-green) 20.74%,
            var(--color-lt-green) 65.5%);
    --gradient-orange-crush: linear-gradient(111.45deg,
            var(--color-orangey) 19.42%,
            #f7bdf8 73.08%);
    --gradient-lipstick: linear-gradient(165.72deg,
            #f7bdf8 21.15%,
            #cd237f 81.93%);
    --gradient-purple-haze: linear-gradient(153.58deg,
            #f7bdf8 32.25%,
            #2f3cc0 92.68%);
    --gradient-skyfall: linear-gradient(131.77deg,
            #0a157a 30.82%,
            #15bfe4 81.82%);
    --gradient-emerald-city: linear-gradient(166.9deg,
            var(--color-shockingly-green) 53.19%,
            #0085d0 107.69%);
    --gradient-summer-fair: linear-gradient(144.02deg,
            var(--color-blue) 4.56%,
            var(--color-pink) 72.98%);
}

#smooth-content {
    overflow: visible;
    height: 10000px;
}

.box {
    height: 100vh;
    width: 100%;
    display: flex;
    justify-content: center;


    .box-img {
        display: flex;
        justify-content: center;
        height: 100vh;
        width: 100%;

        img {
            height: 100%;
            width: 100%;
            object-fit: cover;
        }
    }

    .box-video {
        display: flex;
        justify-content: center;
        height: 100vh;
        width: 100%;
        position: relative;

        video {
            height: 100%;
            width: 100%;
            object-fit: cover;
        }

        .text-1 {
            position: absolute;
            top: 100rem;
            left: 11rem;
            opacity: 0;
            font-size: 4rem;
            color: #fff;
        }

        .text-2 {
            position: absolute;
            top: 30rem;
            right: 30%;
            opacity: 0;
            font-size: 8rem;
            color: #fff;
        }
    }

}

.box-a {}

.box-b {}

.gradient-green {
    background: var(--gradient-macha);
    background-blend-mode: color-dodge;
}

.gradient-green-2 {
    background: var(--gradient-emerald-city);
    background-blend-mode: color-dodge;
}

.gradient-orange {
    background: var(--gradient-orange-crush);
    background-blend-mode: color-dodge;
}

.gradient-purple {
    background: var(--gradient-purple-haze);
    background-blend-mode: color-dodge;
}

.gradient-blue-2 {
    background: var(--gradient-summer-fair);
    background-blend-mode: color-dodge;
}

.gradient-blue {
    background: var(--color-ui-gradient);
    background-blend-mode: color-dodge;
}

.gradient-red {
    background: var(--gradient-lipstick);
    background-blend-mode: color-dodge;
}
</style>
