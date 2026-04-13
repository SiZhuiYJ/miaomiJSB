<template>
    <div class="video-converter">
        <!-- 文件上传区域 -->
        <div
            class="upload-area"
            :class="{ 'drag-over': isDragOver, 'has-files': fileList.length > 0 }"
            @dragover.prevent="handleDragOver"
            @dragleave.prevent="handleDragLeave"
            @drop.prevent="handleDrop"
            @click="triggerFileInput"
        >
            <input
                ref="fileInputRef"
                type="file"
                accept="video/*"
                multiple
                style="display: none"
                @change="handleFileSelect"
            />

            <div v-if="fileList.length === 0" class="upload-placeholder">
                <el-icon :size="48" class="upload-icon">
                    <Upload />
                </el-icon>
                <p class="upload-text">点击或拖拽视频文件到此处</p>
                <p class="upload-hint">支持 MP4、AVI、MOV、MKV 等格式</p>
            </div>

            <div v-else class="file-list">
                <div v-for="(file, index) in fileList" :key="index" class="file-item">
                    <el-icon class="file-icon">
                        <VideoPlay />
                    </el-icon>
                    <div class="file-info">
                        <p class="file-name">{{ file.name }}</p>
                        <p class="file-size">{{ formatFileSize(file.size) }}</p>
                    </div>
                    <el-button
                        type="danger"
                        size="small"
                        text
                        @click.stop="removeFile(index)"
                        :disabled="converting"
                    >
                        <el-icon><Close /></el-icon>
                    </el-button>
                </div>

                <el-button
                    type="primary"
                    class="add-more-btn"
                    @click.stop="triggerFileInput"
                    :disabled="converting"
                >
                    <el-icon><Plus /></el-icon>
                    添加更多
                </el-button>
            </div>
        </div>

        <!-- 转换设置 -->
        <div v-if="fileList.length > 0" class="settings-panel">
            <h3 class="settings-title">转换设置</h3>

            <el-form :model="settings" label-width="100px" size="default">
                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="质量">
                            <el-slider
                                v-model="settings.quality"
                                :min="0.1"
                                :max="1"
                                :step="0.1"
                                :format-tooltip="(val: number) => `${Math.round(val * 100)}%`"
                            />
                        </el-form-item>
                    </el-col>

                    <el-col :span="12">
                        <el-form-item label="最大宽度">
                            <el-select v-model="settings.maxWidth" placeholder="保持原样" clearable>
                                <el-option label="360p" :value="640" />
                                <el-option label="480p" :value="854" />
                                <el-option label="720p" :value="1280" />
                                <el-option label="1080p" :value="1920" />
                                <el-option label="4K" :value="3840" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>

                <el-row :gutter="20">
                    <el-col :span="12">
                        <el-form-item label="编码器">
                            <el-select v-model="settings.videoCodec">
                                <el-option label="VP9 (推荐)" value="libvpx-vp9" />
                                <el-option label="VP8" value="libvpx" />
                            </el-select>
                        </el-form-item>
                    </el-col>

                    <el-col :span="12">
                        <el-form-item label="帧率">
                            <el-select v-model="settings.fps" placeholder="保持原样" clearable>
                                <el-option label="24 fps" :value="24" />
                                <el-option label="30 fps" :value="30" />
                                <el-option label="60 fps" :value="60" />
                            </el-select>
                        </el-form-item>
                    </el-col>
                </el-row>

                <el-form-item>
                    <el-checkbox v-model="settings.removeAudio">移除音频</el-checkbox>
                </el-form-item>
            </el-form>
        </div>

        <!-- 进度显示 -->
        <div v-if="converting || progress > 0" class="progress-panel">
            <div class="progress-header">
                <h3 class="progress-title">
                    {{ converting ? '正在处理...' : '转换完成' }}
                </h3>
                <span class="progress-text">{{ Math.round(progress) }}%</span>
            </div>

            <el-progress
                :percentage="Math.round(progress)"
                :status="progress >= 100 ? 'success' : (converting ? undefined : 'exception')"
                :stroke-width="8"
            />

            <div v-if="logs.length > 0" class="log-panel">
                <p v-for="(log, index) in logs.slice(-8)" :key="index" class="log-item">
                    {{ log }}
                </p>
            </div>
        </div>

        <!-- 操作按钮 -->
        <div v-if="fileList.length > 0" class="action-buttons">
            <el-button
                type="primary"
                size="large"
                :loading="converting"
                :disabled="fileList.length === 0"
                @click="handleConvert"
            >
                <el-icon v-if="!converting"><VideoCamera /></el-icon>
                {{ converting ? '转换中...' : '开始转换' }}
            </el-button>

            <el-button size="large" @click="handleClear" :disabled="converting">
                <el-icon><Delete /></el-icon>
                清空
            </el-button>
        </div>

        <!-- 结果展示 -->
        <div v-if="results.length > 0" class="results-panel">
            <h3 class="results-title">转换结果</h3>

            <div class="results-list">
                <div v-for="(result, index) in results" :key="index" class="result-item">
                    <div class="result-info">
                        <el-icon class="result-icon" :size="32">
                            <SuccessFilled />
                        </el-icon>
                        <div class="result-details">
                            <p class="result-name">{{ result.fileName }}</p>
                            <p class="result-size">{{ formatFileSize(result.size) }}</p>
                        </div>
                    </div>

                    <div class="result-actions">
                        <el-button type="primary" size="small" @click="handlePreview(result)">
                            预览
                        </el-button>
                        <el-button type="success" size="small" @click="handleDownload(result)">
                            下载
                        </el-button>
                    </div>
                </div>
            </div>
        </div>

        <!-- 预览对话框 -->
        <el-dialog
            v-model="showPreview"
            title="视频预览"
            width="80%"
            :close-on-click-modal="false"
        >
            <video
                v-if="previewUrl"
                :src="previewUrl"
                controls
                autoplay
                class="preview-video"
            />
        </el-dialog>
    </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { ElMessage } from 'element-plus';
