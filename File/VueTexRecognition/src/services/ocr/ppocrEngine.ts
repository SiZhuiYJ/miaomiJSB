import * as ort from 'onnxruntime-web'

import type { OcrBox, OcrProgress, OcrRunResult, OcrTextBlock } from '@/types/ocr'

const MODEL_ROOT = '/ocr'
const DET_MODEL_URL = `${MODEL_ROOT}/models/PP-OCRv6_det_tiny.onnx`
const REC_MODEL_URL = `${MODEL_ROOT}/models/PP-OCRv6_rec_tiny.onnx`
const DICT_URL = `${MODEL_ROOT}/ppocr_keys_v6_tiny.json`
const ORT_CDN = 'https://cdn.jsdelivr.net/npm/onnxruntime-web@1.27.0/dist/'

const DET_MAX_SIDE = 960
const DET_MEAN = [0.485, 0.456, 0.406] as const
const DET_STD = [0.229, 0.224, 0.225] as const
const REC_MEAN = [0.5, 0.5, 0.5] as const
const REC_STD = [0.5, 0.5, 0.5] as const
const REC_HEIGHT = 48

type RgbTuple = readonly [number, number, number]

interface DecodeResult {
  text: string
  confidence: number
  charCount: number
}

interface RecognizeOptions {
  onProgress?: (progress: OcrProgress) => void
}

function now() {
  return performance.now()
}

function get2d(canvas: HTMLCanvasElement): CanvasRenderingContext2D {
  const ctx = canvas.getContext('2d', { willReadFrequently: true })
  if (!ctx) throw new Error('Canvas 2D context is unavailable')
  return ctx
}

function imageDataToCanvas(imageData: ImageData): HTMLCanvasElement {
  const canvas = document.createElement('canvas')
  canvas.width = imageData.width
  canvas.height = imageData.height
  get2d(canvas).putImageData(imageData, 0, 0)
  return canvas
}

function resizeImageData(source: CanvasImageSource, targetW: number, targetH: number): ImageData {
  const canvas = document.createElement('canvas')
  canvas.width = targetW
  canvas.height = targetH
  const ctx = get2d(canvas)
  ctx.drawImage(source, 0, 0, targetW, targetH)
  return ctx.getImageData(0, 0, targetW, targetH)
}

function rgbaToCHW(imageData: ImageData, mean: RgbTuple, std: RgbTuple): Float32Array {
  const { data, width, height } = imageData
  const chw = new Float32Array(3 * height * width)

  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const sourceIndex = (y * width + x) * 4
      const targetIndex = y * width + x

      for (const c of [0, 1, 2] as const) {
        chw[c * height * width + targetIndex] =
          (((data[sourceIndex + c] ?? 0) / 255) - mean[c]) / std[c]
      }
    }
  }

  return chw
}

function cropImageData(imageData: ImageData, x: number, y: number, w: number, h: number): ImageData | null {
  const cropX = Math.max(0, Math.floor(x))
  const cropY = Math.max(0, Math.floor(y))
  const cropW = Math.min(Math.floor(w), imageData.width - cropX)
  const cropH = Math.min(Math.floor(h), imageData.height - cropY)

  if (cropW <= 0 || cropH <= 0) return null

  const cropped = new ImageData(cropW, cropH)
  for (let row = 0; row < cropH; row += 1) {
    for (let col = 0; col < cropW; col += 1) {
      const sourceIndex = ((cropY + row) * imageData.width + (cropX + col)) * 4
      const targetIndex = (row * cropW + col) * 4

      cropped.data[targetIndex] = imageData.data[sourceIndex] ?? 0
      cropped.data[targetIndex + 1] = imageData.data[sourceIndex + 1] ?? 0
      cropped.data[targetIndex + 2] = imageData.data[sourceIndex + 2] ?? 0
      cropped.data[targetIndex + 3] = imageData.data[sourceIndex + 3] ?? 255
    }
  }

  return cropped
}

