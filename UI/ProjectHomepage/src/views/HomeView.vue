<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'

type TokenType = 'number' | 'identifier' | 'operator' | 'lparen' | 'rparen' | 'comma'

interface Token {
  type: TokenType
  value: string
}

interface HoverPoint {
  x: number
  y: number
  canvasX: number
  canvasY: number
}

type PlotFunction = (x: number) => number
type ColorMode = 'solid' | 'canvas-gradient' | 'line-gradient'
type GradientDirection =
  | 'left-right'
  | 'right-left'
  | 'top-bottom'
  | 'bottom-top'
  | 'diagonal-down'
  | 'diagonal-up'

interface CurvePoint {
  canvasX: number
  canvasY: number
}

interface DragState {
  pointerId: number
  canvasX: number
  canvasY: number
  xMin: number
  xMax: number
  yMin: number
  yMax: number
}

const expression = ref('sin(x)')
const xMin = ref(-10)
const xMax = ref(10)
const yMin = ref(-6)
const yMax = ref(6)
const errorMessage = ref('')
const hoverPoint = ref<HoverPoint | null>(null)
const dragState = ref<DragState | null>(null)
const canvasRef = ref<HTMLCanvasElement | null>(null)
const chartShellRef = ref<HTMLElement | null>(null)
const route = useRoute()

const examples = ['sin(x)', 'x^2', '0.2x^3 - x', 'sqrt(25 - x^2)', 'cbrt(x)', 'cos(2x) + sin(x)']
const colorMode = ref<ColorMode>('solid')
const solidColor = ref('#0f766e')
const gradientStartColor = ref('#0f766e')
const gradientEndColor = ref('#f97316')
const gradientDirection = ref<GradientDirection>('left-right')
const isColorMenuOpen = ref(false)
const colorModes: Array<{ label: string; value: ColorMode }> = [
  { label: '单色', value: 'solid' },
  { label: '整体渐变', value: 'canvas-gradient' },
  { label: '线条渐变', value: 'line-gradient' },
]
const gradientDirections: Array<{ label: string; value: GradientDirection }> = [
  { label: '左到右', value: 'left-right' },
  { label: '右到左', value: 'right-left' },
  { label: '上到下', value: 'top-bottom' },
  { label: '下到上', value: 'bottom-top' },
  { label: '左上到右下', value: 'diagonal-down' },
  { label: '左下到右上', value: 'diagonal-up' },
]

const mathScope = {
  abs: Math.abs,
  acos: Math.acos,
  asin: Math.asin,
  atan: Math.atan,
  ceil: Math.ceil,
  cos: Math.cos,
  cbrt: Math.cbrt,
  e: Math.E,
  exp: Math.exp,
  floor: Math.floor,
  lg: Math.log10,
  ln: Math.log,
  log: Math.log,
  max: Math.max,
  min: Math.min,
  pi: Math.PI,
  pow: Math.pow,
  round: Math.round,
  sign: Math.sign,
  sin: Math.sin,
  sqrt: Math.sqrt,
  tan: Math.tan,
}

const scopeKeys = Object.keys(mathScope) as Array<keyof typeof mathScope>
const functionNames = new Set(
  scopeKeys.filter((key) => typeof mathScope[key] === 'function').map((key) => key.toString()),
)
const allowedIdentifiers = new Set(['x', ...scopeKeys.map((key) => key.toString())])

let resizeObserver: ResizeObserver | null = null

function selectExample(example: string) {
  expression.value = example
}

function normalizeRangeValue(value: number): number {
  return Number(value.toFixed(8))
}

function setViewport(nextXMin: number, nextXMax: number, nextYMin: number, nextYMax: number) {
  xMin.value = normalizeRangeValue(nextXMin)
  xMax.value = normalizeRangeValue(nextXMax)
  yMin.value = normalizeRangeValue(nextYMin)
  yMax.value = normalizeRangeValue(nextYMax)
}

function resetView() {
  const canvasState = getCanvasRect()
  const xSpan = 20
  const ySpan = canvasState ? xSpan * (canvasState.rect.height / Math.max(canvasState.rect.width, 1)) : 12

  setViewport(-xSpan / 2, xSpan / 2, -ySpan / 2, ySpan / 2)
}

function setColorMode(mode: ColorMode) {
  colorMode.value = mode
}

function toggleColorMenu() {
  isColorMenuOpen.value = !isColorMenuOpen.value
}

function swapGradientColors() {
  const nextStartColor = gradientEndColor.value

  gradientEndColor.value = gradientStartColor.value
  gradientStartColor.value = nextStartColor
}

function applyFormulaFromRoute(value: unknown) {
  if (typeof value === 'string' && value.trim()) {
    expression.value = value
  }
}

