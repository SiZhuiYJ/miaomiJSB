<script setup lang="ts">
import { ref } from 'vue';
import { convertVideoToWebM, type ProgressInfo } from '../utils/videoConverter';

const isConverting = ref(false);
const progress = ref(0);

const handleFileSelect = async (event: Event) => {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    if (!file) return;

    isConverting.value = true;
    progress.value = 0;

    try {
        const webmFile = await convertVideoToWebM(file, {
            codec: 'libvpx',                 // VP8 (兼容性好且快速)
            crf: 28,                         // 适中质量[reference:9]
            preset: 'fast',                  // 偏向速度
            audioBitrate: '96k',
            scale: '1280:-1',                // 限制宽度为 1280px (保持比例)
            onProgress: ({ percent }: ProgressInfo) => {
                progress.value = percent;
            },
        });

        console.log('转换完成', webmFile);
        // 此处可执行下载或预览操作
    } catch (error) {
        console.error('转换失败', error);
    } finally {
        isConverting.value = false;
    }
};
</script>

<template>
    <div>
        <input type="file" accept="video/*" @change="handleFileSelect" :disabled="isConverting" />
        <div v-if="isConverting" class="progress-bar">
            <div class="progress-fill" :style="{ width: progress + '%' }"></div>
            <span>{{ progress }}%</span>
        </div>
    </div>
</template>
}
}
}
})
}
}