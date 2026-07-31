<script setup lang="ts">
import { computed, ref } from 'vue'
import { Copy, FileText } from 'lucide-vue-next'

import type { OcrRunResult, OcrTextBlock } from '@/types/ocr'

const props = defineProps<{
  blocks: OcrTextBlock[]
  result: OcrRunResult | null
  errorMessage: string
}>()

const copied = ref(false)

const fullText = computed(() => props.blocks.map((block) => block.text).join('\n'))
const averageConfidence = computed(() => {
  if (props.blocks.length === 0) return 0
  return props.blocks.reduce((sum, block) => sum + block.confidence, 0) / props.blocks.length
})

async function copyText() {
  if (!fullText.value) return
  await navigator.clipboard.writeText(fullText.value)
  copied.value = true
  window.setTimeout(() => {
    copied.value = false
  }, 1400)
}
</script>

<template>
  <aside class="results-panel" aria-label="识别结果">
    <div class="panel-header">
      <div>
        <h2>识别结果</h2>
        <p>{{ blocks.length }} 个文本块</p>
      </div>
      <button type="button" class="icon-button" :disabled="!fullText" title="复制文本" @click="copyText">
        <Copy :size="17" aria-hidden="true" />
      </button>
    </div>

    <div v-if="result" class="metrics-grid">
      <div>
        <span>平均置信度</span>
        <strong>{{ (averageConfidence * 100).toFixed(1) }}%</strong>
      </div>
      <div>
        <span>检测</span>
        <strong>{{ result.timings.detectMs.toFixed(0) }} ms</strong>
      </div>
      <div>
        <span>识别</span>
        <strong>{{ result.timings.recognizeMs.toFixed(0) }} ms</strong>
      </div>
    </div>

    <div v-if="errorMessage" class="error-box">{{ errorMessage }}</div>
    <div v-if="copied" class="copy-note">已复制</div>

    <div v-if="blocks.length" class="result-list">
      <article v-for="block in blocks" :key="block.id" class="result-item">
        <div class="result-index">#{{ block.id }}</div>
        <div class="result-content">
          <div class="result-text">{{ block.text }}</div>
          <div class="result-meta">{{ (block.confidence * 100).toFixed(2) }}% · {{ block.charCount }} 字符</div>
        </div>
      </article>
    </div>

    <div v-else class="empty-results">
      <FileText :size="26" aria-hidden="true" />
      <span>暂无文本</span>
    </div>
  </aside>
</template>