function tokenize(input: string): Token[] {
  const tokens: Token[] = []
  let index = 0

  while (index < input.length) {
    const char = input[index]

    if (char === undefined) {
      break
    }

    if (/\d|\./.test(char)) {
      const start = index
      index += 1

      while (index < input.length && /[\d.]/.test(input[index] ?? '')) {
        index += 1
      }

      if (/e/i.test(input[index] ?? '') && /\d/.test(input[index + 1] ?? '')) {
        index += 2

        while (index < input.length && /\d/.test(input[index] ?? '')) {
          index += 1
        }
      } else if (
        /e/i.test(input[index] ?? '') &&
        /[+-]/.test(input[index + 1] ?? '') &&
        /\d/.test(input[index + 2] ?? '')
      ) {
        index += 3

        while (index < input.length && /\d/.test(input[index] ?? '')) {
          index += 1
        }
      }

      const value = input.slice(start, index)

      if (!Number.isFinite(Number(value))) {
        throw new Error(`数字格式无效：${value}`)
      }

      tokens.push({ type: 'number', value })
      continue
    }

    if (/[a-zA-Z_]/.test(char)) {
      const start = index
      index += 1

      while (index < input.length && /[a-zA-Z0-9_]/.test(input[index] ?? '')) {
        index += 1
      }

      const value = input.slice(start, index).toLowerCase()

      if (!allowedIdentifiers.has(value)) {
        throw new Error(`不支持的符号：${value}`)
      }

      tokens.push({ type: 'identifier', value })
      continue
    }

    if ('+-*/^%'.includes(char)) {
      tokens.push({ type: 'operator', value: char === '^' ? '**' : char })
      index += 1
      continue
    }

    if (char === '(') {
      tokens.push({ type: 'lparen', value: char })
      index += 1
      continue
    }

    if (char === ')') {
      tokens.push({ type: 'rparen', value: char })
      index += 1
      continue
    }

    if (char === ',') {
      tokens.push({ type: 'comma', value: char })
      index += 1
      continue
    }

    throw new Error(`无法识别的字符：${char}`)
  }

  return tokens
}

function normalizeExpression(rawExpression: string): string {
  const normalized = rawExpression
    .trim()
    .replace(/^y\s*=/i, '')
    .replace(/[（]/g, '(')
    .replace(/[）]/g, ')')
    .replace(/[，]/g, ',')
    .replace(/π/g, 'pi')
    .replace(/√/g, 'sqrt')
    .replace(/\s+/g, '')

  if (!normalized) {
    throw new Error('请输入函数表达式')
  }

  return normalized
}

function isFunctionCall(tokens: Token[], index: number): boolean {
  const token = tokens[index]
  const nextToken = tokens[index + 1]

  return token?.type === 'identifier' && functionNames.has(token.value) && nextToken?.type === 'lparen'
}

function canEndValue(tokens: Token[], index: number): boolean {
  const token = tokens[index]

  if (!token) {
    return false
  }

  if (token.type === 'number' || token.type === 'rparen') {
    return true
  }

  return token.type === 'identifier' && !isFunctionCall(tokens, index)
}

function canStartValue(token: Token | undefined): boolean {
  return token?.type === 'number' || token?.type === 'identifier' || token?.type === 'lparen'
}

function tokensToJavaScript(tokens: Token[]): string {
  return tokens
    .map((token, index) => {
      const nextToken = tokens[index + 1]
      const shouldInsertMultiply = canEndValue(tokens, index) && canStartValue(nextToken)

      return `${token.value}${shouldInsertMultiply ? '*' : ''}`
    })
    .join('')
}

function compileExpression(rawExpression: string): PlotFunction {
  const normalized = normalizeExpression(rawExpression)
  const tokens = tokenize(normalized)
  const body = tokensToJavaScript(tokens)
  const scopeValues = scopeKeys.map((key) => mathScope[key])
  const evaluator = new Function(
    'x',
    ...scopeKeys,
    `"use strict"; return (${body});`,
  ) as (x: number, ...scopeValues: Array<(typeof mathScope)[keyof typeof mathScope]>) => unknown

  return (x: number) => {
    const value = evaluator(x, ...scopeValues)

    return typeof value === 'number' && Number.isFinite(value) ? value : Number.NaN
  }
}

function formatNumber(value: number): string {
  if (Math.abs(value) >= 1000 || (Math.abs(value) < 0.001 && value !== 0)) {
    return value.toExponential(2)
  }

  return Number(value.toFixed(3)).toString()
}

function niceStep(span: number, pixels: number): number {
  const targetGridSize = 72
  const roughStep = span / Math.max(pixels / targetGridSize, 1)
  const magnitude = 10 ** Math.floor(Math.log10(roughStep))
  const multipliers = [1, 2, 5, 10]
  const multiplier = multipliers.find((item) => item * magnitude >= roughStep) ?? 10

  return multiplier * magnitude
}

