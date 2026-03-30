<script setup lang="ts">
import ImageUploader from '@/features/file/components/ImageUploader.vue';
import { ref, onMounted } from 'vue';
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia'
import { authApi } from '@/features/auth/api';
import { notifySuccess, notifyError, notifyWarning } from '@/utils/notification';
import { API_BASE_URL } from '@/config';

const { user } = storeToRefs(useAuthStore());
const loading = ref(false);
const onCropped = (data: string) => {
    // console.log(data);
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
</script>

<template>
    <el-image v-if="user?.avatarKey" style="width: 100px; height: 100px" :src="url" :fit="fit"
        :preview-src-list="[url]" />
    <span v-else class="avatar-text-large">
        {{ (user ? (user?.nickName || user?.userAccount || user?.email).slice(0, 1).toUpperCase() : 'U') }}
    </span>
    <ImageUploader @crop="onCropped" />
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
