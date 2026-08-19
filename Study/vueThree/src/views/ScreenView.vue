<script setup lang="ts">
import { useTemplateRef, onMounted } from 'vue'
import * as THREE from 'three'
const screen = useTemplateRef('screen')
// 创建一个场景
const scene = new THREE.Scene()
scene.background = null
// 创建一个透视相机
const camera = new THREE.PerspectiveCamera(
  45,
  window.innerWidth / window.innerHeight,
  0.1,
  2000
)

// 设置相机位置
camera.position.set(0, 10, 1000)
scene.add(camera)

// 添加物体
const geometry = new THREE.BoxGeometry(300, 300, 100)
// const material = new THREE.MeshPhysicalMaterial({ color: 0xff0000 })
const material = new THREE.MeshStandardMaterial({ color: 0xff0000 })
material.roughness = 0.5 // 设置粗糙度
material.metalness = 0.5 // 设置金属度
const mesh = new THREE.Mesh(geometry, material)
mesh.position.set(0, 0, 0)
scene.add(mesh)

// 添加光源
const pointLight = new THREE.DirectionalLight(0xffffff, 1)
pointLight.intensity = 1// 设置光源强度
pointLight.castShadow = true// 设置光源投射阴影
pointLight.position.set(10, 200, 10)// 设置光源位置
scene.add(pointLight)

// 创建一个渲染器
const renderer = new THREE.WebGLRenderer({
  alpha: true
})
// 设置渲染器的尺寸
renderer.setSize(window.innerWidth, window.innerHeight)
console.log(renderer)
// 将渲染器的dom元素添加到body中
screen.value?.appendChild(renderer.domElement)
// 使用渲染器，通过相机将场景渲染出来
renderer.render(scene, camera)

// 摄像头移动，通过传入偏移量xyz
function moveCamera(x: number, y: number, z: number) {
  camera.position.x += x * 10
  camera.position.y += y * 10
  camera.position.z += z * 10
  console.log(camera.position)
  renderer.render(scene, camera)
}

// 监听键盘事件
window.addEventListener('keydown', (event) => {
  switch (event.key) {
    case 'w':
      moveCamera(0, 0, -1)
      break
    case 's':
      moveCamera(0, 0, 1)
      break
    case 'a':
      moveCamera(-1, 0, 0)
      break
    case 'd':
      moveCamera(1, 0, 0)
      break
    case ' ':
      moveCamera(0, 1, 0)
      break
    case 'Shift':
      moveCamera(0, -1, 0)
      break
  }
})

// 摄像头转向，通过鼠标拖动的偏移量计算
function rotateCamera(x: number, y: number) {
  camera.rotation.x -= y
  camera.rotation.y -= x
  console.log(camera.rotation)
  renderer.render(scene, camera)
}

// 监听鼠标事件
let isDragging = false
let previousMousePosition = {
  x: 0,
  y: 0,
}
window.addEventListener('mousedown', (event) => {
  isDragging = true
  previousMousePosition = {
    x: event.clientX,
    y: event.clientY,
  }
})
window.addEventListener('mousemove', (event) => {
  if (isDragging) {
    const deltaMove = {
      x: event.clientX - previousMousePosition.x,
      y: event.clientY - previousMousePosition.y,
    }
    rotateCamera(deltaMove.x * 0.01, deltaMove.y * 0.01)
    previousMousePosition = {
      x: event.clientX,
      y: event.clientY,
    }
  }
})
window.addEventListener('mouseup', () => {
  isDragging = false
})
onMounted(() => {
  if (screen.value) {
    screen.value.appendChild(renderer.domElement)
    const width = screen.value.clientWidth
    const height = screen.value.clientHeight
    renderer.setSize(width, height)
    camera.aspect = width / height
    camera.updateProjectionMatrix()
  }
})
</script>

<template>
  <div class="screen" ref="screen">

  </div>
</template>

<style>
.screen {
  width: 100%;
  height: 100%;
}

@media (min-width: 1024px) {
  .screen {
    min-height: 100vh;
    display: flex;
    align-items: center;
  }
}
</style>
