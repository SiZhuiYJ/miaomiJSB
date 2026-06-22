<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { storeToRefs } from 'pinia'
import {
  postJson,
  type ConvertLyricsResponse,
  type LyricsResponse,
  type SearchItem,
  type SearchResponse,
  type SongInfo,
  type SongLinkResponse,
  type TrackResponse,
} from './api'
import { useAppStore, type SearchHistoryItem } from './stores/appStore'

const converter = reactive({
  input: '',
  output: '',
})

const appStore = useAppStore()
const { cookies, pageOptions, searchHistory } = storeToRefs(appStore)

const results = ref<SearchItem[]>([])
const tracks = ref<SongInfo[]>([])
const lyrics = ref<LyricsResponse | null>(null)
const activeContainer = ref<SearchItem | null>(null)
const activeSongId = ref('')
const songLink = ref('')
const songLinkSource = ref('')
const songLinkQuality = ref('')
const busy = ref('')
const error = ref('')

const hasResults = computed(() => results.value.length > 0)
const hasTracks = computed(() => tracks.value.length > 0)
const canSearch = computed(() => pageOptions.value.keyword.trim().length > 0 && !busy.value)
const canDirectLoad = computed(
  () => pageOptions.value.keyword.trim().length > 0 && pageOptions.value.provider !== 'all' && !busy.value,
)

async function search() {
  if (!canSearch.value) return
  await run('search', async () => {
    lyrics.value = null
    songLink.value = ''
    songLinkSource.value = ''
    songLinkQuality.value = ''
    tracks.value = []
    activeContainer.value = null
    activeSongId.value = ''
    const response = await postJson<SearchResponse>('/api/search', searchPayload())
    results.value = response.items
    appStore.addSearchHistory({
      keyword: pageOptions.value.keyword,
      provider: pageOptions.value.provider,
      type: pageOptions.value.type,
    })
  })
}

async function directLoad() {
  if (!canDirectLoad.value) return
  const provider = pageOptions.value.provider === 'all' ? 'netease' : pageOptions.value.provider
  const id = pageOptions.value.keyword.trim()

  if (pageOptions.value.type === 'song') {
    await loadLyrics({
      provider,
      type: 'song',
      id,
      title: id,
      artists: [],
      album: '',
      durationMs: null,
      coverUrl: null,
      songCount: null,
      description: null,
    })
    appStore.addSearchHistory({ keyword: id, provider, type: 'song' })
    return
  }

  await expandContainer({
    provider,
    type: pageOptions.value.type,
    id,
    title: id,
    artists: [],
    album: '',
    durationMs: null,
    coverUrl: null,
    songCount: null,
    description: null,
  })
  appStore.addSearchHistory({ keyword: id, provider, type: pageOptions.value.type })
}

async function expandContainer(item: SearchItem) {
  await run(`tracks:${item.provider}:${item.id}`, async () => {
    activeContainer.value = item
    tracks.value = []
    const response = await postJson<TrackResponse>('/api/tracks', {
      provider: item.provider,
      type: item.type,
      id: item.id,
      ...cookiesPayload(),
    })
    tracks.value = response.tracks
  })
}

async function loadLyrics(item: SearchItem | SongInfo) {
  const provider = item.provider
  const songId = 'displayId' in item ? item.displayId || item.id : item.id
  await run(`lyrics:${provider}:${songId}`, async () => {
    songLink.value = ''
    songLinkSource.value = ''
    songLinkQuality.value = ''
    activeSongId.value = `${provider}:${songId}`
    lyrics.value = await postJson<LyricsResponse>('/api/lyrics', {
      provider,
      songId,
      format: pageOptions.value.lyricFormat,
      includeTranslation: pageOptions.value.includeTranslation,
      includeTransliteration: pageOptions.value.includeTransliteration,
      ...cookiesPayload(),
    })
  })
}

async function refreshLyricsFormat() {
  const current = lyrics.value
  if (!current) return
  await loadLyrics({
    ...current.song,
    displayId: current.song.displayId,
  })
}

async function getSongLink() {
  const current = lyrics.value?.song
  if (!current) return
  await run(`link:${current.provider}:${current.displayId}`, async () => {
    await requestSongLink(current)
  })
}

