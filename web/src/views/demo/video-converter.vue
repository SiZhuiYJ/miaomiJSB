<template>
  <div class="video-converter-page">
    <el-card>
      <template #header>
        <div class="header">
          <span>纯前端视频转 WebM + 封面提取</span>
          <el-tag :type="canConvert ? 'success' : 'danger'" effect="dark">
            {{ canConvert ? '当前浏览器支持转码' : '当前浏览器不支持转码' }}
          </el-tag>
        </div>
      </template>

      <el-upload
        drag
        :auto-upload="false"
        accept="video/*"
        :show-file-list="false"
        :on-change="handleFileChange"
      >
        <el-icon class="el-icon--upload"><UploadFilled /></el-icon>
        <div class="el-upload__text">拖拽视频到这里或 <em>点击选择</em></div>
      </el-upload>

      <div v-if="sourceFile" class="result-card">
        <el-descriptions :column="1" border>
          <el-descriptions-item label="原始文件">{{ sourceFile.name }}</el-descriptions-item>
          <el-descriptions-item label="原始大小">{{ formatFileSize(sourceFile.size) }}</el-descriptions-item>
          <el-descriptions-item label="原始时长" v-if="sourceDuration">{{ formatDuration(sourceDuration) }}</el-descriptions-item>
          <el-descriptions-item label="转换后文件" v-if="convertedFile">{{ convertedFile.name }}</el-descriptions-item>
          <el-descriptions-item label="转换后大小" v-if="convertedFile">{{ formatFileSize(convertedFile.size) }}</el-descriptions-item>
          <el-descriptions-item label="转换后时长" v-if="convertedFile">{{ formatDuration(convertedDuration) }}</el-descriptions-item>
          <el-descriptions-item label="压缩比" v-if="convertedFile && sourceFile.size > 0">
            {{ Math.round((convertedFile.size / sourceFile.size) * 100) }}%
          </el-descriptions-item>
        </el-descriptions>

        <div class="actions">
          <el-button type="primary" :loading="converting" @click="startConvert" :disabled="!sourceFile || !canConvert">
            转换为 WebM
          </el-button>
          <el-button :disabled="!convertedFile" @click="downloadConverted">下载 WebM</el-button>
          <el-button :disabled="!coverFile" @click="downloadCover">下载封面 WebP</el-button>
          <el-switch
            v-model="syncProgress"
            :disabled="!sourcePreviewUrl || !convertedPreviewUrl"
            active-text="原视频主控同步"
            inactive-text="独立播放"
          />
        </div>

        <el-progress v-if="converting" :percentage="progress" />

        <div class="preview-block" v-if="sourcePreviewUrl">
          <h4>原视频预览</h4>
          <video
            ref="sourceVideoRef"
            :src="sourcePreviewUrl"
            controls
            preload="metadata"
            @timeupdate="handleSourceTimeUpdate"
            @seeked="handleSourceSeeked"
            @play="handleSourcePlay"
            @pause="handleSourcePause"
          />
        </div>

        <div class="preview-block" v-if="convertedPreviewUrl">
          <h4>转换后视频预览（WebM）</h4>
          <p v-if="syncProgress" class="sync-tip">同步开启：请操作上方原视频播放器</p>
          <video
            ref="convertedVideoRef"
            :src="convertedPreviewUrl"
            controls
            preload="metadata"
            @timeupdate="handleConvertedTimeUpdate"
            @seeked="handleConvertedSeeked"
            @play="handleConvertedPlay"
            @pause="handleConvertedPause"
          />
        </div>

        <div v-if="coverPreviewUrl" class="preview-block">
          <h4>封面预览（WebP）</h4>
          <img :src="coverPreviewUrl" alt="cover preview" />
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import type { UploadFile } from "element-plus";
import { UploadFilled } from "@element-plus/icons-vue";
import { onBeforeUnmount, ref, watch } from "vue";
import { extractVideoFrameToWebP } from "@/utils/convertToWebP";
import { canConvertVideoToWebM, convertVideoToWebM } from "@/utils/videoConverter";
import { getVideoDuration } from "@/utils/mediaDuration";
import { formatFileSize } from "@/features/chat/utils/fileHelper";

const canConvert = canConvertVideoToWebM();
const sourceFile = ref<File | null>(null);
const convertedFile = ref<File | null>(null);
const coverFile = ref<File | null>(null);
const sourcePreviewUrl = ref("");
const convertedPreviewUrl = ref("");
const coverPreviewUrl = ref("");
const sourceDuration = ref<number | null>(null);
const convertedDuration = ref<number | null>(null);
const sourceVideoRef = ref<HTMLVideoElement | null>(null);
const convertedVideoRef = ref<HTMLVideoElement | null>(null);
const syncProgress = ref(true);
let syncingFrom: "source" | "converted" | null = null;
const converting = ref(false);
const progress = ref(0);
const MAX_OUTPUT_SIZE_BYTES = 100 * 1024 * 1024;

function releaseSourcePreview() {
  if (sourcePreviewUrl.value) {
    URL.revokeObjectURL(sourcePreviewUrl.value);
    sourcePreviewUrl.value = "";
  }
}

function releaseConvertedPreview() {
  if (convertedPreviewUrl.value) {
    URL.revokeObjectURL(convertedPreviewUrl.value);
    convertedPreviewUrl.value = "";
  }
}

function releasePreview() {
  if (coverPreviewUrl.value) {
    URL.revokeObjectURL(coverPreviewUrl.value);
    coverPreviewUrl.value = "";
  }
}

