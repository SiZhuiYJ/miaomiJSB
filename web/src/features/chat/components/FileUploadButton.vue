<template>
  <div class="file-upload-button">
    <el-tooltip content="发送文件" placement="top">
      <el-button circle @click="triggerFileSelect" :loading="uploading" :disabled="disabled">
        <el-icon>
          <Paperclip />
        </el-icon>
      </el-button>
    </el-tooltip>
    <input ref="fileInputRef" type="file" multiple size="104857600"
      accept="image/*,video/*,audio/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip,.rar,.7z" style="display: none"
      @change="handleFileSelect" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Paperclip } from '@element-plus/icons-vue';
import { validateFile } from '../utils/fileHelper';
import { convertToWebP } from '@/utils/convertToWebP';

const props = defineProps<{
  conversationId?: number;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  fileSelected: [files: File[]];
}>();

const fileInputRef = ref<HTMLInputElement>();
const uploading = ref(false);

function triggerFileSelect() {
  if (props.disabled) return;
  fileInputRef.value?.click();
}

async function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement;
  const files = Array.from(input.files || []);

  if (files.length === 0) return;

  // 验证所有文件
  const invalidFiles: { name: string; error: string }[] = [];
  const validFiles: File[] = [];

  for (const file of files) {
    const validation = validateFile(file);
    if (validation.valid) {
      try {
        // 判断是否为图片，如果是图片则转换为 WebP 格式
        if (file.type.startsWith('image/')) {
          const webpFile = await convertToWebP(file, {
            quality: 1,
            output: 'file',
            fileName: file.name.replace(/\.[^.]+$/, '.webp')
          }) as File;
          validFiles.push(webpFile);
        }
        // // 判断是否为视频，如果是视频则抽取随机帧作为封面
        // else if (file.type.startsWith('video/')) {
        //   const coverFile = await extractVideoFrameToWebP(file, {
        //     quality: 1,
        //     fileName: `${file.name}.webp`
        //   });
        //   validFiles.push(file);
        //   validFiles.push(coverFile);
        // } 
        else {
          validFiles.push(file);
        }
      } catch (error) {
        console.error(`处理文件 ${file.name} 失败:`, error);
        ElMessage.error(`${file.name}: 处理失败`);
      }
    } else {
      invalidFiles.push({ name: file.name, error: validation.error || '' });
    }
  }

  // 显示错误信息
  if (invalidFiles.length > 0) {
    invalidFiles.forEach(({ name, error }) => {
      ElMessage.warning(`${name}: ${error}`);
    });
  }

  // 如果有有效文件，触发事件
  if (validFiles.length > 0) {
    emit('fileSelected', validFiles);
  }

  // 清空input，允许重复选择同一文件
  input.value = '';
}
</script>

<style scoped lang="scss">
.file-upload-button {
  display: inline-flex;
  align-items: center;
}
</style>
