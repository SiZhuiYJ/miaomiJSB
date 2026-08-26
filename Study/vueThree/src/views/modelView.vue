<script setup lang="ts">
import { ref, onMounted, onUnmounted, useTemplateRef } from 'vue'
import * as THREE from 'three'
import Stats from 'three/addons/libs/stats.module.js'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { Sky } from 'three/addons/objects/Sky.js'
import { SkeletonHelper } from 'three'
import { ThreeMmdLoader } from '@yohawing/three-mmd-loader';
// ===================== 响应式引用 =====================
const model = useTemplateRef<HTMLDivElement>('model')
const loader = new ThreeMmdLoader()
// ===================== Three.js 核心对象 =====================
let renderer: THREE.WebGLRenderer
let scene: THREE.Scene
let camera: THREE.PerspectiveCamera
let controls: OrbitControls
let hutao: any
let timer: THREE.Timer
let stats: Stats
let pmremGenerator: THREE.PMREMGenerator

// ===================== 窗口自适应 =====================
const onResize = (): void => {
    if (!camera || !renderer) return
    camera.aspect = window.innerWidth / window.innerHeight
    camera.updateProjectionMatrix()
    renderer.setSize(window.innerWidth, window.innerHeight)
}

// ===================== 动画循环 =====================
const animate = (): void => {
    timer.update()
    const delta = timer.getDelta()
    if (hutao)
        hutao.update(delta)
    controls.update()
    stats.update()
    renderer.render(scene, camera)
}

// ===================== 初始化场景 =====================
const initScene = async (container: HTMLDivElement): Promise<void> => {
    // ----- 渲染器 -----
    renderer = new THREE.WebGLRenderer({ antialias: true })
    renderer.setPixelRatio(window.devicePixelRatio)
    renderer.setSize(window.innerWidth, window.innerHeight)
    renderer.toneMapping = THREE.ACESFilmicToneMapping
    container.appendChild(renderer.domElement)

    // ----- 场景 -----
    scene = new THREE.Scene()

    // ----- 天空盒 (Sky) -----
    const sky = new Sky()
    sky.scale.setScalar(10000)
    scene.add(sky)

    const uniforms = sky.material.uniforms
    uniforms['turbidity']!.value = 0
    uniforms['rayleigh']!.value = 3
    uniforms['mieDirectionalG']!.value = 0.7
    uniforms['cloudElevation']!.value = 1
    uniforms['sunPosition']!.value.set(-0.8, 0.19, 0.56) // elevation: 11, azimuth: -55

    // ----- 生成环境贴图（使用临时场景包含 sky） -----
    pmremGenerator = new THREE.PMREMGenerator(renderer)
    const environment = pmremGenerator.fromScene(sky as any).texture
    scene.environment = environment
    // 可选：释放临时场景（pmremGenerator 会保留纹理）
    // skyScene 会被垃圾回收

    // ----- 相机 -----
    camera = new THREE.PerspectiveCamera(40, window.innerWidth / window.innerHeight, 1, 100)
    camera.position.set(5, 2, 8)

    // ----- 轨道控制器 -----
    controls = new OrbitControls(camera, renderer.domElement)
    controls.enableDamping = true
    controls.target.set(0, 0.7, 0)
    controls.update()

    // ----- 计时器 (替代 Clock) -----
    timer = new THREE.Timer()
    timer.connect(document)

    // ----- 性能监控 Stats -----
    stats = new Stats()
    container.appendChild(stats.dom)

    // ----- 加载模型 (mmd) -----
    // hutao = await loader.loadModel('public/models/hutao/hutao/hutao.pmx')
    // hutao = await loader.loadModel('public/models/mmd/【桑多涅】_by_原神_e06bb339ac99ae18f7cf88d619e9b975/桑多涅.pmx')
    hutao = await loader.loadModel('public/models/mmd/星穹铁道—流萤·春日手信_by_崩坏：星穹铁道_948486d4ddcc6988bd90019585983d7a/星穹铁道—流萤·春日手信/星穹铁道—流萤·春日手信.pmx')
    hutao.root.position.set(0, 0, 0)
    hutao.root.scale.set(0.1, 0.1, 0.1)
    scene.add(hutao.root)

    const skeletonHelper = new SkeletonHelper(hutao.root)
    skeletonHelper.visible = false // 设置为 true 可以显示骨骼线框
    scene.add(skeletonHelper)

    // ----- 加载动画 (mmd) -----
    const { animation } = await loader.loadAnimation('public/models/hutao/move/荧-嚣张.vmd')
    // const { animation } = await loader.loadAnimation('public/models/hutao/move/ayaka-dance.vmd')
    console.log('动画数据:', animation)
    hutao.setAnimation(animation, true)

    // 启动动画循环（模型加载完成后才启动）
    renderer.setAnimationLoop(animate)

    // ----- 窗口自适应事件 -----
    window.addEventListener('resize', onResize)
}

// ===================== 清理资源 =====================
const disposeScene = (): void => {
    // 停止动画循环
    renderer?.setAnimationLoop(null)

    // 移除事件监听
    window.removeEventListener('resize', onResize)

    // 断开 Timer 连接
    timer?.disconnect()

    // 销毁渲染器
    renderer?.dispose()

    // 释放 PMREMGenerator
    pmremGenerator?.dispose()

    // 移除 stats DOM
    if (stats?.dom && stats.dom.parentNode) {
        stats.dom.parentNode.removeChild(stats.dom)
    }

    // 移除 renderer DOM
    if (renderer?.domElement && renderer.domElement.parentNode) {
        renderer.domElement.parentNode.removeChild(renderer.domElement)
    }
}

// ===================== 生命周期 =====================
onMounted(() => {
    const container = model.value
    if (!container) {
        console.error('容器元素不存在')
        return
    }
    initScene(container)
})

onUnmounted(() => {
    disposeScene()
}) 
</script>

<template>
    <div class="model" ref="model"></div>
</template>

<style>
.model {
    width: 100%;
    height: 100%;
}
</style>