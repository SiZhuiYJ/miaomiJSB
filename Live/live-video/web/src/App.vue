<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

type ViewMode = 'home' | 'publisher' | 'viewer'

type SignalMessage = {
  type?: string
  role?: 'publisher' | 'viewer'
  id?: string
  viewerId?: string
  target?: 'publisher' | 'viewer'
  sdp?: RTCSessionDescriptionInit
  candidate?: RTCIceCandidateInit
  text?: string
  color?: string
}

type PublisherSession = {
  pc: RTCPeerConnection
  pending: RTCIceCandidateInit[]
}

const iceServers: RTCIceServer[] = [{ urls: 'stun:stun.l.google.com:19302' }]
const peerConnectionConfig: RTCConfiguration = {
  iceServers,
  bundlePolicy: 'max-bundle',
}
const publisherMediaConstraints: MediaStreamConstraints = {
  video: {
    width: { ideal: 960, max: 1280 },
    height: { ideal: 540, max: 720 },
    frameRate: { ideal: 24, max: 30 },
  },
  audio: {
    echoCancellation: true,
    noiseSuppression: true,
    autoGainControl: true,
  },
}
const videoEncoding: RTCRtpEncodingParameters = {
  maxBitrate: 900_000,
  maxFramerate: 24,
}
const danmakuColors = ['#ffffff', '#ff6b6b', '#ffd93d', '#38d39f', '#4d96ff', '#ff8bd1']

const route = ref<ViewMode>(readRoute())
const wsUrl = computed(getWebSocketUrl)

const publisherVideo = ref<HTMLVideoElement | null>(null)
const publisherStream = ref<MediaStream | null>(null)
const publisherLive = ref(false)
const publisherTip = ref('等待开播')
const publisherViewerCount = ref(0)
let publisherWs: WebSocket | null = null
const publisherSessions = new Map<string, PublisherSession>()

const viewerVideo = ref<HTMLVideoElement | null>(null)
const danmakuLayer = ref<HTMLDivElement | null>(null)
const danmakuText = ref('')
const viewerTip = ref('等待主播开播')
const viewerLive = ref(false)
const viewerCanEnableSound = ref(false)
const viewerId = `v_${Math.random().toString(36).slice(2, 8)}`
let viewerWs: WebSocket | null = null
let viewerPc: RTCPeerConnection | null = null
let viewerPending: RTCIceCandidateInit[] = []
let viewerRemoteStream: MediaStream | null = null
let viewerConnectionToken = 0

