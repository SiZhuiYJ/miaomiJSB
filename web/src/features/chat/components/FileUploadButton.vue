<template>
  <div class="file-upload-button">
    <el-tooltip content="发送文件" placement="top">
      <el-button 
        circle 
        @click="triggerFileSelect"
        :loading="uploading"
        :disabled="disabled"
      >
        <el-icon><Paperclip /></el-icon>
      </el-button>
    </el-tooltip>
    
    <input
      ref="fileInputRef"
      type="file"
      multiple
      accept="image/*,video/*,audio/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip,.rar,.7z"
      style="display: none"
      @change="handleFileSelect"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Paperclip } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { validateFile } from '../utils/fileHelper';

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
      validFiles.push(file);
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
