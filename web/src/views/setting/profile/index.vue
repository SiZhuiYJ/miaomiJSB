<script setup lang="ts">
import nickname from '@/features/auth/components/Settings/nickname.vue';
import router from "@/routers";
import { useAuthStore } from '@/stores';
import { storeToRefs } from 'pinia';
import { API_BASE_URL } from '@/config';
import { extractEmail, extractDomain } from '@/utils/auth';

const { user } = storeToRefs(useAuthStore());
const inputEmail = ref<string>('');
const emailDisabled = ref<boolean>(true);
const url = computed(() => {
    if (user.value?.avatarKey) {
        return `${API_BASE_URL}mm/Files/users/${user.value.userId}/${user.value.avatarKey}`;
    }
    return '';
});
// 邮箱后缀名
const emailFix = ref<string[]>(['@qq.com', '@163.com', '@126.com', '@sina.com', '@aliyun.com']);
const select = ref<string>();
const fit = 'fill';
watch(() => user.value?.email, (newEmail: string | null) => {
    if (newEmail) {
        inputEmail.value = extractEmail(newEmail);
        select.value = '@' + extractDomain(newEmail);
        emailDisabled.value = true;
    } else {
        inputEmail.value = '';
        emailDisabled.value = false;
    }
}, { immediate: true });
</script>

<template>
    <div class="container">
        <el-image style="width: 100px; height: 100px" :src="url" :fit="fit" :preview-src-list="[url]" lazy />
        <el-icon @click="router.push('/setting/avatar')">
            <Upload />
        </el-icon>
        <el-input v-model="inputEmail" :disabled="emailDisabled" clearable clear-icon="CloseBold" placeholder="邮箱地址"
            style="width: 350px">
            <template #prepend>
                <el-button icon="EditPen" @click="emailDisabled = !emailDisabled" />
            </template>
            <template #append>
                <el-select v-model="select" placeholder="Select" :disabled="emailDisabled" style="width: 150px">
                    <el-option v-for="(value, index) in emailFix" :label="value" :value="index" />
                </el-select>
            </template>
        </el-input>
        <nickname></nickname>
    </div>
</template>

<style scoped lang="scss">
.container {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    padding: 20px;
}
</style>