function readRoute(): ViewMode {
  const value = window.location.hash.replace(/^#\/?/, '')
  if (value === 'publisher' || value === 'viewer') {
    return value
  }

  return 'home'
}

function go(next: ViewMode) {
  window.location.hash = next === 'home' ? '/' : `/${next}`
}

function syncRoute() {
  route.value = readRoute()
}

function getWebSocketUrl() {
  const directWsUrl = import.meta.env.VITE_SIGNALING_WS_URL as string | undefined
  if (directWsUrl) {
    return directWsUrl
  }

  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined
  if (apiBaseUrl) {
    const url = new URL('/ws', apiBaseUrl)
    url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:'
    return url.toString()
  }

  const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws'
  return `${protocol}://${window.location.host}/ws`
}

function parseSignal(data: string): SignalMessage | null {
  try {
    return JSON.parse(data) as SignalMessage
  } catch {
    return null
  }
}

async function startPublishing() {
  if (publisherLive.value) {
    return
  }

  try {
    publisherTip.value = '正在打开摄像头和麦克风'
    publisherStream.value = await navigator.mediaDevices.getUserMedia(publisherMediaConstraints)
    publisherStream.value.getVideoTracks().forEach((track) => {
      track.contentHint = 'motion'
    })
    publisherStream.value.getAudioTracks().forEach((track) => {
      track.contentHint = 'speech'
    })

    if (publisherVideo.value) {
      publisherVideo.value.srcObject = publisherStream.value
    }

    publisherWs = new WebSocket(wsUrl.value)
    publisherWs.onopen = () => {
      publisherWs?.send(JSON.stringify({ type: 'join', role: 'publisher' }))
      publisherLive.value = true
      publisherTip.value = '直播中'
    }
    publisherWs.onclose = () => {
      if (publisherLive.value) {
        stopPublishing('连接已断开')
      }
    }
    publisherWs.onerror = () => {
      publisherTip.value = '信令服务连接失败'
    }
    publisherWs.onmessage = async (event) => {
      const message = parseSignal(event.data)
      if (!message?.type) {
        return
      }

      if (message.type === 'viewer-joined' && message.viewerId) {
        await createOffer(message.viewerId)
      }

      if (message.type === 'answer' && message.viewerId && message.sdp) {
        const session = publisherSessions.get(message.viewerId)
        if (session) {
          await session.pc.setRemoteDescription(message.sdp)
          for (const candidate of session.pending) {
            await session.pc.addIceCandidate(candidate)
          }
          session.pending = []
        }
      }

      if (message.type === 'candidate' && message.target === 'publisher' && message.viewerId && message.candidate) {
        const session = publisherSessions.get(message.viewerId)
        if (!session) {
          return
        }

        if (session.pc.remoteDescription) {
          await session.pc.addIceCandidate(message.candidate)
        } else {
          session.pending.push(message.candidate)
        }
      }

      if (message.type === 'viewer-left' && message.viewerId) {
        publisherSessions.get(message.viewerId)?.pc.close()
        publisherSessions.delete(message.viewerId)
        publisherViewerCount.value = publisherSessions.size
      }
    }
  } catch (error) {
    publisherTip.value = error instanceof Error ? error.message : '无法开始推流'
    stopPublishing(publisherTip.value)
  }
}

async function createOffer(viewerIdToOffer: string) {
  if (!publisherStream.value || !publisherWs || publisherWs.readyState !== WebSocket.OPEN) {
    return
  }

  publisherSessions.get(viewerIdToOffer)?.pc.close()

  const pc = new RTCPeerConnection(peerConnectionConfig)
  publisherSessions.set(viewerIdToOffer, { pc, pending: [] })
  publisherViewerCount.value = publisherSessions.size

  publisherStream.value.getTracks().forEach((track) => {
    pc.addTransceiver(track, {
      direction: 'sendonly',
      streams: [publisherStream.value as MediaStream],
      sendEncodings: track.kind === 'video' ? [videoEncoding] : undefined,
    })
  })

  pc.onicecandidate = (event) => {
    if (event.candidate && publisherWs?.readyState === WebSocket.OPEN) {
      publisherWs.send(
        JSON.stringify({
          type: 'candidate',
          target: 'viewer',
          viewerId: viewerIdToOffer,
          candidate: event.candidate,
        }),
      )
    }
  }
  pc.onconnectionstatechange = () => {
    if (pc.connectionState === 'failed' || pc.connectionState === 'closed') {
      publisherSessions.delete(viewerIdToOffer)
      publisherViewerCount.value = publisherSessions.size
    }
  }

  const offer = await pc.createOffer()
  await pc.setLocalDescription(offer)

  publisherWs.send(JSON.stringify({ type: 'offer', viewerId: viewerIdToOffer, sdp: pc.localDescription }))
}

function stopPublishing(status = '已停止推流') {
  publisherWs?.close()
  publisherWs = null

  publisherSessions.forEach((session) => session.pc.close())
  publisherSessions.clear()
  publisherViewerCount.value = 0

  publisherStream.value?.getTracks().forEach((track) => track.stop())
  publisherStream.value = null

  if (publisherVideo.value) {
    publisherVideo.value.srcObject = null
  }

  publisherLive.value = false
  publisherTip.value = status
}

function setViewerStatus(text: string, live: boolean) {
  viewerTip.value = text
  viewerLive.value = live
}

function connectViewer() {
  disconnectViewer('正在连接')

  const socket = new WebSocket(wsUrl.value)
  viewerWs = socket
  socket.onopen = () => {
    socket.send(JSON.stringify({ type: 'join', role: 'viewer', id: viewerId }))
  }
  socket.onerror = () => {
    if (viewerWs !== socket) {
      return
    }
    setViewerStatus('信令服务连接失败', false)
  }
  socket.onclose = () => {
    if (viewerWs !== socket) {
      return
    }
    if (route.value === 'viewer' && !viewerLive.value) {
      setViewerStatus('连接已断开', false)
    }
  }
  socket.onmessage = async (event) => {
    if (viewerWs !== socket) {
      return
    }

    const message = parseSignal(event.data)
    if (!message?.type) {
      return
    }

    if (message.type === 'waiting') {
      setViewerStatus('等待主播开播', false)
    }

    if (message.type === 'publisher-ready') {
      setViewerStatus('主播在线，正在建立视频连接', false)
    }

    if (message.type === 'publisher-left') {
      resetViewerPeer()
      setViewerStatus('直播已结束', false)
    }

    if (message.type === 'offer' && message.sdp) {
      await acceptOffer(message.sdp)
    }

    if (message.type === 'candidate' && message.target === 'viewer' && message.candidate) {
      if (viewerPc?.remoteDescription) {
        await viewerPc.addIceCandidate(message.candidate)
      } else {
        viewerPending.push(message.candidate)
      }
    }

    if (message.type === 'danmaku' && message.text) {
      showDanmaku(message.text, message.color)
    }
  }
}

async function acceptOffer(sdp: RTCSessionDescriptionInit) {
  resetViewerPeer(false)
  const token = ++viewerConnectionToken

  const pc = new RTCPeerConnection(peerConnectionConfig)
  viewerPc = pc
  viewerPending = []
  viewerRemoteStream = new MediaStream()

  if (viewerVideo.value) {
    viewerVideo.value.srcObject = viewerRemoteStream
    viewerVideo.value.muted = true
    viewerVideo.value.playsInline = true
  }

  pc.ontrack = (event) => {
    if (token !== viewerConnectionToken || pc !== viewerPc || !viewerRemoteStream) {
      return
    }

    if (!viewerRemoteStream.getTracks().some((track) => track.id === event.track.id)) {
      viewerRemoteStream.addTrack(event.track)
    }

    if (viewerVideo.value && viewerVideo.value.srcObject !== viewerRemoteStream) {
      viewerVideo.value.srcObject = viewerRemoteStream
    }

    viewerCanEnableSound.value = true
    setViewerStatus(event.track.kind === 'video' ? '已收到视频，正在播放' : '已收到音频，等待视频', false)
    event.track.onunmute = () => {
      void playViewerVideo()
    }
    void playViewerVideo()
  }

  pc.onicecandidate = (event) => {
    if (event.candidate && viewerWs?.readyState === WebSocket.OPEN) {
      viewerWs.send(
        JSON.stringify({
          type: 'candidate',
          target: 'publisher',
          viewerId,
          candidate: event.candidate,
        }),
      )
    }
  }
  pc.onconnectionstatechange = () => {
    if (token !== viewerConnectionToken || pc !== viewerPc) {
      return
    }

    if (pc.connectionState === 'connected') {
      setViewerStatus('媒体连接已建立', viewerLive.value)
      void playViewerVideo()
    }

    if (pc.connectionState === 'failed') {
      resetViewerPeer(false)
      setViewerStatus('媒体连接失败，请重新连接', false)
    }
  }

  await pc.setRemoteDescription(sdp)
  if (token !== viewerConnectionToken || pc !== viewerPc) {
    return
  }

  const answer = await pc.createAnswer()
  await pc.setLocalDescription(answer)

  viewerWs?.send(JSON.stringify({ type: 'answer', viewerId, sdp: pc.localDescription }))

  for (const candidate of viewerPending) {
    await pc.addIceCandidate(candidate)
  }
  viewerPending = []
}

async function playViewerVideo() {
  const video = viewerVideo.value
  if (!video || !video.srcObject) {
    return
  }

  try {
    await video.play()
    viewerCanEnableSound.value = video.muted
    setViewerStatus(video.muted ? '直播中，点击开启声音' : '直播中，声音已开启', true)
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      return
    }

    viewerCanEnableSound.value = true
    setViewerStatus('已收到视频，点击开启声音后继续播放', true)
  }
}

