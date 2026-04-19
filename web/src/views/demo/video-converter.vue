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
          <el-descriptions-item label="转换后文件" v-if="convertedFile">{{ convertedFile.name }}</el-descriptions-item>
          <el-descriptions-item label="转换后大小" v-if="convertedFile">{{ formatFileSize(convertedFile.size) }}</el-descriptions-item>
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
        </div>

        <el-progress v-if="converting" :percentage="progress" />

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
import { onBeforeUnmount, ref } from "vue";
import { extractVideoFrameToWebP } from "@/utils/convertToWebP";
import { canConvertVideoToWebM, convertVideoToWebM } from "@/utils/videoConverter";
import { formatFileSize } from "@/features/chat/utils/fileHelper";

const canConvert = canConvertVideoToWebM();
const sourceFile = ref<File | null>(null);
const convertedFile = ref<File | null>(null);
const coverFile = ref<File | null>(null);
const coverPreviewUrl = ref("");
const converting = ref(false);
const progress = ref(0);

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
  releasePreview();
}

async function startConvert() {
  if (!sourceFile.value) return;

  converting.value = true;
  progress.value = 0;
  releasePreview();

  try {
    const [webm, cover] = await Promise.all([
      convertVideoToWebM(sourceFile.value, {
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
    coverPreviewUrl.value = URL.createObjectURL(cover);
    ElMessage.success("转换完成，可用于 chat 上传前处理");
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

onBeforeUnmount(() => {
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

  img {
    width: 240px;
    max-width: 100%;
    border-radius: 8px;
    border: 1px solid var(--el-border-color);
  }
}
</style>
