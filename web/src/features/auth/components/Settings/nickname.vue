<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '@/stores';
import { authApi } from '@/features/auth/api';
import { notifySuccess, notifyError, notifyWarning } from '@/utils/notification';

const authStore = useAuthStore();

const loading = ref(false);
const nickName = ref(authStore.user?.nickName || '');

async function handleSave() {
  if (!nickName.value) {
    notifyError('昵称不能为空');
    return;
  }

  if (nickName.value === authStore.user?.nickName) {
    notifyWarning('昵称未修改');
    return;
  }

  loading.value = true;
  try {
    const profileRes = await authApi.updateProfileInfo({
      nickName: nickName.value,
      avatarKey: authStore.user?.avatarKey || null
    });
    authStore.setSession(profileRes.data);
    notifySuccess('昵称修改成功');
  } catch (err: any) {
    console.error(err);
    notifyError('昵称修改失败');
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="container">
    <div class="card">
      <div class="form-group">
        <div class="field">
          <label class="label">昵称</label>
          <input class="input" v-model="nickName" placeholder="设置昵称" focus />
          <label class="desc">好的昵称能让大家更容易记住你。</label>
        </div>
      </div>
    </div>

    <div class="actions">
      <button class="btn-save" :loading="loading" @click="handleSave">保存</button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.container {
  min-height: 100vh;
  box-sizing: border-box;
  background-color: var(--bg-color);
  padding: 20px;
}

.card {
  background-color: var(--bg-elevated);
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 20px;
}

.label {
  display: block;
  font-size: 14px;
  color: var(--label-muted);
  margin-bottom: 8px;
}

.input {
  width: 100%;
  height: 44px;
  background-color: var(--bg-color);
  border-radius: 8px;
  padding: 0 12px;
  font-size: 14px;
  color: var(--label-color);
  box-sizing: border-box;
  border: 1px solid var(--border-color);
}

.desc {
  display: block;
  font-size: 12px;
  color: var(--label-muted);
  margin-top: 8px;
}

.btn-save {
  background-color: var(--theme-primary);
  color: #fff;
  border-radius: 8px;
  font-size: 16px;
  height: 44px;
  line-height: 44px;
  margin-top: 10px;
}

.actions {
  padding: 0 10px;
}
</style>
