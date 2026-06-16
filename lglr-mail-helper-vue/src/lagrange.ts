export type RichSegment = {
  text: string
  color: string
}

export const DEFAULT_COLOR = '#FFFFFF'
export const DEFAULT_N_COLOR = '#C0BDBA'

export const shortcutMap: Record<string, string> = {
  '#c0000FF': '#B',
  '#cFFD700': '#D',
  '#c00FF00': '#G',
  '#c000000': '#K',
  '#cFFA500': '#O',
  '#cFFC0CB': '#P',
  '#cFF0000': '#R',
  '#c800080': '#U',
  '#cFFFFFF': '#W',
  '#cFFFF00': '#Y',
}

export const presetColors = [
  { name: '蓝色', shortcut: '#B', color: '#0000FF' },
  { name: '金色', shortcut: '#D', color: '#FFD700' },
  { name: '绿色', shortcut: '#G', color: '#00FF00' },
  { name: '黑色', shortcut: '#K', color: '#000000' },
  { name: '橙色', shortcut: '#O', color: '#FFA500' },
  { name: '粉色', shortcut: '#P', color: '#FFC0CB' },
  { name: '红色', shortcut: '#R', color: '#FF0000' },
  { name: '紫色', shortcut: '#U', color: '#800080' },
  { name: '白色', shortcut: '#W', color: '#FFFFFF' },
  { name: '黄色', shortcut: '#Y', color: '#FFFF00' },
]

const normalizeHex = (color: string) => color.toUpperCase()

export const mergeSegments = (segments: RichSegment[]) => {
  const merged: RichSegment[] = []
  segments.forEach((segment) => {
    const color = normalizeHex(segment.color || DEFAULT_COLOR)
    if (!segment.text) return
    const previous = merged.at(-1)
    if (previous?.color === color) {
      previous.text += segment.text
    } else {
      merged.push({ text: segment.text, color })
    }
  })
  return merged
}

export const plainText = (segments: RichSegment[]) => segments.map((segment) => segment.text).join('')

export const exportLagrange = (segments: RichSegment[]) => {
  const output: string[] = []
  let lastColor = DEFAULT_COLOR

  mergeSegments(segments).forEach((segment) => {
    const color = normalizeHex(segment.color)
    if (color !== lastColor) {
      output.push(color === DEFAULT_N_COLOR ? '#n' : `#c${color.slice(1)}`)
      lastColor = color
    }

    output.push(segment.text.replaceAll('\n', '#r'))
  })

  while (output.at(-1) === '#r' || output.at(-1) === '#n') {
    output.pop()
  }

  return Object.entries(shortcutMap).reduce(
    (text, [longCode, shortCode]) => text.replaceAll(longCode, shortCode),
    output.join(''),
  )
}

export const importLagrange = (source: string) => {
  const reverseMap = Object.fromEntries(Object.entries(shortcutMap).map(([longCode, shortCode]) => [shortCode, longCode]))
  let text = source
  Object.entries(reverseMap).forEach(([shortCode, longCode]) => {
    text = text.replaceAll(shortCode, longCode)
  })
  text = text.replaceAll('#n', `#c${DEFAULT_N_COLOR.slice(1)}`)

  const segments: RichSegment[] = []
  let color = DEFAULT_COLOR
  let index = 0

  const pushText = (value: string) => {
    segments.push({ text: value, color })
  }

  while (index < text.length) {
    if (text.startsWith('#r', index)) {
      pushText('\n')
      index += 2
      continue
    }

    if (text.startsWith('#c', index) && /^#c[0-9A-Fa-f]{6}/.test(text.slice(index, index + 8))) {
      color = `#${text.slice(index + 2, index + 8).toUpperCase()}`
      index += 8
      continue
    }

    if (text.startsWith('#l', index)) {
      index += 2
      continue
    }

    pushText(text[index])
    index += 1
  }

  return mergeSegments(segments)
}