function resetViewerPeer(clearVideo = true) {
  viewerConnectionToken += 1
  viewerPc?.close()
  viewerPc = null
  viewerPending = []
  viewerRemoteStream?.getTracks().forEach((track) => track.stop())
  viewerRemoteStream = null

  if (clearVideo && viewerVideo.value) {
    viewerVideo.value.srcObject = null
    viewerVideo.value.muted = true
  }

  viewerCanEnableSound.value = false
  viewerLive.value = false
}

function disconnectViewer(status = '已断开') {
  viewerWs?.close()
  viewerWs = null
  resetViewerPeer()

  if (danmakuLayer.value) {
    danmakuLayer.value.innerHTML = ''
  }

  setViewerStatus(status, false)
}

function enableSound() {
  if (viewerVideo.value) {
    viewerVideo.value.muted = false
    void playViewerVideo()
  }
}

function showDanmaku(text: string, color = '#ffffff') {
  const layer = danmakuLayer.value
  if (!layer) {
    return
  }

  const item = document.createElement('div')
  item.className = 'danmaku-item'
  item.textContent = text.slice(0, 50)
  item.style.color = color
  item.style.top = `${8 + Math.random() * 74}%`
  item.style.animationDuration = `${8 + Math.random() * 6}s`

  layer.appendChild(item)
  item.addEventListener('animationend', () => item.remove())
}

