<script setup lang="ts">
import 'vue-cropper/dist/index.css'
import { ref, reactive, watch } from 'vue'
import { VueCropper } from 'vue-cropper'

const props = defineProps<{ img: string }>()
const emit = defineEmits<{
    (e: 'crop', data: string): void
}>()

const cropper = ref<any>(null)

const option = reactive<any>({
    img: props.img || '',
    autoCrop: true,
    // 固定裁剪框为 1:1
    fixed: true,
    // 限制裁剪框在图片内
    full: true,
    infoTrue: false,
    centerBox: true,
    outputSize: 1,
    // 使用 png 无损输出以保证最高画质
    outputType: 'png',
    // 一些版本支持 fixedNumber/fixed-number 用于指定比例
    fixedNumber: [1, 1],
})

watch(() => props.img, (v) => {
    option.img = v || ''
})

const doCrop = () => {
    if (!cropper.value) return
    cropper.value.getCropData((data: string) => {
        emit('crop', data)
    })
}
</script>

<template>
    <div class="cropper-wrapper">
        <vue-cropper ref="cropper" :img="option.img" :auto-crop="option.autoCrop" :fixed="option.fixed"
            :full="option.full" :info-true="option.infoTrue" :center-box="option.centerBox"
            :output-size="option.outputSize" :output-type="option.outputType" :fixed-number="option.fixedNumber">
        </vue-cropper>

        <div style="margin-top:10px; text-align:center;">
            <button type="button" @click="doCrop">截取并返回图片</button>
        </div>
    </div>
</template>

<style scoped>
.cropper-wrapper {
    box-shadow: 0 3px 5px 1px #cfcfcf;
    border: 10px solid white;
    width: 400px;
    height: 400px;

}

button {
    padding: 6px 12px;
    border-radius: 4px;
    border: 1px solid #ccc;
    background: #fff;
    cursor: pointer;
}

button:hover {
    background: #f5f5f5
}
</style>
