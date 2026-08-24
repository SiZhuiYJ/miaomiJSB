<template>
    <div id="container" ref="containerRef">
        <div id="info">
            <a href="https://threejs.org" target="_blank" rel="noopener">three.js</a> webgl - animation -
            keyframes<br />
            Model:
            <a href="https://artstation.com/artwork/1AGwX" target="_blank" rel="noopener">Littlest Tokyo</a>
            by
            <a href="https://artstation.com/glenatron" target="_blank" rel="noopener">Glen Fox</a>, CC Attribution.
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import * as THREE from 'three'
import Stats from 'three/addons/libs/stats.module.js'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { Sky } from 'three/addons/objects/Sky.js'
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'
import { DRACOLoader, DRACO_GLTF_CONFIG } from 'three/addons/loaders/DRACOLoader.js'

// ===================== 响应式引用 =====================
const containerRef = ref<HTMLDivElement | null>(null)

// ===================== Three.js 核心对象（使用确定赋值断言） =====================
let renderer: THREE.WebGLRenderer
let scene: THREE.Scene
let camera: THREE.PerspectiveCamera
let controls: OrbitControls
let mixer: THREE.AnimationMixer
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
    mixer.update(delta)
    controls.update()
    stats.update()
    renderer.render(scene, camera)
}

// ===================== 初始化场景 =====================
const initScene = (container: HTMLDivElement): void => {
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

    // ----- 加载模型 (GLTF + DRACO) -----
    const dracoLoader = new DRACOLoader()
    dracoLoader.setDecoderPath(DRACO_GLTF_CONFIG)

    const loader = new GLTFLoader()
    loader.setDRACOLoader(dracoLoader)

    loader.load(
        '/models/gltf/LittlestTokyo.glb',
        (gltf) => {
            const model = gltf.scene
            model.position.set(1, 1, 0)
            model.scale.set(0.01, 0.01, 0.01)
            scene.add(model)

            // 修复：检查 animations 是否存在且至少有一个 clip
            const animClip = gltf.animations?.[0]
            if (!animClip) {
                console.warn('模型没有可用的动画片段')
                return
            }

            mixer = new THREE.AnimationMixer(model)
            mixer.clipAction(animClip).play()

            // 启动动画循环（模型加载完成后才启动）
            renderer.setAnimationLoop(animate)
        },
        undefined,
        (error) => {
            console.error('模型加载失败:', error)
            // 在界面上显示错误提示
            const infoDiv = document.getElementById('info')
            if (infoDiv) {
                infoDiv.innerHTML += '<br><span style="color:red;">模型加载失败，请检查文件路径。</span>'
            }
        },
    )

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
    const container = containerRef.value
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

<style scoped>
/* ----- 全局重置 & 容器 ----- */
#container {
    position: relative;
    width: 100vw;
    height: 100vh;
    overflow: hidden;
    background-color: #e2e0e0;
    color: #000;
}

/* ----- 信息面板 (覆盖在右上角) ----- */
#info {
    position: absolute;
    top: 20px;
    left: 50%;
    transform: translateX(-50%);
    z-index: 10;
    font-size: 14px;
    line-height: 1.6;
    text-align: center;
    pointer-events: none;
    background: rgba(255, 255, 255, 0.6);
    padding: 6px 16px;
    border-radius: 8px;
    backdrop-filter: blur(4px);
    user-select: none;
    font-family: 'Segoe UI', 'Helvetica Neue', Arial, sans-serif;
}

#info a {
    color: #2983ff;
    pointer-events: auto;
    text-decoration: none;
    font-weight: 500;
}

#info a:hover {
    text-decoration: underline;
}

/* ----- 覆盖 Stats 默认样式 (微调位置) ----- */
:deep(.stats-panel) {
    position: absolute !important;
    top: auto !important;
    bottom: 20px !important;
    left: 20px !important;
    right: auto !important;
    z-index: 20 !important;
    opacity: 0.85;
}
</style>