function dbBoxes(probData: Float32Array, outputW: number, outputH: number, scaleX: number, scaleY: number): OcrBox[] {
  const thresh = 0.2
  const boxThresh = 0.4
  const unclip = 1.4
  const pixelCount = outputW * outputH
  const bin = new Uint8Array(pixelCount)

  for (let i = 0; i < pixelCount; i += 1) {
    bin[i] = (probData[i] ?? 0) > thresh ? 1 : 0
  }

  const label = new Int32Array(pixelCount)
  let curLabel = 0
  const boxes: OcrBox[] = []

  for (let start = 0; start < pixelCount; start += 1) {
    if (bin[start] !== 1 || label[start] !== 0) continue

    curLabel += 1
    const stack = [start]
    label[start] = curLabel

    let minX = outputW
    let minY = outputH
    let maxX = 0
    let maxY = 0
    let sum = 0
    let count = 0

    while (stack.length > 0) {
      const point = stack.pop()
      if (point === undefined) break

      const px = point % outputW
      const py = Math.floor(point / outputW)

      if (px < minX) minX = px
      if (px > maxX) maxX = px
      if (py < minY) minY = py
      if (py > maxY) maxY = py

      sum += probData[point] ?? 0
      count += 1

      if (px > 0 && bin[point - 1] && !label[point - 1]) {
        label[point - 1] = curLabel
        stack.push(point - 1)
      }
      if (px < outputW - 1 && bin[point + 1] && !label[point + 1]) {
        label[point + 1] = curLabel
        stack.push(point + 1)
      }
      if (py > 0 && bin[point - outputW] && !label[point - outputW]) {
        label[point - outputW] = curLabel
        stack.push(point - outputW)
      }
      if (py < outputH - 1 && bin[point + outputW] && !label[point + outputW]) {
        label[point + outputW] = curLabel
        stack.push(point + outputW)
      }
    }

    const boxW = maxX - minX + 1
    const boxH = maxY - minY + 1
    if (Math.min(boxW, boxH) < 3 || count === 0) continue
    if (sum / count < boxThresh) continue

    const area = boxW * boxH
    const perimeter = 2 * (boxW + boxH)
    const distance = (area * unclip) / perimeter

    boxes.push({
      x0: Math.max(0, minX - distance) * scaleX,
      y0: Math.max(0, minY - distance) * scaleY,
      x1: Math.min(outputW, maxX + distance) * scaleX,
      y1: Math.min(outputH, maxY + distance) * scaleY,
    })
  }

  boxes.sort((a, b) => {
    if (Math.abs(a.y0 - b.y0) < 12) return a.x0 - b.x0
    return a.y0 - b.y0
  })

  return boxes
}

function ctcDecode(data: Float32Array, timeSteps: number, classes: number, charList: string[]): DecodeResult {
  let text = ''
  let charCount = 0
  let confidenceSum = 0
  let prev = -1

  for (let t = 0; t < timeSteps; t += 1) {
    const base = t * classes
    let maxValue = -1e9
    let index = 0

    for (let c = 0; c < classes; c += 1) {
      const value = data[base + c] ?? Number.NEGATIVE_INFINITY
      if (!Number.isFinite(value)) continue
      if (value > maxValue) {
        maxValue = value
        index = c
      }
    }

    if (maxValue === -1e9) continue

    if (index !== 0 && index !== prev) {
      let sumExp = 0
      for (let c = 0; c < classes; c += 1) {
        const value = data[base + c] ?? Number.NEGATIVE_INFINITY
        const diff = value - maxValue
        if (diff < -50 || !Number.isFinite(diff)) continue
        sumExp += Math.exp(diff)
      }

      const confidence = sumExp > 0 ? 1 / sumExp : 0.001
      text += charList[index] ?? '\uFFFD'
      confidenceSum += Math.max(0.001, Math.min(confidence, 0.999))
      charCount += 1
    }

    prev = index
  }

  return {
    text,
    charCount,
    confidence: charCount > 0 ? confidenceSum / charCount : 0,
  }
}

function getOutputTensor(outputs: ort.InferenceSession.ReturnType, outputName: string): ort.Tensor {
  const tensor = outputs[outputName]
  if (!tensor) throw new Error(`ONNX output "${outputName}" was not returned`)
  return tensor
}

export class PpocrEngine {
  private detSession: ort.InferenceSession | null = null
  private recSession: ort.InferenceSession | null = null
  private charList: string[] = []
  private loadingPromise: Promise<void> | null = null
  private loadMs = 0

  async load(onProgress?: (progress: OcrProgress) => void): Promise<void> {
    if (this.detSession && this.recSession && this.charList.length > 0) return
    if (this.loadingPromise) return this.loadingPromise

    this.loadingPromise = this.doLoad(onProgress)
    return this.loadingPromise
  }