function drawGrid(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  toCanvasX: (x: number) => number,
  toCanvasY: (y: number) => number,
) {
  const xSpan = xMax.value - xMin.value
  const ySpan = yMax.value - yMin.value
  const xStep = niceStep(xSpan, width)
  const yStep = niceStep(ySpan, height)
  const firstX = Math.ceil(xMin.value / xStep) * xStep
  const firstY = Math.ceil(yMin.value / yStep) * yStep

  context.fillStyle = '#f8fafc'
  context.fillRect(0, 0, width, height)

  context.lineWidth = 1
  context.strokeStyle = '#dbe4ee'
  context.beginPath()

  for (let x = firstX; x <= xMax.value + xStep / 2; x += xStep) {
    const canvasX = toCanvasX(x)
    context.moveTo(canvasX, 0)
    context.lineTo(canvasX, height)
  }

  for (let y = firstY; y <= yMax.value + yStep / 2; y += yStep) {
    const canvasY = toCanvasY(y)
    context.moveTo(0, canvasY)
    context.lineTo(width, canvasY)
  }

  context.stroke()

  context.strokeStyle = '#111827'
  context.lineWidth = 1.5
  context.beginPath()

  if (xMin.value <= 0 && xMax.value >= 0) {
    const canvasX = toCanvasX(0)
    context.moveTo(canvasX, 0)
    context.lineTo(canvasX, height)
  }

  if (yMin.value <= 0 && yMax.value >= 0) {
    const canvasY = toCanvasY(0)
    context.moveTo(0, canvasY)
    context.lineTo(width, canvasY)
  }

  context.stroke()

  context.fillStyle = '#475569'
  context.font = '12px Arial, sans-serif'
  context.textBaseline = 'top'

  for (let x = firstX; x <= xMax.value + xStep / 2; x += xStep) {
    const canvasX = toCanvasX(x)

    if (canvasX < 4 || canvasX > width - 24) {
      continue
    }

    const labelY = yMin.value <= 0 && yMax.value >= 0 ? Math.min(toCanvasY(0) + 6, height - 18) : height - 18
    context.fillText(formatNumber(x), canvasX + 4, labelY)
  }

  context.textBaseline = 'middle'

  for (let y = firstY; y <= yMax.value + yStep / 2; y += yStep) {
    const canvasY = toCanvasY(y)

    if (canvasY < 14 || canvasY > height - 8) {
      continue
    }

    const labelX = xMin.value <= 0 && xMax.value >= 0 ? Math.min(toCanvasX(0) + 6, width - 42) : 8
    context.fillText(formatNumber(y), labelX, canvasY)
  }

  context.fillStyle = '#111827'
  context.font = '600 13px Arial, sans-serif'
  context.textBaseline = 'top'
  context.fillText('x', width - 18, yMin.value <= 0 && yMax.value >= 0 ? toCanvasY(0) + 8 : height - 22)
  context.fillText('y', xMin.value <= 0 && xMax.value >= 0 ? toCanvasX(0) + 8 : 8, 8)
}

function gradientVector(width: number, height: number) {
  switch (gradientDirection.value) {
    case 'right-left':
      return { x0: width, y0: 0, x1: 0, y1: 0 }
    case 'top-bottom':
      return { x0: 0, y0: 0, x1: 0, y1: height }
    case 'bottom-top':
      return { x0: 0, y0: height, x1: 0, y1: 0 }
    case 'diagonal-down':
      return { x0: 0, y0: 0, x1: width, y1: height }
    case 'diagonal-up':
      return { x0: 0, y0: height, x1: width, y1: 0 }
    case 'left-right':
    default:
      return { x0: 0, y0: 0, x1: width, y1: 0 }
  }
}

function gradientPreviewBackground(): string {
  const angle = {
    'left-right': '90deg',
    'right-left': '270deg',
    'top-bottom': '180deg',
    'bottom-top': '0deg',
    'diagonal-down': '135deg',
    'diagonal-up': '45deg',
  }[gradientDirection.value]

  return `linear-gradient(${angle}, ${gradientStartColor.value}, ${gradientEndColor.value})`
}

function createOverallGradient(context: CanvasRenderingContext2D, width: number, height: number): CanvasGradient {
  const vector = gradientVector(width, height)
  const gradient = context.createLinearGradient(vector.x0, vector.y0, vector.x1, vector.y1)

  gradient.addColorStop(0, gradientStartColor.value)
  gradient.addColorStop(1, gradientEndColor.value)

  return gradient
}

function gradientRatioForPoint(point: CurvePoint, width: number, height: number): number {
  const vector = gradientVector(width, height)
  const deltaX = vector.x1 - vector.x0
  const deltaY = vector.y1 - vector.y0
  const denominator = deltaX * deltaX + deltaY * deltaY

  if (denominator === 0) {
    return 0
  }

  return ((point.canvasX - vector.x0) * deltaX + (point.canvasY - vector.y0) * deltaY) / denominator
}

