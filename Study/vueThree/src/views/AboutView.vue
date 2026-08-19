<script setup lang="ts">
import { useTemplateRef, onMounted } from 'vue'
import * as THREE from 'three'
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js'

const container = useTemplateRef('example')
const width = container.value?.clientWidth || 0
const height = container.value?.clientHeight || 0

// 1) 场景
const scene = new THREE.Scene()
// scene.background = new THREE.Color(0x1a1a2e)
scene.background = null

// 2) 相机
const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000)
camera.position.set(0, 5, 15)

// 3) 渲染器
const renderer = new THREE.WebGLRenderer({
  antialias: true,
  alpha: true
})
renderer.setSize(width, height)
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2)) // 限制 2 倍，高分屏别硬扛
renderer.outputColorSpace = THREE.SRGBColorSpace              // 颜色不发灰的关键


// 4) 控制器
const controls = new OrbitControls(camera, renderer.domElement)
controls.enableDamping = true    // 惯性拖拽，手感提升巨大

// 5) 灯光
scene.add(new THREE.AmbientLight(0xffffff, 0.8))
const dirLight = new THREE.DirectionalLight(0xffffff, 1.2)
dirLight.position.set(15, 30, 20)
scene.add(dirLight)

// 6) 一个方块
const cube = new THREE.Mesh(
  new THREE.BoxGeometry(4, 4, 4),
  new THREE.MeshStandardMaterial({ color: 0x4ade80 })
)
scene.add(cube)

// 7) 渲染循环
function animate() {
  requestAnimationFrame(animate)
  controls.update()
  renderer.render(scene, camera)
}
animate()

// 8) 自适应
window.addEventListener('resize', () => {
  const w = container.value?.clientWidth || 0, h = container.value?.clientHeight || 0
  camera.aspect = w / h
  camera.updateProjectionMatrix()   // 改了相机参数必须调这一句
  renderer.setSize(w, h)
})

onMounted(() => {
  if (container.value) {
    container.value.appendChild(renderer.domElement)
    const width = container.value.clientWidth
    const height = container.value.clientHeight
    renderer.setSize(width, height)
    camera.aspect = width / height
    camera.updateProjectionMatrix()
  }
})
</script>

<template>
  <div class="example" ref="example">
  </div>
</template>

<style>
.example {
  width: 100%;
  height: 100%;
}

@media (min-width: 1024px) {
  .example {
    min-height: 100vh;
    display: flex;
    align-items: center;
  }
}
</style>
