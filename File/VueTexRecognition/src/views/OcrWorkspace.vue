<script setup lang="ts">
import { onMounted } from 'vue'

import OcrCanvas from '@/components/OcrCanvas.vue'
import OcrResults from '@/components/OcrResults.vue'
import OcrToolbar from '@/components/OcrToolbar.vue'
import { useOcrWorkspace } from '@/composables/useOcrWorkspace'
import type { LoadedImagePayload } from '@/types/ocr'

const workspace = useOcrWorkspace()

onMounted(async () => {
  await workspace.loadModels()
  if (workspace.canRun.value) await workspace.run()
})

async function handleImageLoaded(payload: LoadedImagePayload) {
  await workspace.setLoadedImage(payload)
  if (workspace.canRun.value) await workspace.run()
}
</script>

<template>
  <main class="workspace">
    <header class="app-topbar">
      <div>
        <h1>PP-OCRv6 文本识别</h1>
        <p>浏览器本地推理</p>
      </div>
      <div class="runtime-pill">WASM</div>
    </header>

    <OcrToolbar
      :status-message="workspace.statusMessage.value"
      :selected-file-name="workspace.selectedFileName.value"
      :can-run="workspace.canRun.value"
      :is-busy="workspace.isBusy.value"
      :progress="workspace.progress.value"
      :image-size="workspace.imageSize"
      @select-file="workspace.selectFile"
      @run="workspace.run"
      @clear="workspace.clear"
    />

    <section class="workspace-grid">
      <OcrCanvas
        :image-url="workspace.imageUrl.value"
        :boxes="workspace.boxes.value"
        :blocks="workspace.blocks.value"
        :is-busy="workspace.isBusy.value"
        @image-loaded="handleImageLoaded"
        @rerun="workspace.run"
      />

      <OcrResults
        :blocks="workspace.blocks.value"
        :result="workspace.result.value"
        :error-message="workspace.errorMessage.value"
      />
    </section>
  </main>
</template>