async function requestSongLink(song: SongInfo) {
  const response = await postJson<SongLinkResponse>('/api/song-link', {
    provider: song.provider,
    songId: song.displayId,
    quality: pageOptions.value.audioQuality,
    ...cookiesPayload(),
  })
  songLink.value = response.url
  songLinkSource.value = `${response.source} · ${qualityLabel(response.quality)}`
  songLinkQuality.value = response.quality
  return response
}

async function convertLyrics() {
  await run('convert', async () => {
    const response = await postJson<ConvertLyricsResponse>('/api/convert', {
      input: converter.input,
      from: pageOptions.value.converterFrom,
      to: pageOptions.value.converterTo,
      durationMs: pageOptions.value.converterDurationMs,
    })
    converter.output = response.output
  })
}

async function copyLyrics() {
  if (!lyrics.value?.output) return
  await navigator.clipboard.writeText(lyrics.value.output)
}

function downloadLyrics() {
  if (!lyrics.value) return
  const blob = new Blob([lyrics.value.output], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `${safeFilename(lyrics.value.song.title)}.${lyrics.value.format}`
  anchor.click()
  URL.revokeObjectURL(url)
}

async function downloadAudio() {
  const current = lyrics.value?.song
  if (!current) return

  await run(`download-audio:${current.provider}:${current.displayId}`, async () => {
    const response = songLink.value && songLinkQuality.value === pageOptions.value.audioQuality
      ? { url: songLink.value, source: songLinkSource.value, quality: pageOptions.value.audioQuality }
      : await requestSongLink(current)

    const anchor = document.createElement('a')
    anchor.href = response.url
    anchor.download = `${safeFilename(`${current.title} - ${artistsLabel(current.artists)}`)}.${audioExtension(response.url)}`
    anchor.target = '_blank'
    anchor.rel = 'noreferrer'
    anchor.click()
  })
}

function searchPayload() {
  return {
    keyword: pageOptions.value.keyword,
    provider: pageOptions.value.provider,
    type: pageOptions.value.type,
    ...cookiesPayload(),
  }
}

function cookiesPayload() {
  return {
    netEaseCookie: cookies.value.netEaseCookie,
    qqCookie: cookies.value.qqCookie,
  }
}

function applyHistory(item: SearchHistoryItem) {
  pageOptions.value.keyword = item.keyword
  pageOptions.value.provider = item.provider
  pageOptions.value.type = item.type
}

async function run(name: string, action: () => Promise<void>) {
  busy.value = name
  error.value = ''
  try {
    await action()
  } catch (err) {
    error.value = err instanceof Error ? err.message : String(err)
  } finally {
    busy.value = ''
  }
}

function providerLabel(provider: string) {
  return provider === 'qq' ? 'QQ音乐' : '网易云'
}

function typeLabel(type: string) {
  if (type === 'album') return '专辑'
  if (type === 'playlist') return '歌单'
  return '单曲'
}

function artistsLabel(artists: readonly string[] | undefined) {
  return artists?.filter(Boolean).join(' / ') || '未知歌手'
}

function formatDuration(durationMs?: number | null) {
  if (!durationMs) return ''
  const totalSeconds = Math.round(durationMs / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
}

function qualityLabel(quality: string) {
  const labels: Record<string, string> = {
    standard: '标准',
    higher: '较高',
    exhigh: '极高',
    lossless: '无损',
    hires: 'Hi-Res',
  }
  return labels[quality] || quality
}

function audioExtension(url: string) {
  const path = new URL(url, window.location.href).pathname.toLowerCase()
  if (path.endsWith('.flac')) return 'flac'
  if (path.endsWith('.m4a')) return 'm4a'
  if (path.endsWith('.ogg')) return 'ogg'
  return 'mp3'
}

function safeFilename(value: string) {
  return value.replace(/[\\/:*?"<>|]/g, '_') || 'lyrics'
}
</script>

<template>
  <div class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <div>
          <p class="eyebrow">Music Lyrics</p>
          <h1>歌词检索与转换</h1>
        </div>
        <span class="api-pill">前后分离</span>
      </div>

      <form class="search-form" @submit.prevent="search">
        <label>
          搜索内容
          <input
            v-model.trim="pageOptions.keyword"
            type="search"
            placeholder="歌曲、专辑、歌单关键词或 ID"
            autocomplete="off"
          />
        </label>

        <div v-if="searchHistory.length" class="history-box">
          <div class="mini-head">
            <span>搜索记录</span>
            <button type="button" @click="appStore.clearSearchHistory">清空</button>
          </div>
          <div class="history-list">
            <button
              v-for="item in searchHistory"
              :key="item.id"
              type="button"
              @click="applyHistory(item)"
            >
              <strong>{{ item.keyword }}</strong>
              <span>{{ providerLabel(item.provider) }} · {{ typeLabel(item.type) }}</span>
            </button>
          </div>
        </div>

        <div class="field-row">
          <label>
            平台
            <select v-model="pageOptions.provider">
              <option value="netease">网易云</option>
              <option value="qq">QQ音乐</option>
              <option value="all">聚合</option>
            </select>
          </label>
          <label>
            类型
            <select v-model="pageOptions.type">
              <option value="song">单曲</option>
              <option value="album">专辑</option>
              <option value="playlist">歌单</option>
            </select>
          </label>
        </div>

        <div class="field-row">
          <label>
            输出格式
            <select v-model="pageOptions.lyricFormat" @change="refreshLyricsFormat">
              <option value="lrc">LRC</option>
              <option value="srt">SRT</option>
            </select>
          </label>
          <label class="check-field">
            <input v-model="pageOptions.includeTranslation" type="checkbox" @change="refreshLyricsFormat" />
            合并译文
          </label>
        </div>

        <label class="check-field">
          <input v-model="pageOptions.includeTransliteration" type="checkbox" @change="refreshLyricsFormat" />
          合并音译
        </label>

        <label>
          音质
          <select v-model="pageOptions.audioQuality">
            <option value="standard">标准</option>
            <option value="higher">较高</option>
            <option value="exhigh">极高</option>
            <option value="lossless">无损</option>
            <option value="hires">Hi-Res</option>
          </select>
        </label>

        <details class="cookie-box">
          <summary>Cookie（可选）</summary>
          <div class="cookie-help">
            <p>获取方式：登录 music.163.com 或 y.qq.com，按 F12 打开开发者工具，切到 Network，刷新页面或播放/搜索一首歌，点开相关请求后在 Request Headers 中复制 Cookie 的值。</p>
            <p>网易云常见请求：weapi/...；QQ 音乐常见请求：musicu.fcg、fcg_query_lyric_new.fcg。</p>
            <p>只粘贴 Cookie 后面的内容，不要带 Cookie: 前缀。Cookie 等同登录凭证，只建议本地使用，不要提交到 Git 或发给别人。</p>
            <code>MUSIC_U=xxx; __csrf=xxx; NMTID=xxx; ...</code>
            <code>uin=xxx; qm_keyst=xxx; qqmusic_key=xxx; p_skey=xxx; ...</code>
          </div>
          <label>
            网易云 Cookie
            <textarea v-model.trim="cookies.netEaseCookie" rows="3" spellcheck="false" />
          </label>
          <label>
            QQ音乐 Cookie
            <textarea v-model.trim="cookies.qqCookie" rows="3" spellcheck="false" />
          </label>
        </details>

        <div class="option-actions">
          <button type="button" @click="appStore.resetPageOptions">重置选项</button>
          <button type="button" @click="appStore.clearCookies">清空 Cookie</button>
          <button type="button" @click="appStore.clearSearchHistory">清空记录</button>
        </div>

        <div class="button-row">
          <button class="primary-button" type="submit" :disabled="!canSearch">
            {{ busy === 'search' ? '搜索中...' : '搜索' }}
          </button>
          <button type="button" :disabled="!canDirectLoad" @click="directLoad">按 ID/链接直取</button>
        </div>
      </form>

      <p v-if="error" class="error-text">{{ error }}</p>
    </aside>

    <main class="workspace">
      <section class="result-pane">
        <div class="section-head">
          <div>
            <p class="eyebrow">Results</p>
            <h2>搜索结果</h2>
          </div>
          <span>{{ results.length }} 条</span>
        </div>

        <div v-if="!hasResults" class="empty-state">
          输入关键词后搜索，单曲可直接获取歌词；专辑和歌单可先展开曲目。
        </div>

        <ul v-else class="result-list">
          <li v-for="item in results" :key="`${item.provider}:${item.type}:${item.id}`" class="result-item">
            <img v-if="item.coverUrl" :src="item.coverUrl" alt="" loading="lazy" />
            <div class="item-main">
              <div class="item-title">
                <strong>{{ item.title }}</strong>
                <span>{{ providerLabel(item.provider) }} · {{ typeLabel(item.type) }}</span>
              </div>
              <p>
                {{ artistsLabel(item.artists) }}
                <template v-if="item.album"> · {{ item.album }}</template>
                <template v-if="item.durationMs"> · {{ formatDuration(item.durationMs) }}</template>
                <template v-if="item.songCount"> · {{ item.songCount }} 首</template>
              </p>
            </div>
            <button v-if="item.type === 'song'" type="button" @click="loadLyrics(item)">
              歌词
            </button>
            <button v-else type="button" @click="expandContainer(item)">
              展开
            </button>
          </li>
        </ul>

        <div v-if="activeContainer" class="tracks-block">
          <div class="section-head compact">
            <div>
              <p class="eyebrow">Tracks</p>
              <h2>{{ activeContainer.title }}</h2>
            </div>
            <span>{{ tracks.length }} 首</span>
          </div>
          <div v-if="busy.startsWith('tracks:')" class="empty-state">曲目加载中...</div>
          <ul v-else-if="hasTracks" class="track-list">
            <li v-for="track in tracks" :key="`${track.provider}:${track.displayId}`">
              <span>{{ track.title }} · {{ artistsLabel(track.artists) }}</span>
              <button type="button" @click="loadLyrics(track)">歌词</button>
            </li>
          </ul>
        </div>
      </section>

      <section class="lyrics-pane">
        <div class="section-head">
          <div>
            <p class="eyebrow">Lyrics</p>
            <h2>歌词预览</h2>
          </div>
          <div class="actions">
            <button type="button" :disabled="!lyrics" @click="copyLyrics">复制</button>
            <button type="button" :disabled="!lyrics" @click="downloadLyrics">下载</button>
            <button type="button" :disabled="!lyrics" @click="getSongLink">试听链接</button>
            <button type="button" :disabled="!lyrics" @click="downloadAudio">下载音频</button>
          </div>
        </div>

        <div v-if="lyrics" class="song-summary">
          <img v-if="lyrics.song.coverUrl" :src="lyrics.song.coverUrl" alt="" />
          <div>
            <h3>{{ lyrics.song.title }}</h3>
            <p>{{ artistsLabel(lyrics.song.artists) }} · {{ lyrics.song.album || '未知专辑' }}</p>
            <p>{{ providerLabel(lyrics.song.provider) }} · {{ formatDuration(lyrics.song.durationMs) }}</p>
          </div>
        </div>

        <div v-if="songLink" class="audio-player">
          <audio :src="songLink" controls autoplay preload="metadata" />
          <span>{{ songLinkSource }}</span>
        </div>

        <a v-if="songLink" class="song-link" :href="songLink" target="_blank" rel="noreferrer">
          {{ songLink }}
        </a>

        <div v-if="busy.startsWith('lyrics:')" class="empty-state">歌词加载中...</div>
        <pre v-else-if="lyrics" class="lyrics-output">{{ lyrics.output }}</pre>
        <div v-else class="empty-state">选择一首歌后，这里显示可复制和下载的歌词。</div>

        <div class="converter">
          <div class="section-head compact">
            <div>
              <p class="eyebrow">Convert</p>
              <h2>LRC / SRT 互转</h2>
            </div>
            <button type="button" @click="convertLyrics">转换</button>
          </div>
          <div class="field-row">
            <label>
              从
              <select v-model="pageOptions.converterFrom">
                <option value="lrc">LRC</option>
                <option value="srt">SRT</option>
              </select>
            </label>
            <label>
              到
              <select v-model="pageOptions.converterTo">
                <option value="srt">SRT</option>
                <option value="lrc">LRC</option>
              </select>
            </label>
            <label>
              时长 ms
              <input v-model.number="pageOptions.converterDurationMs" type="number" min="0" step="1000" />
            </label>
          </div>
          <div class="convert-grid">
            <textarea v-model="converter.input" placeholder="粘贴 LRC 或 SRT" spellcheck="false" />
            <textarea v-model="converter.output" placeholder="转换结果" spellcheck="false" readonly />
          </div>
        </div>
      </section>
    </main>
  </div>
</template>
