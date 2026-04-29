<script setup lang="ts">
import './style.css'
import { onMounted, onUnmounted, ref, useTemplateRef } from 'vue';
import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';
import { ScrollSmoother } from 'gsap/ScrollSmoother';

gsap.registerPlugin(ScrollTrigger, ScrollSmoother);

const mainRef = useTemplateRef('mainRef');
let smoother;
let ctx;

const scrollTo = () => {
    smoother.scrollTo('.box-c', true, 'center center');
};

onMounted(() => {
    if (mainRef.value)
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
                end: '+=200',
                markers: true,
            });
        }, mainRef.value);
});
onUnmounted(() => {
    ctx.revert();
});
</script>

<template>
    <div id="smooth-wrapper" ref="mainRef">
        <div id="smooth-content">
            <header class="header">
                <h1 class="title">GreenSock ScrollSmoother on a Vue3 App</h1>
                <button class="button" @click="scrollTo">Jump to C</button>
            </header>
            <div class="box box-a gradient-purple" data-speed="0.5">a</div>
            <div class="box box-b gradient-green" data-speed="0.8">b</div>
            <div class="box box-c gradient-orange" data-speed="1.5">c</div>
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
