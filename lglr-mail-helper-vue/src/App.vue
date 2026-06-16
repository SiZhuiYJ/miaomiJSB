<script setup lang="ts">
import { computed, ref } from 'vue'
import { exportLagrange, importLagrange, plainText, presetColors, type RichSegment } from './lagrange'

const segments = ref<RichSegment[]>([
  { text: '同盟成员请集结至 (123,456) ，准备执行封锁任务。', color: '#FFFFFF' },
])
const output = ref('')
const selectedColor = ref('#FFD700')
const editor = ref<HTMLElement>()
const activeRange = ref<Range | null>(null)

const textLength = computed(() => output.value.length)
const isOverLimit = computed(() => textLength.value > 300)
const previewText = computed(() => plainText(segments.value))

const escapeHtml = (value: string) =>
  value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;')

const highlightCoordinates = (text: string) =>
  escapeHtml(text)
    .replaceAll('\n', '<br>')
    .replace(/(\(\d+,\d+\))/g, '<mark>$1</mark>')

const commitEditorText = () => {
  segments.value = [{ text: editor.value?.innerText.replace(/\n$/, '') ?? '', color: '#FFFFFF' }]
}

const rememberSelection = () => {
  const selection = window.getSelection()
  if (!selection || selection.rangeCount === 0 || !editor.value?.contains(selection.anchorNode)) return
  activeRange.value = selection.getRangeAt(0).cloneRange()
}

const applyColor = () => {
  const range = activeRange.value
  const root = editor.value
  if (!range || !root || range.collapsed) return

  const fullText = root.innerText.replace(/\n$/, '')
  const beforeRange = range.cloneRange()
  beforeRange.selectNodeContents(root)
  beforeRange.setEnd(range.startContainer, range.startOffset)

  const start = beforeRange.toString().length
  const selected = range.toString().length
  const end = start + selected
  segments.value = [
    { text: fullText.slice(0, start), color: '#FFFFFF' },
    { text: fullText.slice(start, end), color: selectedColor.value.toUpperCase() },
    { text: fullText.slice(end), color: '#FFFFFF' },
  ]
  activeRange.value = null
}

const onExport = () => {
  output.value = exportLagrange(segments.value)
}

const onImport = () => {
  segments.value = importLagrange(output.value)
}

const copyOutput = async () => {
  await navigator.clipboard.writeText(output.value)
}

const overwriteFromClipboard = async () => {
  output.value = await navigator.clipboard.readText()
}
</script>

<template>
  <main class="app-shell">
    <section class="hero">
      <p class="eyebrow">Lagrange Mail Helper · Vue + TypeScript + SCSS</p>
      <h1>无尽的拉格朗日公告邮件撰写助手</h1>
      <p>将原 Tkinter 多色文本编辑器转化为浏览器端工具，支持彩色预览、坐标高亮、导入导出与字数检查。</p>
    </section>

    <section class="workspace">
      <article class="panel">
        <div class="panel__title">
          <h2>彩色预览编辑区</h2>
          <span>选中文本后选择颜色并应用</span>
        </div>

        <div
          ref="editor"
          class="rich-editor"
          contenteditable="true"
          @input="commitEditorText"
          @mouseup="rememberSelection"
          @keyup="rememberSelection"
        >
          <template v-for="(segment, index) in segments" :key="`${index}-${segment.color}`">
            <span :style="{ color: segment.color }" v-html="highlightCoordinates(segment.text)" />
          </template>
        </div>

        <div class="color-tools">
          <label>
            自定义颜色
            <input v-model="selectedColor" type="color" />
          </label>
          <button v-for="preset in presetColors" :key="preset.shortcut" type="button" @click="selectedColor = preset.color">
            <i :style="{ backgroundColor: preset.color }" />
            {{ preset.name }} {{ preset.shortcut }}
          </button>
          <button class="primary" type="button" @click="applyColor">应用到选中文本</button>
        </div>
      </article>

      <article class="panel">
        <div class="panel__title">
          <h2>拉格朗日格式内容</h2>
          <span :class="{ danger: isOverLimit }">文本长度：{{ textLength }}/300</span>
        </div>
        <textarea v-model="output" spellcheck="false" placeholder="导出后显示可直接粘贴到邮件中的格式化内容，也可在此粘贴原始格式后点击导入。" />
        <div class="actions">
          <button class="primary" type="button" @click="onExport">导出↓</button>
          <button type="button" @click="onImport">↑导入</button>
          <button type="button" @click="copyOutput">复制输出</button>
          <button type="button" @click="overwriteFromClipboard">覆盖粘贴</button>
        </div>
      </article>
    </section>

    <section class="help-grid">
      <article>
        <h3>实时预览</h3>
        <p>{{ previewText || '在上方输入公告内容后，这里会同步维护纯文本状态。' }}</p>
      </article>
      <article>
        <h3>控制符说明</h3>
        <p>#r 换行，#n 清除颜色，#c + 6 位 HEX 设置高级颜色，#B/#D/#G 等为常用颜色快捷码。</p>
      </article>
    </section>
  </main>
</template>
