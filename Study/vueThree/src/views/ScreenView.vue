<script setup lang="ts">
import * as THREE from 'three'
// 创建一个场景
const scene = new THREE.Scene()
// 创建一个透视相机
const camera = new THREE.PerspectiveCamera(
  75,
  window.innerWidth / window.innerHeight,
  0.1,
  1000
)
// 设置相机位置
camera.position.set(0, 10, 100)
scene.add(camera)
// 添加物体
const geometry = new THREE.BoxGeometry(10, 10, 10)
const material = new THREE.MeshBasicMaterial({ color: 0xff0000 })
const mesh = new THREE.Mesh(geometry, material)
mesh.position.set(0, 0, 0)
scene.add(mesh)
// 创建一个渲染器
const renderer = new THREE.WebGLRenderer()
// 设置渲染器的尺寸
renderer.setSize(window.innerWidth, window.innerHeight)
console.log(renderer)
// 将渲染器的dom元素添加到body中
document.body.appendChild(renderer.domElement)
// 使用渲染器，通过相机将场景渲染出来
renderer.render(scene, camera)

// 摄像头移动，通过传入偏移量xyz
function moveCamera(x: number, y: number, z: number) {
  camera.position.x += x
  camera.position.y += y
  camera.position.z += z
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

</script>

<template>
  <div class="screen">
    <!-- 控制器 -->
  </div>
</template>

<style>
@media (min-width: 1024px) {
  .screen {
    min-height: 100vh;
    display: flex;
    align-items: center;
  }
}
</style>
