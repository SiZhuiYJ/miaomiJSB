<script setup lang="ts">
import ImageUploader from '@/features/file/components/ImageUploader.vue';
import { ref, onMounted, watch } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia'
import { authApi } from '@/features/auth/api';
import { notifySuccess, notifyError, notifyWarning } from '@/utils/notification';
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
        return `${API_BASE_URL}mm/Files/users/${user.value.userId}/${user.value.avatarKey}`;
    }
    return '';
});
watch(
    () => image.value,
    (newVal) => {
        console.log(newVal)
    },
    {
        immediate: true
    }
);

// 上传头像
function uploadImage() {
    if (image.value) {
        const imageFile = dataURLToFile(image.value)
        if (imageFile)
            uploadFile(imageFile, loading)
    }
}
</script>

<template>
    <el-image v-if="user?.avatarKey" style="width: 400px; height: 400px" :src="url" :fit="fit"
        :preview-src-list="[url]" />
    <span v-else class="avatar-text-large">
        {{ (user ? (user?.nickName || user?.userAccount || user?.email).slice(0, 1).toUpperCase() : 'U') }}
    </span>
    <ImageUploader @crop="onCropped" />
    <el-button @click="uploadImage" :disabled="IsUpload" :loading="loading">
        更改头像
    </el-button>
</template>

<style scoped lang="scss">
.avatar-text-large {
    width: 100px;
    height: 100px;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: var(--accent-alt);
}
</style>