function parseHexColor(color: string): [number, number, number] {
  const fallback: [number, number, number] = [15, 118, 110]
  const match = color.match(/^#([0-9a-f]{6})$/i)

  if (!match?.[1]) {
    return fallback
  }

  return [
    Number.parseInt(match[1].slice(0, 2), 16),
    Number.parseInt(match[1].slice(2, 4), 16),
    Number.parseInt(match[1].slice(4, 6), 16),
  ]
}

function interpolateCurveColor(ratio: number): string {
  const start = parseHexColor(gradientStartColor.value)
  const end = parseHexColor(gradientEndColor.value)
  const progress = Math.min(Math.max(ratio, 0), 1)
  const red = Math.round(start[0] + (end[0] - start[0]) * progress)
  const green = Math.round(start[1] + (end[1] - start[1]) * progress)
  const blue = Math.round(start[2] + (end[2] - start[2]) * progress)

  return `rgb(${red}, ${green}, ${blue})`
}

function drawLineGradientCurve(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  plotFunction: PlotFunction,
  toCanvasX: (x: number) => number,
  toCanvasY: (y: number) => number,
) {
  const xSpan = xMax.value - xMin.value
  let previousPoint: CurvePoint | null = null

  context.lineWidth = 2.75
  context.lineJoin = 'round'
  context.lineCap = 'round'

  for (let pixelX = 0; pixelX <= width; pixelX += 1) {
    const x = xMin.value + (pixelX / width) * xSpan
    const y = plotFunction(x)
    const canvasY = toCanvasY(y)

    if (!Number.isFinite(y) || Math.abs(canvasY) > height * 8) {
      previousPoint = null
      continue
    }

    const point = {
      canvasX: toCanvasX(x),
      canvasY,
    }
    const shouldBreak = previousPoint !== null && Math.abs(canvasY - previousPoint.canvasY) > height * 0.9

    if (previousPoint && !shouldBreak) {
      context.strokeStyle = interpolateCurveColor(gradientRatioForPoint(point, width, height))
      context.beginPath()
      context.moveTo(previousPoint.canvasX, previousPoint.canvasY)
      context.lineTo(point.canvasX, point.canvasY)
      context.stroke()
    }

    previousPoint = point
  }
}

function drawCurve(
  context: CanvasRenderingContext2D,
  width: number,
  height: number,
  plotFunction: PlotFunction,
  toCanvasX: (x: number) => number,
  toCanvasY: (y: number) => number,
) {
  if (colorMode.value === 'line-gradient') {
    drawLineGradientCurve(context, width, height, plotFunction, toCanvasX, toCanvasY)
    return
  }

  const xSpan = xMax.value - xMin.value
  let drawing = false
  let previousCanvasY: number | null = null

  context.strokeStyle =
    colorMode.value === 'canvas-gradient' ? createOverallGradient(context, width, height) : solidColor.value
  context.lineWidth = 2.75
  context.lineJoin = 'round'
  context.lineCap = 'round'
  context.beginPath()

  for (let pixelX = 0; pixelX <= width; pixelX += 1) {
    const x = xMin.value + (pixelX / width) * xSpan
    const y = plotFunction(x)
    const canvasY = toCanvasY(y)

    if (!Number.isFinite(y) || Math.abs(canvasY) > height * 8) {
      drawing = false
      previousCanvasY = null
      continue
    }

    const shouldBreak = previousCanvasY !== null && Math.abs(canvasY - previousCanvasY) > height * 0.9

    if (!drawing || shouldBreak) {
      context.moveTo(toCanvasX(x), canvasY)
      drawing = true
    } else {
      context.lineTo(toCanvasX(x), canvasY)
    }

    previousCanvasY = canvasY
  }

  context.stroke()
}

function drawHoverPoint(context: CanvasRenderingContext2D, width: number, height: number) {
  const point = hoverPoint.value

  if (!point || point.canvasY < 0 || point.canvasY > height) {
    return
  }

  context.strokeStyle = '#64748b'
  context.lineWidth = 1
  context.setLineDash([5, 5])
  context.beginPath()
  context.moveTo(point.canvasX, 0)
  context.lineTo(point.canvasX, height)
  context.moveTo(0, point.canvasY)
  context.lineTo(width, point.canvasY)
  context.stroke()
  context.setLineDash([])

  context.fillStyle = '#f97316'
  context.beginPath()
  context.arc(point.canvasX, point.canvasY, 4, 0, Math.PI * 2)
  context.fill()

  const label = `(${formatNumber(point.x)}, ${formatNumber(point.y)})`
  context.font = '600 12px Arial, sans-serif'
  const labelWidth = context.measureText(label).width + 18
  const labelX = Math.min(point.canvasX + 12, width - labelWidth - 8)
  const labelY = Math.max(point.canvasY - 34, 8)

  context.fillStyle = '#111827'
  context.fillRect(labelX, labelY, labelWidth, 26)
  context.fillStyle = '#ffffff'
  context.textBaseline = 'middle'
  context.fillText(label, labelX + 9, labelY + 13)
}

function renderPlot() {
  const canvas = canvasRef.value

  if (!canvas) {
    return
  }

  const context = canvas.getContext('2d')

  if (!context) {
    return
  }

  const rect = canvas.getBoundingClientRect()
  const width = Math.max(rect.width, 1)
  const height = Math.max(rect.height, 1)
  const pixelRatio = window.devicePixelRatio || 1

  canvas.width = Math.floor(width * pixelRatio)
  canvas.height = Math.floor(height * pixelRatio)
  context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0)

  const xSpan = xMax.value - xMin.value
  const ySpan = yMax.value - yMin.value
  const hasValidRange = xSpan > 0 && ySpan > 0
  const toCanvasX = (x: number) => ((x - xMin.value) / xSpan) * width
  const toCanvasY = (y: number) => height - ((y - yMin.value) / ySpan) * height

  if (!hasValidRange) {
    errorMessage.value = '坐标范围需要满足最小值小于最大值'
    context.fillStyle = '#f8fafc'
    context.fillRect(0, 0, width, height)
    return
  }

  drawGrid(context, width, height, toCanvasX, toCanvasY)

  try {
    const plotFunction = compileExpression(expression.value)
    errorMessage.value = ''
    drawCurve(context, width, height, plotFunction, toCanvasX, toCanvasY)
    drawHoverPoint(context, width, height)
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '函数表达式无效'
  }
}

