<script setup lang="ts">
import { ref, onMounted, onUnmounted, useTemplateRef, watch } from 'vue'
import * as THREE from 'three'
import Stats from 'three/addons/libs/stats.module.js'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { Sky } from 'three/addons/objects/Sky.js'
import { ThreeMmdLoader } from '@yohawing/three-mmd-loader';

import { useGameStore } from '@/features/game/stores'
import { storeToRefs } from 'pinia'

const gameStore = useGameStore()

// 使用 storeToRefs 解构出响应式的 state 和 getters
const { currentCharacterId, currentMotionId } = storeToRefs(gameStore)
const { currentCharacter, currentMotion, GetCharacterById, GetMotionById } = useGameStore()


// ===================== 响应式引用 =====================
const model = useTemplateRef<HTMLDivElement>('model')
// 当前加载的模型对象
let currentModel: any = null
// 当前模型是否已加载
const isModelLoaded = ref(false)

// ===================== Three.js 核心对象 =====================
let renderer: THREE.WebGLRenderer
let scene: THREE.Scene
let camera: THREE.PerspectiveCamera
let controls: OrbitControls
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

// ========== 动画循环（修改） ==========
const animate = (): void => {
    timer.update()
    const elapsedTime = timer.getElapsed(); // 获取从开始经过的总秒数
    if (currentModel) {
        currentModel.update(elapsedTime); // 传入绝对时间
    }
    controls.update()
    stats.update()
    renderer.render(scene, camera)
}

// ===================== 初始化场景 =====================
const initScene = async (container: HTMLDivElement): Promise<void> => {
    // ----- 渲染器 -----
    renderer = new THREE.WebGLRenderer({ antialias: true, failIfMajorPerformanceCaveat: false, alpha: true })
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
    camera.position.set(0, 0, 0)

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

    // 启动动画循环（模型加载完成后才启动）
    renderer.setAnimationLoop(animate)

    // ----- 窗口自适应事件 -----
    window.addEventListener('resize', onResize)
}

// 递归清理网格、材质、纹理、几何体
function disposeObject(obj: THREE.Object3D) {
    if (!obj) return
    // 1. 处理网格
    if (obj instanceof THREE.Mesh) {
        // 清理几何体
        if (obj.geometry) {
            obj.geometry.dispose()
        }

        // 清理材质（MMD 通常有多组材质）
        const materials = Array.isArray(obj.material) ? obj.material : [obj.material]
        for (const material of materials) {
            // 清理纹理贴图
            if (material.map) material.map.dispose()
            if (material.lightMap) material.lightMap.dispose()
            if (material.bumpMap) material.bumpMap.dispose()
            if (material.normalMap) material.normalMap.dispose()
            if (material.specularMap) material.specularMap.dispose()

            // 清理材质本身
            material.dispose()
        }
    }

    // 2. 递归遍历所有子对象
    while (obj.children.length > 0) {
        const child = obj.children[0]
        if (child) {               // 显式检查 child 存在
            disposeObject(child)
            obj.remove(child)
        }
    }
}

// ========== 切换处理函数 ==========
const loadCharacter = async (characterId: string) => {
    // 1. 清理旧模型
    if (currentModel) {
        // 先停止动画（如果有）
        if (typeof currentModel.stopAnimation === 'function') {
            currentModel.stopAnimation()
        }

        // 从场景移除
        scene.remove(currentModel.root)

        // 递归释放 GPU 内存
        disposeObject(currentModel.root)

        // 如果库本身提供了 dispose 方法，也调用一下
        if (typeof currentModel.dispose === 'function') {
            currentModel.dispose()
        }

        currentModel = null
        isModelLoaded.value = false
    }

    // 2. 查找角色配置
    const character = GetCharacterById(characterId)
    if (!character) return

    // 3. 加载新模型
    try {
        const loader = new ThreeMmdLoader()
        currentModel = await loader.loadModel(character.modelPath)
        // 设置位置/缩放等
        currentModel.root.position.set(0, 0, 0)
        currentModel.root.scale.set(0.9, 0.9, 0.9)
        scene.add(currentModel.root)
        isModelLoaded.value = true

        // 4. 加载默认动作（如果有）
        if (currentMotionId.value) {
            // 否则加载当前选中的动作（可能属于另一个角色，但路径要存在）
            const motion = GetMotionById(currentMotionId.value)
            if (motion) {
                await loadMotion(motion.path)
            }
        } else if (character.defaultMotion) {
            await loadMotion(character.defaultMotion)
        }
    } catch (error) {
        console.error('加载角色失败:', error)
    }
}

const loadMotion = async (motionPath: string) => {
    if (!currentModel) {
        console.warn('请先加载模型')
        return
    }
    try {
        const loader = new ThreeMmdLoader()
        const { animation } = await loader.loadAnimation(motionPath)
        console.log('动画', animation);
        // 应用动画，并开启循环
        currentModel.setAnimation(animation)
    } catch (error) {
        console.error('加载动作失败:', error)
    }
}

// ========== 监听切换 ==========
// 监听角色切换
watch(currentCharacterId, (newId) => {
    if (newId) {
        loadCharacter(newId)
    }
}, { immediate: true }) // 立即执行一次，加载默认角色

// 监听动作切换（仅当模型已加载时才生效）
watch(currentMotionId, (newId) => {
    if (!isModelLoaded.value) return
    const motion = GetMotionById(newId)
    if (motion) {
        loadMotion(motion.path)
    }
})



// ===================== 清理资源（增强） =====================
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
    if (currentModel && typeof currentModel.dispose === 'function') {
        currentModel.dispose()
        currentModel = null
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
    <!-- UI 控制面板（浮层） -->
    <div class="controls">
        <div class="control-group">
            <label>角色：</label>
            <select v-model="currentCharacterId">
                <option v-for="char in currentCharacter" :key="char.id" :value="char.id">
                    {{ char.name }}
                </option>
            </select>
        </div>
        <div class="control-group">
            <label>动作：</label>
            <select v-model="currentMotionId">
                <option v-for="motion in currentMotion" :key="motion.id" :value="motion.id">
                    {{ motion.name }}
                </option>
            </select>
        </div>
    </div>
</template>

<style>
.model {
    width: 100%;
    height: 100%;
}

.controls {
    position: absolute;
    bottom: 30px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    gap: 20px;
    background: rgba(0, 0, 0, 0.7);
    padding: 12px 24px;
    border-radius: 8px;
    color: white;
    font-size: 14px;
}

.control-group {
    display: flex;
    align-items: center;
    gap: 8px;
}

select {
    padding: 4px 8px;
    border-radius: 4px;
    border: none;
    background: #333;
    color: white;
    cursor: pointer;
}
</style>