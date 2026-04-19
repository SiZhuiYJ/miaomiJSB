export interface ConvertVideoToWebMOptions {
  videoBitsPerSecond?: number;
  audioBitsPerSecond?: number;
  maxOutputSizeBytes?: number;
  mimeType?: string;
  maxDurationSeconds?: number;
  onProgress?: (progress: number) => void;
}

const DEFAULT_MAX_DURATION_SECONDS = 5 * 60;
const MIME_CANDIDATES = [
  "video/webm;codecs=vp9,opus",
  "video/webm;codecs=vp8,opus",
  "video/webm;codecs=vp8",
  "video/webm",
];

function pickSupportedMimeType(preferred?: string): string {
  const candidates = preferred ? [preferred, ...MIME_CANDIDATES] : MIME_CANDIDATES;
  const fallback = "video/webm";

  if (typeof MediaRecorder === "undefined") {
    return fallback;
  }

  for (const mimeType of candidates) {
    if (MediaRecorder.isTypeSupported(mimeType)) {
      return mimeType;
    }
  }

  return fallback;
}

function normalizeWebMName(fileName: string): string {
  const safeName = fileName.trim() || "video";
  return /\.[^.]+$/.test(safeName)
    ? safeName.replace(/\.[^.]+$/, ".webm")
    : `${safeName}.webm`;
}

function waitForMediaEvent(
  element: HTMLMediaElement,
  eventName: keyof HTMLMediaElementEventMap,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const handleSuccess = () => {
      cleanup();
      resolve();
    };

    const handleFailure = () => {
      cleanup();
      reject(new Error("视频加载失败，无法完成转码"));
    };

    const cleanup = () => {
      element.removeEventListener(eventName, handleSuccess);
      element.removeEventListener("error", handleFailure);
    };

    element.addEventListener(eventName, handleSuccess, { once: true });
    element.addEventListener("error", handleFailure, { once: true });
  });
}

function createVideoCaptureStream(video: HTMLVideoElement): MediaStream | null {
  const source = video as HTMLVideoElement & {
    captureStream?: () => MediaStream;
    mozCaptureStream?: () => MediaStream;
  };
  if (typeof source.captureStream === "function") {
    return source.captureStream();
  }
  if (typeof source.mozCaptureStream === "function") {
    return source.mozCaptureStream();
  }
  return null;
}

function createMixedStream(
  video: HTMLVideoElement,
  canvas: HTMLCanvasElement,
): { stream: MediaStream; useCanvasDraw: boolean } {
  const directStream = createVideoCaptureStream(video);
  if (directStream && directStream.getVideoTracks().length > 0) {
    return { stream: directStream, useCanvasDraw: false };
  }

  const stream = canvas.captureStream();

  const AudioContextImpl = window.AudioContext || (window as any).webkitAudioContext;
  if (!AudioContextImpl) return { stream, useCanvasDraw: true };

  const audioContext = new AudioContextImpl();
  const destination = audioContext.createMediaStreamDestination();
  const source = audioContext.createMediaElementSource(video);
  source.connect(destination);
  source.connect(audioContext.destination);

  destination.stream.getAudioTracks().forEach((track) => stream.addTrack(track));

  const teardown = () => {
    source.disconnect();
    destination.disconnect();
    audioContext.close().catch(() => undefined);
  };

  video.addEventListener("ended", teardown, { once: true });
  video.addEventListener("pause", teardown, { once: true });

  return { stream, useCanvasDraw: true };
}

function estimateSourceBitrate(
  sourceFile: File,
  durationSeconds: number,
  maxOutputSizeBytes?: number,
): { videoBitsPerSecond?: number; audioBitsPerSecond?: number } {
  if (!Number.isFinite(durationSeconds) || durationSeconds <= 0) {
    return {};
  }

  const sourceTotalBitsPerSecond = Math.floor((sourceFile.size * 8) / durationSeconds);
  const maxOutputBitsPerSecond = maxOutputSizeBytes
    ? Math.floor((maxOutputSizeBytes * 8) / durationSeconds)
    : undefined;
  const totalBitsPerSecond = typeof maxOutputBitsPerSecond === "number"
    ? Math.min(sourceTotalBitsPerSecond, maxOutputBitsPerSecond)
    : sourceTotalBitsPerSecond;
  if (!Number.isFinite(totalBitsPerSecond) || totalBitsPerSecond <= 0) {
    return {};
  }

  const audioBitsPerSecond = Math.min(192_000, Math.max(96_000, Math.floor(totalBitsPerSecond * 0.12)));
  const videoBitsPerSecond = Math.max(300_000, Math.floor(totalBitsPerSecond - audioBitsPerSecond));

  return {
    videoBitsPerSecond,
    audioBitsPerSecond,
  };
}

export function canConvertVideoToWebM(): boolean {
  if (typeof window === "undefined") return false;
  if (typeof MediaRecorder === "undefined") return false;
  return MIME_CANDIDATES.some((mime) => MediaRecorder.isTypeSupported(mime));
}