function getCanvasRect() {
  const canvas = canvasRef.value

  if (!canvas) {
    return null
  }

  return {
    canvas,
    rect: canvas.getBoundingClientRect(),
  }
}

function handleWheel(event: WheelEvent) {
  const canvasState = getCanvasRect()

  if (!canvasState || xMax.value <= xMin.value || yMax.value <= yMin.value) {
    return
  }

  event.preventDefault()

  const { rect } = canvasState
  const canvasX = Math.min(Math.max(event.clientX - rect.left, 0), rect.width)
  const canvasY = Math.min(Math.max(event.clientY - rect.top, 0), rect.height)
  const xRatio = canvasX / Math.max(rect.width, 1)
  const yRatio = (rect.height - canvasY) / Math.max(rect.height, 1)
  const xSpan = xMax.value - xMin.value
  const ySpan = yMax.value - yMin.value
  const anchorX = xMin.value + xRatio * xSpan
  const anchorY = yMin.value + yRatio * ySpan
  const zoomFactor = event.deltaY > 0 ? 1.14 : 1 / 1.14
  const unitsPerPixel = Math.max(xSpan / Math.max(rect.width, 1), ySpan / Math.max(rect.height, 1))
  const nextUnitsPerPixel = Math.min(Math.max(unitsPerPixel * zoomFactor, 0.000001), 1000000)
  const nextXSpan = nextUnitsPerPixel * rect.width
  const nextYSpan = nextUnitsPerPixel * rect.height

  const nextXMin = anchorX - xRatio * nextXSpan
  const nextYMin = anchorY - yRatio * nextYSpan

  setViewport(nextXMin, nextXMin + nextXSpan, nextYMin, nextYMin + nextYSpan)
}

function handlePointerDown(event: PointerEvent) {
  if (event.button !== 0) {
    return
  }

  const canvasState = getCanvasRect()

  if (!canvasState || xMax.value <= xMin.value || yMax.value <= yMin.value) {
    return
  }

  const { canvas, rect } = canvasState

  isColorMenuOpen.value = false
  dragState.value = {
    pointerId: event.pointerId,
    canvasX: event.clientX - rect.left,
    canvasY: event.clientY - rect.top,
    xMin: xMin.value,
    xMax: xMax.value,
    yMin: yMin.value,
    yMax: yMax.value,
  }
  hoverPoint.value = null
  canvas.setPointerCapture(event.pointerId)
  event.preventDefault()
}

function handlePanMove(event: PointerEvent, state: DragState) {
  const canvasState = getCanvasRect()

  if (!canvasState) {
    return
  }

  const { rect } = canvasState
  const nextCanvasX = event.clientX - rect.left
  const nextCanvasY = event.clientY - rect.top
  const deltaX = nextCanvasX - state.canvasX
  const deltaY = nextCanvasY - state.canvasY
  const xUnitsPerPixel = (state.xMax - state.xMin) / Math.max(rect.width, 1)
  const yUnitsPerPixel = (state.yMax - state.yMin) / Math.max(rect.height, 1)
  const xOffset = deltaX * xUnitsPerPixel
  const yOffset = deltaY * yUnitsPerPixel

  setViewport(state.xMin - xOffset, state.xMax - xOffset, state.yMin + yOffset, state.yMax + yOffset)
}

