import { setANICursorWithGroupElement, type CursorController } from 'ani-cursor.js'

import normalCursor from '@/assets/Miku/Normal Select.ani?url'
import linkCursor from '@/assets/Miku/Link.ani?url'
import textCursor from '@/assets/Miku/Text Select.ani?url'
import precisionCursor from '@/assets/Miku/Precision Select.ani?url'
import moveCursor from '@/assets/Miku/Move.ani?url'
import helpCursor from '@/assets/Miku/Help Select.ani?url'
import workCursor from '@/assets/Miku/Work.ani?url'
import unavailableCursor from '@/assets/Miku/Unavailable.ani?url'

const CURSOR_SIZE = 32

interface CursorBinding {
  selectors: string[]
  aniUrl: string
  fallback: string
  hotspot?: [number, number]
}

let controllers: CursorController[] = []

const cursorBindings: CursorBinding[] = [
  {
    selectors: ['html', 'body', '#app'],
    aniUrl: normalCursor,
    fallback: 'auto',
  },
  {
    selectors: [
      'html :is([title], [aria-describedby]):not(a):not(button):not(input):not(textarea):not(select)',
    ],
    aniUrl: helpCursor,
    fallback: 'help',
  },
  {
    selectors: [
      'html :is(input:not([type="button"]):not([type="submit"]):not([type="reset"]):not([type="color"]):not([type="range"]), textarea, [contenteditable="true"])',
    ],
    aniUrl: textCursor,
    fallback: 'text',
  },
  {
    selectors: ['html :is(.chart-shell canvas:not(.dragging), canvas[role="img"]:not(.dragging))'],
    aniUrl: precisionCursor,
    fallback: 'crosshair',
  },
  {
    selectors: ['html :is(.chart-shell canvas.dragging, canvas[role="img"].dragging)'],
    aniUrl: moveCursor,
    fallback: 'grabbing',
  },
  {
    selectors: [
      'html :is(a, button, [role="button"], summary, label[for], select, input[type="button"], input[type="submit"], input[type="reset"], input[type="color"], input[type="range"])',
    ],
    aniUrl: linkCursor,
    fallback: 'pointer',
  },
  {
    selectors: ['html :is([aria-busy="true"], .loading, .is-loading)'],
    aniUrl: workCursor,
    fallback: 'progress',
  },
  {
    selectors: [
      'html :is(button:disabled, input:disabled, textarea:disabled, select:disabled, [aria-disabled="true"], .disabled, .is-disabled)',
    ],
    aniUrl: unavailableCursor,
    fallback: 'not-allowed',
  },
]

function applyCursorBinding({ selectors, aniUrl, fallback, hotspot }: CursorBinding): CursorController {
  return setANICursorWithGroupElement(selectors, aniUrl, fallback, CURSOR_SIZE, CURSOR_SIZE, hotspot?.[0], hotspot?.[1])
}

export function destroyMikuCursor() {
  controllers.forEach((controller) => controller.destroy())
  controllers = []
}

export function installMikuCursor() {
  destroyMikuCursor()

  controllers = cursorBindings.map(applyCursorBinding)

  controllers.forEach((controller) => {
    controller.ready.catch((error: unknown) => {
      console.warn('Failed to load Miku ANI cursor.', error)
    })
  })
}

if (import.meta.hot) {
  import.meta.hot.dispose(destroyMikuCursor)
}
