<template>
    <div class="video-converter-page">
        <el-card class="page-card">
            <template #header>
                <div class="card-header">
                    <h2>视频转 WebM 工具</h2>
                    <p class="subtitle">基于 FFmpeg.wasm 的纯前端视频转换</p>
                </div>
            </template>

            <VideoConverter />
        </el-card>
    </div>
</template>

<script setup lang="ts">
import VideoConverter from '@/components/VideoConverter/index.vue';
</script>

<style scoped lang="scss">
.video-converter-page {
    padding: 24px;
    min-height: 100vh;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.page-card {
    max-width: 900px;
    margin: 0 auto;

    :deep(.el-card__header) {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border-bottom: none;
    }

    .card-header {
        h2 {
            margin: 0;
            color: white;
            font-size: 24px;
            font-weight: 600;
        }

        .subtitle {
            margin: 8px 0 0;
            color: rgba(255, 255, 255, 0.9);
            font-size: 14px;
        }
    }
}
</style>