function handlePointerMove(event: PointerEvent) {
  const currentDragState = dragState.value

  if (currentDragState?.pointerId === event.pointerId) {
    handlePanMove(event, currentDragState)
    return
  }

  const canvas = canvasRef.value

  if (!canvas || xMax.value <= xMin.value || yMax.value <= yMin.value) {
    return
  }

  try {
    const plotFunction = compileExpression(expression.value)
    const rect = canvas.getBoundingClientRect()
    const x = xMin.value + ((event.clientX - rect.left) / rect.width) * (xMax.value - xMin.value)
    const y = plotFunction(x)
    const canvasY = rect.height - ((y - yMin.value) / (yMax.value - yMin.value)) * rect.height

    hoverPoint.value = {
      x,
      y,
      canvasX: event.clientX - rect.left,
      canvasY,
    }

    renderPlot()
  } catch {
    hoverPoint.value = null
  }
}

function handlePointerUp(event: PointerEvent) {
  const canvas = canvasRef.value

  if (dragState.value?.pointerId !== event.pointerId) {
    return
  }

  if (canvas?.hasPointerCapture(event.pointerId)) {
    canvas.releasePointerCapture(event.pointerId)
  }

  dragState.value = null
  hoverPoint.value = null
  renderPlot()
}

function handlePointerLeave() {
  if (dragState.value) {
    return
  }

  hoverPoint.value = null
  renderPlot()
}

watch(
  [expression, xMin, xMax, yMin, yMax, colorMode, solidColor, gradientStartColor, gradientEndColor, gradientDirection],
  () => {
  hoverPoint.value = null
  renderPlot()
  },
)

watch(() => route.query.formula, applyFormulaFromRoute, { immediate: true })

onMounted(() => {
  nextTick(() => {
    resetView()

    if (chartShellRef.value) {
      resizeObserver = new ResizeObserver(renderPlot)
      resizeObserver.observe(chartShellRef.value)
    }

    window.addEventListener('resize', renderPlot)
    renderPlot()
  })
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  window.removeEventListener('resize', renderPlot)
})
</script>

<template>
  <main class="function-page">
    <section class="tool-panel" aria-label="函数输入">
      <div class="title-block">
        <div>
          <p>Project Homepage</p>
          <h1>函数坐标系</h1>
        </div>
        <RouterLink class="manual-link" to="/manual">使用手册</RouterLink>
      </div>

      <label class="field">
        <span>输入函数</span>
        <div class="formula-input">
          <span>y =</span>
          <input
            v-model="expression"
            autocomplete="off"
            inputmode="text"
            placeholder="例如 sin(x)、x^2、sqrt(x)"
            type="text"
          />
        </div>
      </label>

      <div class="examples" aria-label="函数示例">
        <button
          v-for="example in examples"
          :key="example"
          class="example-button"
          type="button"
          @click="selectExample(example)"
        >
          {{ example }}
        </button>
      </div>

      <div class="range-grid">
        <label class="field">
          <span>X 最小</span>
          <input v-model.number="xMin" type="number" step="0.5" />
        </label>
        <label class="field">
          <span>X 最大</span>
          <input v-model.number="xMax" type="number" step="0.5" />
        </label>
        <label class="field">
          <span>Y 最小</span>
          <input v-model.number="yMin" type="number" step="0.5" />
        </label>
        <label class="field">
          <span>Y 最大</span>
          <input v-model.number="yMax" type="number" step="0.5" />
        </label>
      </div>

      <button class="reset-button" type="button" @click="resetView">重置坐标范围</button>

      <div class="status" :class="{ error: errorMessage }" aria-live="polite">
        <template v-if="errorMessage">{{ errorMessage }}</template>
        <template v-else-if="hoverPoint">
          x = {{ formatNumber(hoverPoint.x) }}，y = {{ formatNumber(hoverPoint.y) }}
        </template>
        <template v-else>滚轮缩放，按住坐标系拖动平移，鼠标移入图像可查看坐标。</template>
      </div>
    </section>

    <section ref="chartShellRef" class="chart-shell" aria-label="函数图像">
      <div class="chart-actions">
        <button
          class="color-menu-button"
          :aria-expanded="isColorMenuOpen"
          aria-controls="curve-color-menu"
          type="button"
          @click.stop="toggleColorMenu"
        >
          <span
            class="button-swatch"
            :style="{
              background:
                colorMode === 'solid'
                  ? solidColor
                  : gradientPreviewBackground(),
            }"
            aria-hidden="true"
          ></span>
          颜色
        </button>

        <div v-if="isColorMenuOpen" id="curve-color-menu" class="color-menu" @click.stop>
          <span class="panel-label">曲线颜色</span>

          <div class="color-mode-toggle" role="group" aria-label="颜色模式">
            <button
              v-for="mode in colorModes"
              :key="mode.value"
              :aria-pressed="colorMode === mode.value"
              :class="{ active: colorMode === mode.value }"
              type="button"
              @click="setColorMode(mode.value)"
            >
              {{ mode.label }}
            </button>
          </div>

          <div class="color-fields" :class="{ single: colorMode === 'solid' }">
            <label v-if="colorMode === 'solid'" class="color-field">
              <span>颜色</span>
              <input v-model="solidColor" type="color" />
              <code>{{ solidColor }}</code>
            </label>

            <template v-else>
              <label class="color-field">
                <span>起点</span>
                <input v-model="gradientStartColor" type="color" />
                <code>{{ gradientStartColor }}</code>
              </label>
              <button class="swap-color-button" type="button" @click="swapGradientColors">
                交换颜色
              </button>
              <label class="color-field">
                <span>终点</span>
                <input v-model="gradientEndColor" type="color" />
                <code>{{ gradientEndColor }}</code>
              </label>
            </template>
          </div>

          <div v-if="colorMode !== 'solid'" class="direction-panel">
            <span class="panel-label">渐变方向</span>
            <div class="direction-grid" role="group" aria-label="渐变方向">
              <button
                v-for="direction in gradientDirections"
                :key="direction.value"
                :aria-pressed="gradientDirection === direction.value"
                :class="{ active: gradientDirection === direction.value }"
                type="button"
                @click="gradientDirection = direction.value"
              >
                {{ direction.label }}
              </button>
            </div>
          </div>

          <div
            class="color-preview"
            :style="{
              background:
                colorMode === 'solid'
                  ? solidColor
                  : gradientPreviewBackground(),
            }"
            aria-hidden="true"
          ></div>
        </div>
      </div>

      <canvas
        ref="canvasRef"
        aria-label="函数图像坐标系"
        :class="{ dragging: dragState }"
        role="img"
        @pointercancel="handlePointerUp"
        @pointerdown="handlePointerDown"
        @pointerleave="handlePointerLeave"
        @pointermove="handlePointerMove"
        @pointerup="handlePointerUp"
        @wheel="handleWheel"
      ></canvas>
    </section>
  </main>
