<script setup lang="ts">
import ImageUploader from '@/features/file/components/ImageUploader.vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia'
import { notifySuccess } from '@/utils/notification';
import { API_BASE_URL } from '@/config';
import { uploadFile, dataURLToFile } from '@/features/setting/composables/useImage'

const image = ref<string>()
const IsUpload = ref<boolean>(true)

const { user } = storeToRefs(useAuthStore());
const loading = ref(false);
const onCropped = (data: string) => {
    image.value = data
    if (image.value)
        IsUpload.value = false
    console.log(data);
    console.log('裁剪成功')
    notifySuccess('裁剪成功')
};
const fit = 'fill';
const url = computed(() => {
    if (user.value?.avatarKey) {
        return `${API_BASE_URL}/mm/Files/users/${user.value.userId}/${user.value.avatarKey}`;
    }
    return '';
});
watch(
    () => image.value,
    (newVal?: string) => {
        console.log(newVal)
    },
    {
        immediate: true
    }
);

// 上传头像
async function uploadImage() {
    if (image.value) {
        const imageFile = dataURLToFile(image.value)
        if (imageFile)
            await uploadFile(imageFile, loading)
    }
}
</script>

<template>
    <div class="setting-subpage">
        <div class="setting-card">
            <div class="setting-header">
                <h3 class="setting-title">头像修改</h3>
                <p class="setting-subtitle">上传并裁剪新头像后，点击按钮完成更新。</p>
            </div>

            <div class="avatar-main">
                <el-image v-if="user?.avatarKey" class="avatar-preview" :src="url" :fit="fit" :preview-src-list="[url]" lazy />
                <span v-else class="avatar-text-large">
                    {{ (user ? (user?.nickName || user?.userAccount || user?.email).slice(0, 1).toUpperCase() : 'U') }}
                </span>
                <ImageUploader @crop="onCropped" />
                <el-button type="primary" @click="uploadImage" :disabled="IsUpload" :loading="loading">
                    更改头像
                </el-button>
            </div>
        </div>
    </div>
</template>

<style scoped lang="scss">
.setting-subpage {
    width: min(760px, 92vw);
    margin: 24px auto;
}

.setting-card {
    background-color: var(--bg-elevated);
    border-radius: 16px;
    padding: 24px;
    border: 1px solid var(--border-color);
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.04);
}

.setting-header {
    margin-bottom: 16px;
}

.setting-title {
    margin: 0;
    font-size: 20px;
    color: var(--text-color);
}

.setting-subtitle {
    margin: 8px 0 0;
    color: var(--text-muted);
    font-size: 13px;
}

.avatar-main {
    display: flex;
    flex-direction: column;
    gap: 16px;
    align-items: center;
}

.avatar-preview {
    width: min(400px, 100%);
    height: min(400px, 60vw);
    border-radius: 16px;
}

.avatar-text-large {
    width: 120px;
    height: 120px;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: var(--accent-alt);
    border-radius: 16px;
    font-size: 36px;
    color: var(--accent-on);
}
</style>