function sendDanmaku() {
  const text = danmakuText.value.trim()
  if (!text || !viewerWs || viewerWs.readyState !== WebSocket.OPEN) {
    return
  }

  const color = danmakuColors[Math.floor(Math.random() * danmakuColors.length)] ?? '#ffffff'
  viewerWs.send(JSON.stringify({ type: 'danmaku', text, color }))
  danmakuText.value = ''
}

watch(route, async (next, previous) => {
  if (previous === 'publisher' && next !== 'publisher') {
    stopPublishing('已离开主播端')
  }

  if (previous === 'viewer' && next !== 'viewer') {
    disconnectViewer('已离开观看端')
  }

  if (next === 'viewer') {
    await nextTick()
    connectViewer()
  }
})

onMounted(() => {
  window.addEventListener('hashchange', syncRoute)
  if (!window.location.hash) {
    window.location.hash = '/'
  }

  if (route.value === 'viewer') {
    connectViewer()
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('hashchange', syncRoute)
  stopPublishing()
  disconnectViewer()
})
</script>

<template>
  <div class="app-shell">
    <header class="topbar">
      <button class="brand" type="button" @click="go('home')" aria-label="返回首页">
        <span class="brand-mark icon-play"></span>
        <span>Live Video</span>
      </button>
      <div class="endpoint">{{ wsUrl }}</div>
    </header>

    <main v-if="route === 'home'" class="home-view">
      <section class="role-picker">
        <p class="eyebrow">WebRTC Live Room</p>
        <h1>实时直播间</h1>
        <p class="summary">浏览器摄像头推流、低延迟观看、弹幕互动。</p>

        <div class="role-grid">
          <button class="role-card publisher-card" type="button" @click="go('publisher')">
            <span class="role-icon icon-broadcast"></span>
            <span class="role-title">主播推流</span>
            <span class="role-meta">摄像头 / 麦克风</span>
          </button>

          <button class="role-card viewer-card" type="button" @click="go('viewer')">
            <span class="role-icon icon-screen"></span>
            <span class="role-title">观众观看</span>
            <span class="role-meta">视频 / 弹幕</span>
          </button>
        </div>
      </section>
    </main>

    <main v-else-if="route === 'publisher'" class="work-view">
      <section class="room-header">
        <div class="room-title">
          <button class="icon-button icon-arrow-left" type="button" @click="go('home')" aria-label="返回首页"></button>
          <div>
            <p>Publisher</p>
            <h1>主播推流</h1>
          </div>
        </div>
        <span class="live-badge" :class="{ active: publisherLive }">
          <span class="pulse-dot"></span>
          {{ publisherLive ? '直播中' : '未开播' }}
        </span>
      </section>

      <section class="studio-layout">
        <div class="video-panel">
          <div class="panel-header">
            <span>画面预览</span>
            <span>{{ publisherTip }}</span>
          </div>
          <div class="video-stage">
            <video ref="publisherVideo" autoplay muted playsinline></video>
            <div v-if="!publisherStream" class="empty-video">
              <span class="empty-icon icon-camera"></span>
              <span>摄像头未开启</span>
            </div>
          </div>
          <div class="control-row">
            <button class="primary-button red" type="button" :disabled="publisherLive" @click="startPublishing">
              <span class="button-icon icon-record"></span>
              开始推流
            </button>
            <button class="secondary-button" type="button" :disabled="!publisherLive" @click="stopPublishing()">
              <span class="button-icon icon-stop"></span>
              停止推流
            </button>
            <button class="secondary-button blue" type="button" @click="go('viewer')">
              <span class="button-icon icon-open"></span>
              打开观看端
            </button>
          </div>
        </div>

        <aside class="side-metrics">
          <div>
            <span class="metric-label">观看连接</span>
            <strong>{{ publisherViewerCount }}</strong>
          </div>
          <div>
            <span class="metric-label">信令地址</span>
            <code>{{ wsUrl }}</code>
          </div>
        </aside>
      </section>
    </main>

    <main v-else class="work-view">
      <section class="room-header">
        <div class="room-title">
          <button class="icon-button icon-arrow-left" type="button" @click="go('home')" aria-label="返回首页"></button>
          <div>
            <p>Viewer</p>
            <h1>观众观看</h1>
          </div>
        </div>
        <span class="live-badge viewer" :class="{ active: viewerLive }">
          <span class="pulse-dot"></span>
          {{ viewerLive ? 'LIVE' : 'WAITING' }}
        </span>
      </section>

      <section class="watch-layout">
        <div class="player-panel">
          <div class="video-stage">
            <video ref="viewerVideo" autoplay playsinline muted></video>
            <div ref="danmakuLayer" class="danmaku-layer"></div>
            <div v-if="!viewerLive" class="empty-video">
              <span class="empty-icon icon-screen"></span>
              <span>{{ viewerTip }}</span>
            </div>
          </div>

          <div class="danmaku-bar">
            <input
              v-model="danmakuText"
              type="text"
              maxlength="50"
              placeholder="发一条弹幕"
              @keydown.enter="sendDanmaku"
            />
            <button class="primary-button blue compact" type="button" @click="sendDanmaku">
              <span class="button-icon icon-send"></span>
              发送
            </button>
          </div>

          <div class="control-row">
            <button class="secondary-button blue" type="button" @click="connectViewer">
              <span class="button-icon icon-connect"></span>
              连接直播
            </button>
            <button v-if="viewerCanEnableSound" class="secondary-button green" type="button" @click="enableSound">
              <span class="button-icon icon-volume"></span>
              开启声音
            </button>
            <button class="secondary-button" type="button" @click="disconnectViewer()">
              <span class="button-icon icon-stop"></span>
              断开连接
            </button>
          </div>
        </div>

        <aside class="activity-panel">
          <div class="status-line">
            <span class="status-dot" :class="{ active: viewerLive }"></span>
            <span>{{ viewerTip }}</span>
          </div>
          <div class="room-stat">
            <span>观众 ID</span>
            <code>{{ viewerId }}</code>
          </div>
          <div class="room-stat">
            <span>信令地址</span>
            <code>{{ wsUrl }}</code>
          </div>
        </aside>
      </section>
    </main>
  </div>
</template>

<style scoped>
.app-shell {
  min-height: 100vh;
  color: #f4f7fb;
  background:
    linear-gradient(180deg, rgba(28, 28, 36, 0.98) 0%, rgba(13, 13, 16, 0.96) 36%, #0d0d10 100%),
    #0d0d10;
}

.topbar {
  height: 58px;
  padding: 0 28px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(16, 16, 21, 0.86);
  backdrop-filter: blur(14px);
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  border: 0;
  padding: 0;
  background: transparent;
  color: #ffffff;
  font-size: 17px;
  font-weight: 700;
  cursor: pointer;
}

.brand-mark,
.role-icon,
.empty-icon {
  display: inline-grid;
  place-items: center;
  flex: 0 0 auto;
}

.brand-mark {
  width: 30px;
  height: 30px;
  border-radius: 8px;
  background: linear-gradient(135deg, #ff4d6d, #00aeec);
}

.endpoint {
  max-width: min(44vw, 560px);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: #7f8796;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 12px;
}

.home-view {
  min-height: calc(100vh - 58px);
  display: grid;
  place-items: center;
  padding: 36px 20px 52px;
}

.role-picker {
  width: min(760px, 100%);
  text-align: center;
}

.eyebrow {
  margin-bottom: 12px;
  color: #00aeec;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0;
  text-transform: uppercase;
}

.role-picker h1 {
  margin: 0;
  color: #ffffff;
  font-size: 44px;
  font-weight: 800;
  line-height: 1.12;
}

.summary {
  margin: 14px 0 36px;
  color: #9aa2b2;
  font-size: 15px;
}

.role-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.role-card {
  min-height: 190px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  border: 1px solid rgba(255, 255, 255, 0.09);
  border-radius: 8px;
  background: rgba(22, 22, 29, 0.9);
  color: #ffffff;
  cursor: pointer;
  transition:
    transform 0.18s ease,
    border-color 0.18s ease,
    background 0.18s ease;
}

.role-card:hover {
  transform: translateY(-3px);
  border-color: rgba(255, 255, 255, 0.22);
  background: rgba(31, 31, 40, 0.95);
}

.role-icon {
  width: 54px;
  height: 54px;
  border-radius: 8px;
}

.publisher-card .role-icon {
  color: #ff4d6d;
  background: rgba(255, 77, 109, 0.15);
}

.viewer-card .role-icon {
  color: #00aeec;
  background: rgba(0, 174, 236, 0.15);
}

.role-title {
  font-size: 19px;
  font-weight: 700;
}

.role-meta {
  color: #7f8796;
  font-size: 13px;
}

.work-view {
  width: min(1180px, 100%);
  margin: 0 auto;
  padding: 26px 24px 42px;
}

.room-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
}

.room-title {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.room-title p {
  margin: 0 0 2px;
  color: #7f8796;
  font-size: 12px;
  font-weight: 700;
}

.room-title h1 {
  margin: 0;
  color: #ffffff;
  font-size: 23px;
  font-weight: 750;
  line-height: 1.2;
}

.icon-button {
  width: 36px;
  height: 36px;
  display: inline-grid;
  place-items: center;
  border: 1px solid rgba(255, 255, 255, 0.09);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.06);
  color: #dbe2ee;
  cursor: pointer;
}

.live-badge {
  min-width: 94px;
  height: 30px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 7px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.07);
  color: #8f98a8;
  font-size: 12px;
  font-weight: 800;
}

.live-badge.active {
  background: rgba(255, 77, 109, 0.16);
  color: #ff6b86;
}

.live-badge.viewer.active {
  background: rgba(34, 197, 94, 0.16);
  color: #38d39f;
}

.pulse-dot,
.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: currentColor;
}

