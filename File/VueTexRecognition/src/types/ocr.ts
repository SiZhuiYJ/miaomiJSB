export interface OcrBox {
  x0: number
  y0: number
  x1: number
  y1: number
}

export interface OcrProgress {
  phase: 'loading' | 'detecting' | 'recognizing' | 'done'
  message: string
  current?: number
  total?: number
}

export interface OcrTextBlock {
  id: number
  box: OcrBox
  text: string
  confidence: number
  charCount: number
}

export interface OcrTimings {
  loadMs: number
  detectMs: number
  recognizeMs: number
  totalMs: number
}

export interface OcrRunResult {
  blocks: OcrTextBlock[]
  boxes: OcrBox[]
  timings: OcrTimings
}

export interface LoadedImagePayload {
  imageData: ImageData
  width: number
  height: number
}
