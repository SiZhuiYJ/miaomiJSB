<script setup lang="ts">
import './style.css'
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

const mainRef = useTemplateRef('mainRef');
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
    if (!mainRef.value)
        return;

    ctx = gsap.context(() => {
        // create the smooth scroller FIRST!
        smoother = ScrollSmoother.create({
            smooth: 2, // seconds it takes to "catch up" to native scroll position
            effects: true, // look for data-speed and data-lag attributes on elements and animate accordingly
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
    }, mainRef.value);
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
            <div class="box box-a gradient-purple" data-speed="0.5">a</div>
            <div class="box box-b gradient-green" data-speed="0.8">b</div>
            <div class="box box-c gradient-orange" data-speed="1.5">c</div>
            <div class="box box-d gradient-red" data-speed="1.8">d</div>
            <div class="line"></div>
        </div>
    </div>
    <footer>
        <a href="https://greensock.com/scrollsmoother">
            <img class="greensock-icon"
                src="https://s3-us-west-2.amazonaws.com/s.cdpn.io/16327/scroll-smoother-logo-light.svg" width="220"
                height="70" />
        </a>
    </footer>
</template>
