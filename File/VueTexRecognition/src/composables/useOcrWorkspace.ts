import { computed, onBeforeUnmount, reactive, ref } from 'vue'

import { ppocrEngine } from '@/services/ocr/ppocrEngine'
import type { LoadedImagePayload, OcrProgress, OcrRunResult, OcrTextBlock } from '@/types/ocr'

type WorkspaceStatus = 'idle' | 'loading-models' | 'image-ready' | 'running' | 'done' | 'error'

export function useOcrWorkspace() {
  const status = ref<WorkspaceStatus>('loading-models')
  const statusMessage = ref('模型加载中')
  const selectedFileName = ref('')
  const imageUrl = ref('')
  const imageData = ref<ImageData | null>(null)
  const imageSize = reactive({ width: 0, height: 0 })
  const result = ref<OcrRunResult | null>(null)
  const progress = ref<OcrProgress | null>(null)
  const errorMessage = ref('')

  const blocks = computed<OcrTextBlock[]>(() => result.value?.blocks ?? [])
  const boxes = computed(() => result.value?.boxes ?? [])
  const canRun = computed(() => Boolean(imageData.value) && status.value !== 'running' && status.value !== 'loading-models')
  const isBusy = computed(() => status.value === 'loading-models' || status.value === 'running')

  function setProgress(next: OcrProgress) {
    progress.value = next
    statusMessage.value = next.message
  }

  async function loadModels() {
    status.value = 'loading-models'
    errorMessage.value = ''

    try {
      await ppocrEngine.load(setProgress)
      status.value = imageData.value ? 'image-ready' : 'idle'
      statusMessage.value = imageData.value ? '图片已加载，可以开始识别' : '模型已就绪'
    } catch (err) {
      status.value = 'error'
      errorMessage.value = err instanceof Error ? err.message : String(err)
      statusMessage.value = '模型加载失败'
    }
  }

  function selectFile(file: File) {
    if (imageUrl.value) URL.revokeObjectURL(imageUrl.value)

    selectedFileName.value = file.name
    imageUrl.value = URL.createObjectURL(file)
    imageData.value = null
    result.value = null
    errorMessage.value = ''
    status.value = status.value === 'loading-models' ? 'loading-models' : 'idle'
    statusMessage.value = status.value === 'loading-models' ? '等待模型加载完成' : '图片加载中'
  }

  async function setLoadedImage(payload: LoadedImagePayload) {
    imageData.value = payload.imageData
    imageSize.width = payload.width
    imageSize.height = payload.height
    result.value = null

    if (status.value === 'loading-models') {
      statusMessage.value = '图片已加载，模型完成后可识别'
      return
    }

    status.value = 'image-ready'
    statusMessage.value = '图片已加载，可以开始识别'
  }

  async function run() {
    if (!imageData.value || status.value === 'running' || status.value === 'loading-models') return

    status.value = 'running'
    statusMessage.value = '识别中'
    errorMessage.value = ''
    result.value = null

    try {
      result.value = await ppocrEngine.recognize(imageData.value, { onProgress: setProgress })
      status.value = 'done'
      statusMessage.value =
        result.value.blocks.length > 0 ? `识别完成，${result.value.blocks.length} 个文本块` : '未检测到有效文本'
    } catch (err) {
      status.value = 'error'
      errorMessage.value = err instanceof Error ? err.message : String(err)
      statusMessage.value = '识别失败'
    }
  }

  function clear() {
    if (imageUrl.value) URL.revokeObjectURL(imageUrl.value)

    imageUrl.value = ''
    selectedFileName.value = ''
    imageData.value = null
    result.value = null
    errorMessage.value = ''
    progress.value = null
    imageSize.width = 0
    imageSize.height = 0
    status.value = 'idle'
    statusMessage.value = '模型已就绪'
  }

  onBeforeUnmount(() => {
    if (imageUrl.value) URL.revokeObjectURL(imageUrl.value)
  })

  return {
    status,
    statusMessage,
    selectedFileName,
    imageUrl,
    imageSize,
    result,
    blocks,
    boxes,
    progress,
    errorMessage,
    canRun,
    isBusy,
    loadModels,
    selectFile,
    setLoadedImage,
    run,
    clear,
  }
}