function handleFileChange(uploadFile: UploadFile) {
  const raw = uploadFile.raw;
  if (!raw) return;

  sourceFile.value = raw;
  convertedFile.value = null;
  coverFile.value = null;
  progress.value = 0;
  sourceDuration.value = null;
  convertedDuration.value = null;
  syncingFrom = null;
  releaseSourcePreview();
  releaseConvertedPreview();
  releasePreview();
  sourcePreviewUrl.value = URL.createObjectURL(raw);
  void getVideoDuration(raw).then((duration) => {
    sourceDuration.value = duration;
  });
}

async function startConvert() {
  if (!sourceFile.value) return;

  converting.value = true;
  progress.value = 0;
  releasePreview();

  try {
    const [webm, cover] = await Promise.all([
      convertVideoToWebM(sourceFile.value, {
        maxOutputSizeBytes: MAX_OUTPUT_SIZE_BYTES,
        onProgress: (value) => {
          progress.value = value;
        },
      }),
      extractVideoFrameToWebP(sourceFile.value, {
        quality: 0.95,
        fileName: `${sourceFile.value.name.replace(/\.[^.]+$/, "") || "video"}-cover.webp`,
      }),
    ]);

    convertedFile.value = webm;
    coverFile.value = cover;
    releaseConvertedPreview();
    convertedPreviewUrl.value = URL.createObjectURL(webm);
    coverPreviewUrl.value = URL.createObjectURL(cover);
    convertedDuration.value = await getVideoDuration(webm);
    ElMessage.success(convertedDuration.value ? "转换完成，可用于 chat 上传前处理" : "转换完成：该 WebM 可能缺少时长元数据");
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : "转换失败");
  } finally {
    converting.value = false;
  }
}

function downloadBlob(file: File) {
  const url = URL.createObjectURL(file);
  const a = document.createElement("a");
  a.href = url;
  a.download = file.name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

function downloadConverted() {
  if (!convertedFile.value) return;
  downloadBlob(convertedFile.value);
}

function downloadCover() {
  if (!coverFile.value) return;
  downloadBlob(coverFile.value);
}

function syncCurrentTime(from: "source" | "converted") {
  if (!syncProgress.value) return;

  const source = sourceVideoRef.value;
  const converted = convertedVideoRef.value;
  if (!source || !converted) return;

  const active = from === "source" ? source : converted;
  const passive = from === "source" ? converted : source;
  if (!Number.isFinite(active.currentTime)) return;

  const next = Math.max(0, Math.min(active.currentTime, Number.isFinite(passive.duration) ? passive.duration : active.currentTime));
  if (Math.abs(passive.currentTime - next) < 0.2) return;

  syncingFrom = from;
  passive.currentTime = next;
  window.setTimeout(() => {
    syncingFrom = null;
  }, 0);
}

function syncPlayState(from: "source" | "converted", action: "play" | "pause") {
  if (!syncProgress.value) return;

  const source = sourceVideoRef.value;
  const converted = convertedVideoRef.value;
  if (!source || !converted) return;

  const passive = from === "source" ? converted : source;
  if (action === "play" && passive.paused) {
    void passive.play().catch(() => undefined);
  }
  if (action === "pause" && !passive.paused) {
    passive.pause();
  }
}

function handleSourceTimeUpdate() {
  if (syncingFrom === "converted") return;
  syncCurrentTime("source");
}

function handleConvertedTimeUpdate() {
  if (syncProgress.value) return;
  if (syncingFrom === "source") return;
  syncCurrentTime("converted");
}

function handleSourceSeeked() {
  if (syncingFrom === "converted") return;
  syncCurrentTime("source");
}

function handleConvertedSeeked() {
  if (syncProgress.value) return;
  if (syncingFrom === "source") return;
  syncCurrentTime("converted");
}

function handleSourcePlay() {
  syncPlayState("source", "play");
}

function handleConvertedPlay() {
  if (syncProgress.value) {
    const source = sourceVideoRef.value;
    if (source?.paused) {
      convertedVideoRef.value?.pause();
    } else {
      syncCurrentTime("source");
    }
    return;
  }
  syncPlayState("converted", "play");
}

function handleSourcePause() {
  syncPlayState("source", "pause");
}

function handleConvertedPause() {
  if (syncProgress.value) {
    const source = sourceVideoRef.value;
    if (!source?.paused) {
      void convertedVideoRef.value?.play().catch(() => undefined);
    } else {
      syncCurrentTime("source");
    }
    return;
  }
  syncPlayState("converted", "pause");
}

function formatDuration(value: number | null): string {
  if (!value || !Number.isFinite(value)) return "无法识别";
  const total = Math.max(1, Math.round(value));
  const minutes = Math.floor(total / 60)
    .toString()
    .padStart(2, "0");
  const seconds = (total % 60).toString().padStart(2, "0");
  return `${minutes}:${seconds}`;
}

watch(syncProgress, (enabled) => {
  if (!enabled) return;

  const source = sourceVideoRef.value;
  const converted = convertedVideoRef.value;
  if (!source || !converted) return;

  syncCurrentTime("source");
  if (source.paused && !converted.paused) {
    converted.pause();
  }
  if (!source.paused && converted.paused) {
    void converted.play().catch(() => undefined);
  }
});

onBeforeUnmount(() => {
  releaseSourcePreview();
  releaseConvertedPreview();
  releasePreview();
});
</script>

<style scoped lang="scss">
.video-converter-page {
  padding: 20px;
}

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.result-card {
  margin-top: 16px;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin: 16px 0;
}

.preview-block {
  margin-top: 12px;

  video {
    width: 420px;
    max-width: 100%;
    border-radius: 8px;
    border: 1px solid var(--el-border-color);
    background: #000;
  }

  .sync-tip {
    margin: 6px 0 10px;
    color: var(--el-text-color-secondary);
    font-size: 12px;
  }

  img {
    width: 240px;
    max-width: 100%;
    border-radius: 8px;
    border: 1px solid var(--el-border-color);
  }
}
</style>
