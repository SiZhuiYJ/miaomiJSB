<script setup lang="ts">
import { ref, watch } from 'vue'
import Cropper from './Cropper.vue'

const imgSrc = ref<string>('')
const cropped = ref<string>('')
const croppedWidth = ref<number | null>(null)
const croppedHeight = ref<number | null>(null)

const onFileChange = (e: Event) => {
    const files = (e.target as HTMLInputElement).files
    if (!files || !files[0]) return
    const file = files[0]
    const reader = new FileReader()
    reader.onload = () => {
        imgSrc.value = reader.result as string
        cropped.value = ''
    }
    reader.readAsDataURL(file)
}

const onCropped = (data: string) => {
    cropped.value = data
}

watch(cropped, (v) => {
    croppedWidth.value = null
    croppedHeight.value = null
    if (!v) return
    const img = new Image()
    img.onload = () => {
        croppedWidth.value = img.width
        croppedHeight.value = img.height
    }
    img.src = v
})
</script>

<template>
    <div class="uploader">
        <div style="margin-bottom:12px;">
            <input type="file" accept="image/*" @change="onFileChange" />
        </div>

        <div v-if="imgSrc" style="border:1px solid #eaeaea; padding:10px;">
            <div style="margin-bottom:8px;font-weight:600">裁剪区域</div>
            <Cropper :img="imgSrc" @crop="onCropped" />
        </div>

        <div v-if="cropped" style="margin-top:16px;">
            <div style="margin-bottom:8px;font-weight:600">裁剪结果预览</div>
            <img :src="cropped" alt="cropped" style="max-width:320px; border:1px solid #ddd;" />
            <div style="margin-top:8px">
                尺寸: <span v-if="croppedWidth !== null">{{ croppedWidth }}x{{ croppedHeight }}</span><span
                    v-else>已生成</span>
            </div>
        </div>
    </div>
</template>

<style scoped>
.uploader {
    display: flex;
    flex-direction: column
}
</style>
