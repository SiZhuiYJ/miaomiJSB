const startButton = document.querySelector('#startButton')
const stopButton = document.querySelector('#stopButton')
const message = document.querySelector('#message')
const video = document.querySelector('#previewVideo')
const placeholder = document.querySelector('#placeholder')

let activeStream = null

const setMessage = (text) => {
  message.textContent = `ⓘ ${text}`
}

const getSupportedGetUserMedia = () => {
  if (typeof navigator === 'undefined') return undefined
  if (navigator.mediaDevices?.getUserMedia) {
    return (constraints) => navigator.mediaDevices.getUserMedia(constraints)
  }

  const legacyGetUserMedia = navigator.getUserMedia
    || navigator.webkitGetUserMedia
    || navigator.mozGetUserMedia
    || navigator.msGetUserMedia

  if (!legacyGetUserMedia) return undefined
  return (constraints) => new Promise((resolve, reject) => {
    legacyGetUserMedia.call(navigator, constraints, resolve, reject)
  })
}

const getCameraStream = async () => {
  const getUserMedia = getSupportedGetUserMedia()
  if (!getUserMedia) {
    throw new Error('当前浏览器/内嵌 WebView 不支持摄像头采集，请使用 HTTPS 页面并升级到支持 getUserMedia 的浏览器。')
  }

  return getUserMedia({
    audio: true,
    video: {
      facingMode: 'user',
      width: { ideal: 1280 },
      height: { ideal: 720 },
    },
  })
}

const startPush = async () => {
  try {
    setMessage('正在申请摄像头/麦克风权限...')
    activeStream = await getCameraStream()
    video.srcObject = activeStream
    await video.play()
    startButton.disabled = true
    stopButton.disabled = false
    placeholder.hidden = true
    setMessage('摄像头已开启，可以开始接入推流服务。')
  } catch (error) {
    setMessage(error instanceof Error ? error.message : String(error))
  }
}

const stopPush = () => {
  activeStream?.getTracks().forEach((track) => track.stop())
  activeStream = null
  video.srcObject = null
  startButton.disabled = false
  stopButton.disabled = true
  placeholder.hidden = false
  setMessage('推流已停止')
}

startButton.addEventListener('click', startPush)
stopButton.addEventListener('click', stopPush)