import {
    Upload,
    VideoPlay,
    Close,
    Plus,
    VideoCamera,
    Delete,
    SuccessFilled,
} from '@element-plus/icons-vue';
import { convertToWebM, type VideoConvertOptions, type ConvertResult } from '@/utils/convertToWebM';

// 状态
const fileInputRef = ref<HTMLInputElement>();
const isDragOver = ref(false);
const fileList = ref<File[]>([]);
const converting = ref(false);
const progress = ref(0);
const logs = ref<string[]>([]);
const results = ref<ConvertResult[]>([]);
const showPreview = ref(false);
const previewUrl = ref('');

// 设置
const settings = reactive<Partial<VideoConvertOptions>>({
    quality: 0.8,
    videoCodec: 'libvpx-vp9',
    audioCodec: 'libopus',
});

// 格式化文件大小
function formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
}

// 触发文件选择
function triggerFileInput() {
    fileInputRef.value?.click();
}

// 处理文件选择
function handleFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files) {
        addFiles(Array.from(input.files));
        input.value = ''; // 重置以允许重复选择同一文件
    }
}

// 处理拖拽
function handleDragOver() {
    isDragOver.value = true;
}

function handleDragLeave() {
    isDragOver.value = false;
}

function handleDrop(event: DragEvent) {
    isDragOver.value = false;
    const files = event.dataTransfer?.files;
    if (files) {
        addFiles(Array.from(files).filter(f => f.type.startsWith('video/')));
    }
}

// 添加文件
function addFiles(files: File[]) {
    const validFiles = files.filter(file => file.type.startsWith('video/'));

    if (validFiles.length !== files.length) {
        ElMessage.warning('已过滤非视频文件');
    }

    fileList.value.push(...validFiles);
    ElMessage.success(`已添加 ${validFiles.length} 个文件`);
}

// 移除文件
function removeFile(index: number) {
    fileList.value.splice(index, 1);
}

// 清空
function handleClear() {
    fileList.value = [];
    results.value = [];
    progress.value = 0;
    logs.value = [];
}

// 转换视频
async function handleConvert() {
    if (fileList.value.length === 0) {
        ElMessage.warning('请先选择视频文件');
        return;
    }

    converting.value = true;
    progress.value = 0;
    logs.value = [];
    results.value = [];

    // 添加初始日志
    logs.value.push('准备开始转换...');
    logs.value.push('首次使用需要加载 FFmpeg 核心模块（约 30MB），请耐心等待...');

    try {
        for (let i = 0; i < fileList.value.length; i++) {
            const file = fileList.value[i];
            if (!file) continue;

            logs.value.push(`\n正在处理第 ${i + 1}/${fileList.value.length} 个文件: ${file.name}`);

            const result = await convertToWebM(file, {
                ...settings,
                onProgress: (p) => {
                    // 计算总体进度
                    const baseProgress = (i / fileList.value.length) * 100;
                    const fileProgress = (p / 100) * (100 / fileList.value.length);
                    progress.value = baseProgress + fileProgress;
                },
                onLog: (message) => {
                    logs.value.push(message);
                    // 自动滚动到底部
                    setTimeout(() => {
                        const logPanel = document.querySelector('.log-panel');
                        if (logPanel) {
                            logPanel.scrollTop = logPanel.scrollHeight;
                        }
                    }, 0);
                },
            });

            results.value.push(result);
        }

        ElMessage.success(`成功转换 ${results.value.length} 个视频`);
    } catch (error) {
        console.error('转换失败:', error);
        ElMessage.error(error instanceof Error ? error.message : '转换失败');
        logs.value.push(`错误: ${error instanceof Error ? error.message : '未知错误'}`);
    } finally {
        converting.value = false;
    }
}