</template>

<style scoped>
.function-page {
  min-height: 100vh;
  display: grid;
  grid-template-columns: minmax(280px, 360px) minmax(0, 1fr);
  gap: 24px;
  padding: 28px;
  background:
    linear-gradient(180deg, rgba(15, 118, 110, 0.08), rgba(248, 250, 252, 0) 42%),
    #eef4f7;
  color: #111827;
}

.tool-panel {
  align-self: stretch;
  display: flex;
  flex-direction: column;
  gap: 18px;
  padding: 24px;
  border: 1px solid #d7e0e8;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
}

.title-block {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 4px;
}

.title-block p {
  margin: 0;
  color: #64748b;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
}

.title-block h1 {
  margin: 0;
  color: #0f172a;
  font-size: 32px;
  line-height: 1.1;
}

.manual-link {
  flex: 0 0 auto;
  min-height: 34px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 0 10px;
  background: #f8fafc;
  color: #0f766e;
  font-size: 13px;
  font-weight: 800;
  text-decoration: none;
}

.manual-link:hover {
  border-color: #0f766e;
  background: #ecfeff;
}

.field {
  display: grid;
  gap: 8px;
}

.field span {
  color: #475569;
  font-size: 13px;
  font-weight: 700;
}

.field input {
  width: 100%;
  min-height: 42px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 0 12px;
  background: #ffffff;
  color: #111827;
  font: inherit;
  outline: none;
  transition:
    border-color 160ms ease,
    box-shadow 160ms ease;
}

.field input:focus {
  border-color: #0f766e;
  box-shadow: 0 0 0 3px rgba(15, 118, 110, 0.16);
}

.formula-input {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  align-items: center;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  background: #ffffff;
  overflow: hidden;
}

.formula-input:focus-within {
  border-color: #0f766e;
  box-shadow: 0 0 0 3px rgba(15, 118, 110, 0.16);
}

.formula-input span {
  height: 100%;
  display: grid;
  place-items: center;
  padding: 0 12px;
  border-right: 1px solid #e2e8f0;
  background: #f8fafc;
  color: #0f172a;
  font-weight: 800;
}

.formula-input input {
  border: 0;
  box-shadow: none;
}

.formula-input input:focus {
  box-shadow: none;
}