  async recognize(imageData: ImageData, options: RecognizeOptions = {}): Promise<OcrRunResult> {
    await this.load(options.onProgress)

    if (!this.detSession || !this.recSession) throw new Error('OCR sessions are not loaded')

    const totalStart = now()
    const sourceCanvas = imageDataToCanvas(imageData)
    const originalW = imageData.width
    const originalH = imageData.height

    options.onProgress?.({ phase: 'detecting', message: '检测文本区域' })
    const detectStart = now()
    const resizeRatio = Math.min(1, DET_MAX_SIDE / Math.max(originalW, originalH))
    const detW = Math.max(32, Math.round((originalW * resizeRatio) / 32) * 32)
    const detH = Math.max(32, Math.round((originalH * resizeRatio) / 32) * 32)
    const detImage = resizeImageData(sourceCanvas, detW, detH)
    const detInput = rgbaToCHW(detImage, DET_MEAN, DET_STD)
    const detTensor = new ort.Tensor('float32', detInput, [1, 3, detH, detW])
    const detInputName = this.detSession.inputNames[0] ?? 'x'
    const detOutputName = this.detSession.outputNames[0]
    if (!detOutputName) throw new Error('Detection model has no output')

    const detOutputMap = await this.detSession.run({ [detInputName]: detTensor })
    const detOutput = getOutputTensor(detOutputMap, detOutputName)
    const probData = detOutput.data as Float32Array
    const probH = Number(detOutput.dims[2])
    const probW = Number(detOutput.dims[3])
    if (!Number.isFinite(probH) || !Number.isFinite(probW) || probH <= 0 || probW <= 0) {
      throw new Error(`Unexpected detection output shape: ${detOutput.dims.join('x')}`)
    }

    const boxes = dbBoxes(probData, probW, probH, originalW / probW, originalH / probH)
    const detectMs = now() - detectStart

    const recognizeStart = now()
    const blocks: OcrTextBlock[] = []
    const recInputName = this.recSession.inputNames[0] ?? 'x'
    const recOutputName = this.recSession.outputNames[0]
    if (!recOutputName) throw new Error('Recognition model has no output')

    let blockId = 1
    for (const [boxIndex, box] of boxes.entries()) {
      options.onProgress?.({
        phase: 'recognizing',
        message: `识别文本块 ${boxIndex + 1}/${boxes.length}`,
        current: boxIndex + 1,
        total: boxes.length,
      })

      const cropW = box.x1 - box.x0
      const cropH = box.y1 - box.y0
      if (cropW < 2 || cropH < 2) continue

      const cropped = cropImageData(imageData, box.x0, box.y0, cropW, cropH)
      if (!cropped) continue

      const recW = Math.max(8, Math.round((REC_HEIGHT * cropW) / cropH))
      const finalRecW = Math.min(recW, 2400)
      const cropCanvas = imageDataToCanvas(cropped)
      const recImage = resizeImageData(cropCanvas, finalRecW, REC_HEIGHT)
      const recInput = rgbaToCHW(recImage, REC_MEAN, REC_STD)
      const recTensor = new ort.Tensor('float32', recInput, [1, 3, REC_HEIGHT, finalRecW])

      const recOutputMap = await this.recSession.run({ [recInputName]: recTensor })
      const recOutput = getOutputTensor(recOutputMap, recOutputName)
      const timeSteps = Number(recOutput.dims[1])
      const classes = Number(recOutput.dims[2])
      if (!Number.isFinite(timeSteps) || !Number.isFinite(classes) || classes <= 0) {
        throw new Error(`Unexpected recognition output shape: ${recOutput.dims.join('x')}`)
      }
      if (classes > this.charList.length) {
        throw new Error(`Character list length ${this.charList.length} does not cover model output ${classes}`)
      }

      const decoded = ctcDecode(recOutput.data as Float32Array, timeSteps, classes, this.charList)
      const text = decoded.text.trim()
      if (!text) continue

      blocks.push({
        id: blockId,
        box,
        text,
        confidence: decoded.confidence,
        charCount: decoded.charCount,
      })
      blockId += 1
    }

    const recognizeMs = now() - recognizeStart
    const totalMs = now() - totalStart
    options.onProgress?.({ phase: 'done', message: '识别完成' })

    return {
      blocks,
      boxes,
      timings: {
        loadMs: this.loadMs,
        detectMs,
        recognizeMs,
        totalMs,
      },
    }
  }

  private async doLoad(onProgress?: (progress: OcrProgress) => void): Promise<void> {
    const loadStart = now()
    onProgress?.({ phase: 'loading', message: '加载字符集' })

    ort.env.wasm.wasmPaths = ORT_CDN
    ort.env.wasm.numThreads = 1

    const dictResponse = await fetch(DICT_URL)
    if (!dictResponse.ok) throw new Error(`Failed to load character dictionary: ${dictResponse.status}`)
    const dict = (await dictResponse.json()) as string[]
    this.charList = ['', ...dict, ' ']

    onProgress?.({ phase: 'loading', message: '加载检测模型' })
    this.detSession = await ort.InferenceSession.create(DET_MODEL_URL, {
      executionProviders: ['wasm'],
    })

    onProgress?.({ phase: 'loading', message: '加载识别模型' })
    this.recSession = await ort.InferenceSession.create(REC_MODEL_URL, {
      executionProviders: ['wasm'],
    })

    this.loadMs = now() - loadStart
  }
}

export const ppocrEngine = new PpocrEngine()