export async function convertVideoToWebM(
  sourceFile: File,
  options: ConvertVideoToWebMOptions = {},
): Promise<File> {
  if (sourceFile.type === "video/webm" && sourceFile.name.toLowerCase().endsWith(".webm")) {
    return sourceFile;
  }

  if (!canConvertVideoToWebM()) {
    throw new Error("当前浏览器不支持前端 WebM 转码，请更换 Chromium/Edge 最新版后重试");
  }

  const maxDuration = options.maxDurationSeconds ?? DEFAULT_MAX_DURATION_SECONDS;
  const sourceUrl = URL.createObjectURL(sourceFile);
  const video = document.createElement("video");
  video.src = sourceUrl;
  video.preload = "auto";
  video.muted = false;
  video.playsInline = true;
  video.crossOrigin = "anonymous";

  await waitForMediaEvent(video, "loadedmetadata");

  if (!Number.isFinite(video.duration) || video.duration <= 0) {
    URL.revokeObjectURL(sourceUrl);
    throw new Error("无法读取视频时长，无法转换为 WebM");
  }

  if (video.duration > maxDuration) {
    URL.revokeObjectURL(sourceUrl);
    throw new Error(`视频时长超过 ${maxDuration} 秒，建议先剪辑后再上传`);
  }

  const width = video.videoWidth || 640;
  const height = video.videoHeight || 360;
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext("2d");
  if (!ctx) {
    URL.revokeObjectURL(sourceUrl);
    throw new Error("浏览器不支持 Canvas 2D，无法转换视频");
  }

  const { stream: mixedStream, useCanvasDraw } = createMixedStream(video, canvas);
  const mimeType = pickSupportedMimeType(options.mimeType);
  const guessedBitrate = estimateSourceBitrate(sourceFile, video.duration, options.maxOutputSizeBytes);
  const videoBitsPerSecond = options.videoBitsPerSecond ?? guessedBitrate.videoBitsPerSecond;
  const audioBitsPerSecond = options.audioBitsPerSecond ?? guessedBitrate.audioBitsPerSecond;

  const chunks: BlobPart[] = [];
  const recorderOptions: MediaRecorderOptions = { mimeType };
  if (typeof videoBitsPerSecond === "number") {
    recorderOptions.videoBitsPerSecond = videoBitsPerSecond;
  }
  if (typeof audioBitsPerSecond === "number") {
    recorderOptions.audioBitsPerSecond = audioBitsPerSecond;
  }
  const recorder = new MediaRecorder(mixedStream, recorderOptions);

  let rafId = 0;
  let videoFrameCallbackId = 0;
  let progressTimer = 0;

  const drawFrameByRaf = () => {
    if (video.paused || video.ended) return;
    ctx.drawImage(video, 0, 0, width, height);
    rafId = requestAnimationFrame(drawFrameByRaf);
  };

  const drawFrameByVideoFrameCallback = () => {
    const callbackApi = video as HTMLVideoElement & {
      requestVideoFrameCallback?: (callback: () => void) => number;
    };
    if (typeof callbackApi.requestVideoFrameCallback !== "function") {
      drawFrameByRaf();
      return;
    }

    const draw = () => {
      if (video.paused || video.ended) return;
      ctx.drawImage(video, 0, 0, width, height);
      videoFrameCallbackId = callbackApi.requestVideoFrameCallback!(draw);
    };

    videoFrameCallbackId = callbackApi.requestVideoFrameCallback(draw);
  };

  try {
    const outputBlob = await new Promise<Blob>((resolve, reject) => {
      recorder.ondataavailable = (event) => {
        if (event.data && event.data.size > 0) {
          chunks.push(event.data);
        }
      };

      recorder.onerror = () => {
        reject(new Error("视频编码失败，请更换源视频或浏览器后重试"));
      };

      recorder.onstop = () => {
        if (chunks.length === 0) {
          reject(new Error("未生成有效的 WebM 数据"));
          return;
        }
        resolve(new Blob(chunks, { type: "video/webm" }));
      };

      video.onended = () => {
        if (recorder.state !== "inactive") {
          recorder.stop();
        }
      };

      recorder.start(500);
      video
        .play()
        .then(() => {
          if (useCanvasDraw) {
            drawFrameByVideoFrameCallback();
          }
          progressTimer = window.setInterval(() => {
            if (!options.onProgress || !video.duration) return;
            const progress = Math.min(99, Math.round((video.currentTime / video.duration) * 100));
            options.onProgress(progress);
          }, 200);
        })
        .catch(() => {
          reject(new Error("视频播放失败，无法开始转码"));
        });
    });

    if (options.maxOutputSizeBytes && outputBlob.size > options.maxOutputSizeBytes) {
      throw new Error(
        `转换后文件超过 ${Math.round(options.maxOutputSizeBytes / 1024 / 1024)}MB，请缩短时长或降低分辨率后重试`,
      );
    }

    options.onProgress?.(100);

    return new File([outputBlob], normalizeWebMName(sourceFile.name), {
      type: "video/webm",
      lastModified: Date.now(),
    });
  } finally {
    if (useCanvasDraw) {
      cancelAnimationFrame(rafId);
      const cancelApi = video as HTMLVideoElement & {
        cancelVideoFrameCallback?: (handle: number) => void;
      };
      if (videoFrameCallbackId && typeof cancelApi.cancelVideoFrameCallback === "function") {
        cancelApi.cancelVideoFrameCallback(videoFrameCallbackId);
      }
    }
    if (progressTimer) {
      clearInterval(progressTimer);
    }

    mixedStream.getTracks().forEach((track) => track.stop());
    video.pause();
    video.removeAttribute("src");
    video.load();
    URL.revokeObjectURL(sourceUrl);
  }
}