.live-badge.active .pulse-dot,
.status-dot.active {
  box-shadow: 0 0 12px currentColor;
}

.studio-layout,
.watch-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 280px;
  gap: 18px;
  align-items: start;
}

.video-panel,
.player-panel,
.side-metrics,
.activity-panel {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  background: rgba(20, 20, 26, 0.92);
  overflow: hidden;
}

.panel-header {
  min-height: 46px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 0 16px;
  color: #87909f;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  font-size: 13px;
}

.panel-header span:first-child {
  color: #ffffff;
  font-weight: 700;
}

.video-stage {
  position: relative;
  aspect-ratio: 16 / 9;
  min-height: 260px;
  background: #000000;
  overflow: hidden;
}

video {
  width: 100%;
  height: 100%;
  display: block;
  object-fit: contain;
  background: #000000;
}

.empty-video {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 20px;
  color: #626b7b;
  font-size: 14px;
  pointer-events: none;
}

.empty-icon {
  width: 58px;
  height: 58px;
  color: #4c5564;
}

.control-row {
  min-height: 68px;
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  padding: 14px 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.primary-button,
.secondary-button {
  min-height: 40px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border-radius: 8px;
  padding: 0 18px;
  color: #ffffff;
  border: 0;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  transition:
    opacity 0.16s ease,
    transform 0.16s ease;
}

.primary-button:hover,
.secondary-button:hover {
  transform: translateY(-1px);
}

.primary-button:disabled,
.secondary-button:disabled {
  opacity: 0.38;
  cursor: not-allowed;
  transform: none;
}

.primary-button.red {
  background: linear-gradient(135deg, #ff4d6d, #e83857);
  box-shadow: 0 10px 24px rgba(255, 77, 109, 0.22);
}

.primary-button.blue,
.secondary-button.blue {
  color: #d9f5ff;
  border: 1px solid rgba(0, 174, 236, 0.28);
  background: rgba(0, 174, 236, 0.15);
}

.primary-button.blue {
  background: linear-gradient(135deg, #00aeec, #008fc7);
  color: #ffffff;
}

.secondary-button {
  color: #aeb7c6;
  border: 1px solid rgba(255, 255, 255, 0.09);
  background: rgba(255, 255, 255, 0.07);
}

.secondary-button.green {
  color: #bff5d9;
  border-color: rgba(34, 197, 94, 0.3);
  background: rgba(34, 197, 94, 0.15);
}

.compact {
  min-width: 86px;
  flex: 0 0 auto;
}

.button-icon,
.icon-button::before,
.brand-mark::before,
.role-icon::before,
.empty-icon::before {
  display: inline-block;
  width: 1.1em;
  text-align: center;
  font-size: 18px;
  line-height: 1;
}

.icon-play::before {
  content: '▶';
  font-size: 15px;
}

.icon-broadcast::before {
  content: '◉';
  font-size: 26px;
}

.icon-screen::before {
  content: '▭';
  font-size: 28px;
}

.icon-camera::before {
  content: '▣';
  font-size: 30px;
}

.icon-record::before {
  content: '●';
}

.icon-stop::before {
  content: '■';
}

.icon-open::before {
  content: '↗';
}

.icon-send::before {
  content: '➜';
}

.icon-connect::before {
  content: '⌁';
}

.icon-volume::before {
  content: '▸';
}

.icon-arrow-left::before {
  content: '‹';
  font-size: 28px;
}

.side-metrics,
.activity-panel {
  display: grid;
  gap: 14px;
  padding: 16px;
}

.side-metrics > div,
.room-stat {
  display: grid;
  gap: 6px;
}

.metric-label,
.room-stat span {
  color: #778191;
  font-size: 12px;
  font-weight: 700;
}

.side-metrics strong {
  color: #ffffff;
  font-size: 32px;
  font-weight: 800;
}

code {
  max-width: 100%;
  overflow-wrap: anywhere;
  color: #d9f5ff;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 12px;
}

.danmaku-layer {
  position: absolute;
  inset: 0;
  z-index: 2;
  overflow: hidden;
  pointer-events: none;
}

:deep(.danmaku-item) {
  position: absolute;
  right: 0;
  z-index: 3;
  white-space: nowrap;
  font-size: 22px;
  font-weight: 800;
  text-shadow:
    1px 1px 3px rgba(0, 0, 0, 0.9),
    -1px -1px 3px rgba(0, 0, 0, 0.9);
  animation: danmaku-move linear forwards;
}

@keyframes danmaku-move {
  from {
    transform: translateX(0);
  }
  to {
    transform: translateX(calc(-100% - 1200px));
  }
}

.danmaku-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 13px 16px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  background: rgba(0, 0, 0, 0.22);
}

.danmaku-bar input {
  min-width: 0;
  flex: 1;
  height: 40px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 999px;
  padding: 0 15px;
  color: #ffffff;
  background: rgba(255, 255, 255, 0.07);
  font: inherit;
}

.danmaku-bar input::placeholder {
  color: #687284;
}

.danmaku-bar input:focus {
  outline: none;
  border-color: rgba(0, 174, 236, 0.55);
  background: rgba(255, 255, 255, 0.1);
}

.status-line {
  display: flex;
  align-items: center;
  gap: 9px;
  color: #c6ceda;
  font-size: 14px;
}

.status-dot {
  color: #626b7b;
}

.status-dot.active {
  color: #38d39f;
}

@media (max-width: 900px) {
  .studio-layout,
  .watch-layout {
    grid-template-columns: 1fr;
  }

  .topbar {
    padding: 0 18px;
  }

  .endpoint {
    display: none;
  }
}

@media (max-width: 640px) {
  .role-picker h1 {
    font-size: 34px;
  }

  .role-grid {
    grid-template-columns: 1fr;
  }

  .work-view {
    padding: 18px 14px 32px;
  }

  .room-header {
    align-items: flex-start;
  }

  .video-stage {
    min-height: 190px;
  }

  .primary-button,
  .secondary-button {
    flex: 1 1 150px;
  }
}
</style>
