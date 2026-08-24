<script setup lang="ts">
import { useTemplateRef, onMounted, onBeforeUnmount } from 'vue'
import * as THREE from 'three'
import { Sky } from 'three/examples/jsm/objects/Sky.js'

// 地块基础高度
const baseHeight = 10
const tileSize = 10
const tileHeight = 10

// 地图单元类型
type MapCellType = 0 | 1 | 2 // 0: 空地, 1: 地面（高度10），2: 墙壁（高度20）
// 地图数据（二维数组）
const mapData: MapCellType[][] = [
    [1, 1, 1, 1, 2, 1, 1, 1, 1, 1],
    [1, 2, 1, 1, 1, 1, 1, 1, 1, 1],
    [1, 1, 0, 0, 0, 1, 0, 0, 0, 1],
    [1, 1, 1, 1, 1, 1, 0, 0, 0, 2],
    [1, 1, 0, 1, 0, 1, 1, 1, 1, 1],
    [1, 0, 0, 1, 1, 0, 0, 0, 0, 1],
    [1, 1, 1, 0, 1, 1, 1, 1, 1, 1],
    [1, 1, 1, 1, 1, 1, 1, 2, 1, 2],
]

const game = useTemplateRef<HTMLDivElement>('game')

let scene: THREE.Scene
let camera: THREE.PerspectiveCamera
let renderer: THREE.WebGLRenderer

const timer = new THREE.Timer();
timer.connect(document);

// 摄像头移动（通过偏移量）
function moveCamera(x: number, y: number, z: number) {
    if (!camera) return
    camera.position.x += x * 10
    camera.position.y += y * 10
    camera.position.z += z * 10
    renderer.render(scene, camera)
}

// 键盘事件处理
function onKeyDown(event: KeyboardEvent) {
    switch (event.key) {
        case 'w': moveCamera(0, 0, -1); break
        case 's': moveCamera(0, 0, 1); break
        case 'a': moveCamera(-1, 0, 0); break
        case 'd': moveCamera(1, 0, 0); break
        case ' ': moveCamera(0, 1, 0); break
        case 'Shift': moveCamera(0, -1, 0); break
    }
}

onMounted(() => {
    if (!game.value) return

    // 创建场景
    scene = new THREE.Scene()
    scene.background = null

    // 创建透视相机
    camera = new THREE.PerspectiveCamera(
        45,
        game.value.clientWidth / game.value.clientHeight,
        0.1,
        2000
    )
    camera.rotation.x = -Math.PI / 4
    // 计算地图中心位置
    const rows = mapData.length
    const cols = mapData[0]?.length ?? 0  // 若为空则默认为0（不会发生）
    const mapCenterX = (cols - 1) * tileSize / 2
    const mapCenterZ = (rows - 1) * tileSize * 1.6
    camera.position.set(mapCenterX, 80, mapCenterZ)

    // 遍历地图数据创建物体
    mapData.forEach((row, i) => {
        row.forEach((cellType, j) => {
            if (cellType === 0) return // 空地不创建
            const isWall = cellType === 2
            const height = isWall ? tileHeight + baseHeight : baseHeight
            const geometry = new THREE.BoxGeometry(tileSize, height, tileSize)
            const material = new THREE.MeshStandardMaterial({
                color: isWall ? 0xff0000 : 0x00ff00
            })
            const mesh = new THREE.Mesh(geometry, material)
            mesh.position.set(j * tileSize, height / 2, i * tileSize)
            scene.add(mesh)
        })
    })

    // 添加光源
    const dirLight = new THREE.DirectionalLight(0xffffff, 1)
    dirLight.position.set(10, 200, 10)
    dirLight.castShadow = true
    scene.add(dirLight)

    // 创建渲染器
    renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true
    })
    // renderer.setPixelRatio(window.devicePixelRatio);
    renderer.setSize(game.value.clientWidth, game.value.clientHeight)
    // renderer.toneMapping = THREE.ACESFilmicToneMapping;
    game.value.appendChild(renderer.domElement)

    // Sky
    const sky = new Sky();
    sky.scale.setScalar(10000);
    scene.add(sky);

    const uniforms = sky.material.uniforms;
    uniforms['turbidity']!.value = 0;
    uniforms['rayleigh']!.value = 3;
    uniforms['mieDirectionalG']!.value = 0.7;
    uniforms['cloudElevation']!.value = 1;
    uniforms['sunPosition']!.value.set(- 0.8, 0.19, 0.56); // elevation: 11, azimuth: -55
    const pmremGenerator = new THREE.PMREMGenerator(renderer);
    // ----- 生成环境贴图（使用临时场景包含 sky） -----
    const environment = pmremGenerator.fromScene(sky as any).texture
    scene.environment = environment
    // 可选：释放临时场景（pmremGenerator 会保留纹理）

    // 首次渲染
    renderer.render(scene, camera)

    // 监听键盘事件
    window.addEventListener('keydown', onKeyDown)

    // 窗口尺寸自适应
    const handleResize = () => {
        if (!game.value) return
        const width = game.value.clientWidth
        const height = game.value.clientHeight
        renderer.setSize(width, height)
        camera.aspect = width / height
        camera.updateProjectionMatrix()
        renderer.render(scene, camera)
    }
    window.addEventListener('resize', handleResize)

    // 在组件卸载时清理
    onBeforeUnmount(() => {
        window.removeEventListener('keydown', onKeyDown)
        window.removeEventListener('resize', handleResize)
        // 断开 Timer 连接
        timer?.disconnect()
        renderer.dispose()
        if (game.value) {
            game.value.removeChild(renderer.domElement)
        }
    })
})
</script>

<template>
    <div class="game" ref="game"></div>
</template>

<style scoped>
.game {
    width: 100vw;
    height: 100vh;
    overflow: hidden;
}
</style>