.examples {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.panel-label {
  color: #475569;
  font-size: 13px;
  font-weight: 700;
}

.color-mode-toggle {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 4px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 4px;
  background: #f8fafc;
}

.color-mode-toggle button {
  min-height: 34px;
  border: 0;
  border-radius: 4px;
  background: transparent;
  color: #334155;
  font: inherit;
  font-size: 13px;
  font-weight: 800;
  cursor: pointer;
}

.color-mode-toggle button.active {
  background: #0f766e;
  color: #ffffff;
}

.color-fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.color-fields.single {
  grid-template-columns: 1fr;
}

.color-field {
  display: grid;
  grid-template-columns: auto 42px minmax(0, 1fr);
  gap: 8px;
  align-items: center;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  padding: 10px;
  background: #f8fafc;
}

.color-field span {
  color: #475569;
  font-size: 13px;
  font-weight: 700;
}

.color-field input[type='color'] {
  width: 42px;
  height: 32px;
  border: 0;
  padding: 0;
  background: transparent;
  cursor: pointer;
}

.color-field code {
  min-width: 0;
  color: #334155;
  font-family: Consolas, "Courier New", monospace;
  font-size: 12px;
  overflow-wrap: anywhere;
}

.swap-color-button {
  min-height: 34px;
  border: 1px dashed #94a3b8;
  border-radius: 6px;
  background: #ffffff;
  color: #0f766e;
  font: inherit;
  font-size: 13px;
  font-weight: 800;
  cursor: pointer;
}

.swap-color-button:hover {
  border-color: #0f766e;
  background: #ecfeff;
}

.swap-color-button:active {
  transform: translateY(1px);
}

.color-preview {
  height: 10px;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
}

.example-button,
.reset-button {
  min-height: 38px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  background: #f8fafc;
  color: #0f172a;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
  transition:
    transform 160ms ease,
    border-color 160ms ease,
    background 160ms ease;
}

.example-button {
  padding: 0 10px;
  font-size: 13px;
}

.example-button:hover,
.reset-button:hover {
  border-color: #0f766e;
  background: #ecfeff;
}

.example-button:active,
.reset-button:active {
  transform: translateY(1px);
}

.range-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.reset-button {
  width: 100%;
  padding: 0 14px;
}

.status {
  min-height: 50px;
  display: flex;
  align-items: center;
  margin-top: auto;
  border: 1px solid #bfdbfe;
  border-radius: 6px;
  padding: 12px;
  background: #eff6ff;
  color: #1e3a8a;
  font-size: 14px;
  line-height: 1.5;
}

.status.error {
  border-color: #fecaca;
  background: #fef2f2;
  color: #991b1b;
}

.chart-shell {
  position: relative;
  min-height: 520px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
  overflow: hidden;
}

.chart-actions {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 2;
}

.color-menu-button {
  min-height: 38px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 0 12px;
  background: rgba(255, 255, 255, 0.94);
  color: #0f172a;
  font: inherit;
  font-weight: 800;
  cursor: pointer;
  box-shadow: 0 10px 28px rgba(15, 23, 42, 0.14);
}

.color-menu-button:hover {
  border-color: #0f766e;
  background: #ffffff;
}

.button-swatch {
  width: 18px;
  height: 18px;
  border: 1px solid #cbd5e1;
  border-radius: 999px;
}

.color-menu {
  position: absolute;
  top: 46px;
  right: 0;
  width: min(320px, calc(100vw - 56px));
  display: grid;
  gap: 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  padding: 14px;
  background: rgba(255, 255, 255, 0.98);
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.18);
}

.color-menu .color-fields {
  grid-template-columns: 1fr;
}

.direction-panel {
  display: grid;
  gap: 8px;
}

.direction-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 6px;
}

.direction-grid button {
  min-height: 32px;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  padding: 0 8px;
  background: #f8fafc;
  color: #334155;
  font: inherit;
  font-size: 12px;
  font-weight: 800;
  cursor: pointer;
}

.direction-grid button.active {
  border-color: #0f766e;
  background: #ecfeff;
  color: #0f766e;
}

.chart-shell canvas {
  display: block;
  width: 100%;
  height: 100%;
  cursor: grab;
  touch-action: none;
}

.chart-shell canvas.dragging {
  cursor: grabbing;
}

@media (max-width: 900px) {
  .function-page {
    grid-template-columns: 1fr;
    padding: 18px;
  }

  .chart-shell {
    min-height: 62vh;
  }
}

@media (max-width: 520px) {
  .function-page {
    padding: 12px;
  }

  .tool-panel {
    padding: 18px;
  }

  .title-block h1 {
    font-size: 27px;
  }

  .range-grid {
    grid-template-columns: 1fr;
  }

  .title-block {
    display: grid;
  }

  .manual-link {
    justify-self: start;
  }

  .color-mode-toggle,
  .color-fields {
    grid-template-columns: 1fr;
  }
}
</style>