// 预览视频
function handlePreview(result: ConvertResult) {
    previewUrl.value = result.url;
    showPreview.value = true;
}

// 下载视频
function handleDownload(result: ConvertResult) {
    const a = document.createElement('a');
    a.href = result.url;
    a.download = result.fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    ElMessage.success('下载已开始');
}
</script>

<style scoped lang="scss">
.video-converter {
    max-width: 800px;
    margin: 0 auto;
    padding: 24px;
}

.upload-area {
    border: 2px dashed #dcdfe6;
    border-radius: 8px;
    padding: 48px 24px;
    text-align: center;
    cursor: pointer;
    transition: all 0.3s ease;
    background-color: #fafafa;

    &:hover {
        border-color: #409eff;
        background-color: #f0f9ff;
    }

    &.drag-over {
        border-color: #409eff;
        background-color: #e6f4ff;
        transform: scale(1.02);
    }

    &.has-files {
        padding: 24px;
    }
}

.upload-placeholder {
    .upload-icon {
        color: #c0c4cc;
        margin-bottom: 16px;
    }

    .upload-text {
        font-size: 16px;
        color: #606266;
        margin: 8px 0;
    }

    .upload-hint {
        font-size: 13px;
        color: #909399;
        margin: 0;
    }
}

.file-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.file-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px;
    background: white;
    border-radius: 6px;
    border: 1px solid #e4e7ed;

    .file-icon {
        font-size: 24px;
        color: #409eff;
    }

    .file-info {
        flex: 1;
        text-align: left;

        .file-name {
            margin: 0;
            font-size: 14px;
            color: #303133;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .file-size {
            margin: 4px 0 0;
            font-size: 12px;
            color: #909399;
        }
    }
}

.add-more-btn {
    margin-top: 12px;
}

.settings-panel {
    margin-top: 24px;
    padding: 24px;
    background: white;
    border-radius: 8px;
    border: 1px solid #e4e7ed;

    .settings-title {
        margin: 0 0 20px;
        font-size: 16px;
        color: #303133;
    }
}

.progress-panel {
    margin-top: 24px;
    padding: 24px;
    background: white;
    border-radius: 8px;
    border: 1px solid #e4e7ed;

    .progress-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 16px;

        .progress-title {
            margin: 0;
            font-size: 16px;
            color: #303133;
        }

        .progress-text {
            font-size: 14px;
            color: #409eff;
            font-weight: 500;
        }
    }
}

.log-panel {
    margin-top: 16px;
    padding: 12px;
    background: #f5f7fa;
    border-radius: 4px;
    max-height: 200px;
    overflow-y: auto;
    scroll-behavior: smooth;

    .log-item {
        margin: 4px 0;
        font-size: 12px;
        color: #606266;
        font-family: 'Consolas', 'Monaco', monospace;
        line-height: 1.6;
        word-break: break-all;
    }
}

.action-buttons {
    margin-top: 24px;
    display: flex;
    gap: 12px;
    justify-content: center;
}

.results-panel {
    margin-top: 24px;

    .results-title {
        font-size: 16px;
        color: #303133;
        margin-bottom: 16px;
    }
}

.results-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.result-item {
    padding: 16px;
    background: white;
    border-radius: 8px;
    border: 1px solid #e4e7ed;
    display: flex;
    justify-content: space-between;
    align-items: center;

    .result-info {
        display: flex;
        align-items: center;
        gap: 12px;

        .result-icon {
            color: #67c23a;
        }

        .result-details {
            .result-name {
                margin: 0;
                font-size: 14px;
                color: #303133;
            }

            .result-size {
                margin: 4px 0 0;
                font-size: 12px;
                color: #909399;
            }
        }
    }

    .result-actions {
        display: flex;
        gap: 8px;
    }
}

.preview-video {
    width: 100%;
    max-height: 70vh;
    border-radius: 4px;
}
</style>
