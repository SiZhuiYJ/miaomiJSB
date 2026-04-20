<script setup lang="ts">
import { computed, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores';
import IsPin from './IsPin.vue';
import type { ConversationDetail, ConversationMember, UserRole } from '../types';
import { updateConversationMemberRole } from '../api/conversations';
import { uploadConversationAvatar as uploadConversationAvatarApi } from '../api/files';
import {
  groupRoleMap,
  formatChatTime,
  getConversationAvatarText,
  getConversationAvatarUrl,
  getConversationDisplayTitle,
  getMemberAvatarUrl,
} from '../utils/chat';

const props = defineProps<{
  modelValue: boolean;
  conversation: ConversationDetail;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  'update:conversation': [];
  'load-more': [];
}>();

const { user } = storeToRefs(useAuthStore());

const avatarInputRef = ref<HTMLInputElement | null>(null);
const uploadingAvatar = ref(false);
const roleUpdatingUserId = ref<number | null>(null);

const title = computed(() => getConversationDisplayTitle(props.conversation, user.value?.userId));
const avatarUrl = computed(() => getConversationAvatarUrl(props.conversation));
const isGroupConversation = computed(() => props.conversation.conversationType === 'group');
const currentMember = computed(
  () => props.conversation.members.find((item) => item.userId === user.value?.userId) ?? null,
);
const currentMemberRole = computed<UserRole | null>(() => currentMember.value?.memberRole ?? null);
const canChangeGroupAvatar = computed(
  () => isGroupConversation.value && ['owner', 'admin'].includes(currentMemberRole.value || ''),
);
const canManageMemberRoles = computed(
  () => isGroupConversation.value && currentMemberRole.value === 'owner',
);
const sortedMembers = computed(() => {
  const orderMap: Record<UserRole, number> = {
    owner: 0,
    admin: 1,
    member: 2,
  };

  return [...props.conversation.members].sort((left, right) => {
    const orderDiff = orderMap[left.memberRole] - orderMap[right.memberRole];
    if (orderDiff !== 0) return orderDiff;
    return Date.parse(left.joinedAt) - Date.parse(right.joinedAt);
  });
});

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === 'object' && error !== null) {
    const responseMessage = (error as { response?: { data?: { message?: unknown } } }).response?.data?.message;
    if (typeof responseMessage === 'string' && responseMessage.trim()) {
      return responseMessage;
    }

    const message = (error as { message?: unknown }).message;
    if (typeof message === 'string' && message.trim()) {
      return message;
    }
  }

  return fallback;
}

function applyConversationDetail(detail: ConversationDetail) {
  Object.assign(props.conversation, {
    ...detail,
    members: detail.members,
  });
}

function toggleMuted() {
  props.conversation.isMuted = !props.conversation.isMuted;
  emit('update:conversation');
}

function togglePinned() {
  props.conversation.isPinned = !props.conversation.isPinned;
  emit('update:conversation');
}

function openAvatarPicker() {
  if (!canChangeGroupAvatar.value || uploadingAvatar.value) return;
  avatarInputRef.value?.click();
}

async function handleAvatarChange(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';

  if (!file || !canChangeGroupAvatar.value) return;

  uploadingAvatar.value = true;
  try {
    const { data } = await uploadConversationAvatarApi(props.conversation.id, file);
    props.conversation.avatarKey = data.key;
    emit('update:conversation');
    ElMessage.success('群头像已更新');
  } catch (error) {
    console.error('Failed to upload conversation avatar:', error);
    ElMessage.error(getErrorMessage(error, '群头像上传失败'));
  } finally {
    uploadingAvatar.value = false;
  }
}

function getNextMemberRole(member: ConversationMember): 'admin' | 'member' | null {
  if (member.memberRole === 'admin') return 'member';
  if (member.memberRole === 'member') return 'admin';
  return null;
}

async function handleToggleMemberRole(member: ConversationMember) {
  const nextRole = getNextMemberRole(member);
  if (!canManageMemberRoles.value || !nextRole) return;

  roleUpdatingUserId.value = member.userId;
  try {
    const { data } = await updateConversationMemberRole(props.conversation.id, member.userId, {
      memberRole: nextRole,
    });
    applyConversationDetail(data);
    emit('update:conversation');
    ElMessage.success(nextRole === 'admin' ? '已设为管理员' : '已设为成员');
  } catch (error) {
    console.error('Failed to update conversation member role:', error);
    ElMessage.error(getErrorMessage(error, '成员权限更新失败'));
  } finally {
    roleUpdatingUserId.value = null;
  }
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    title="会话详情"
    width="min(92vw, 620px)"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="conversation-detail-dialog">
      <div class="avatar-section">
        <el-image
          v-if="avatarUrl"
          class="avatar-preview"
          :src="avatarUrl"
          fit="cover"
          :preview-src-list="[avatarUrl]"
          lazy
        />
        <el-avatar v-else class="avatar-fallback" :size="96" shape="square">
          {{ getConversationAvatarText(conversation) }}
        </el-avatar>

        <div v-if="isGroupConversation" class="avatar-actions">
          <el-button
            color="#111827"
            plain
            :loading="uploadingAvatar"
            :disabled="!canChangeGroupAvatar"
            @click="openAvatarPicker"
          >
            更换群头像
          </el-button>
          <span class="avatar-tip">
            {{ canChangeGroupAvatar ? '仅支持常见图片格式' : '仅群主或管理员可更换群头像' }}
          </span>
        </div>
        <input
          ref="avatarInputRef"
          class="hidden-input"
          type="file"
          accept="image/*"
          @change="handleAvatarChange"
        >
      </div>

      <div class="detail-meta">
        <p class="meta-item">会话标题：{{ title }}</p>
        <p class="meta-item">会话类型：{{ conversation.conversationType === 'group' ? '群聊' : '单聊' }}</p>
        <p class="meta-item">会话 ID：{{ conversation.id }}</p>
        <p class="meta-item">创建时间：{{ formatChatTime(conversation.createdAt) }}</p>
        <p class="meta-item">更新时间：{{ formatChatTime(conversation.updatedAt) }}</p>
      </div>

      <div class="members-section">
        <div class="section-header">
          <span>成员列表</span>
          <span v-if="canManageMemberRoles" class="section-tip">群主可切换成员和管理员职位</span>
        </div>

        <div class="member-list">
          <div v-for="item in sortedMembers" :key="item.userId" class="member-row">
            <div class="member-main">
              <el-avatar class="conversation-avatar" :src="getMemberAvatarUrl(item)" :size="48" shape="square">
                {{ item.nickName ? item.nickName.slice(0, 1) : '用' }}
              </el-avatar>
              <div class="member-text">
                <div class="member-name-row">
                  <span class="member-name">{{ item.nickName || item.userAccount || item.userId }}</span>
                  <el-tag
                    size="small"
                    :type="item.memberRole === 'owner' ? 'danger' : item.memberRole === 'admin' ? 'warning' : 'info'"
                  >
                    {{ groupRoleMap[item.memberRole] || item.memberRole }}
                  </el-tag>
                </div>
                <div class="member-subtext">加入时间：{{ formatChatTime(item.joinedAt) }}</div>
              </div>
            </div>

            <el-button
              v-if="canManageMemberRoles && item.memberRole !== 'owner' && item.userId !== user?.userId"
              size="small"
              color="#111827"
              plain
              :loading="roleUpdatingUserId === item.userId"
              @click="handleToggleMemberRole(item)"
            >
              {{ item.memberRole === 'admin' ? '设为成员' : '设为管理员' }}
            </el-button>
          </div>
        </div>
      </div>

      <div class="action-icons">
        <el-icon @click="toggleMuted">
          <MuteNotification v-if="conversation.isMuted" />
          <Bell v-else />
        </el-icon>
        <IsPin v-model:is-pinned="conversation.isPinned" @toggle-pinned="togglePinned" />
      </div>

      <el-button color="#111827" class="load-more" @click="emit('load-more')">加载更早消息</el-button>
    </div>
  </el-dialog>
</template>

<style scoped lang="scss">
.conversation-detail-dialog {
  display: grid;
  gap: 20px;
}

.avatar-section {
  display: grid;
  justify-items: center;
  gap: 10px;
}

.avatar-preview,
.avatar-fallback {
  width: 96px;
  height: 96px;
  border-radius: 16px;
  overflow: hidden;
  background: #f3f4f6;
}

.avatar-actions {
  display: grid;
  justify-items: center;
  gap: 8px;
}

.avatar-tip {
  color: #6b7280;
  font-size: 12px;
  line-height: 18px;
}

.hidden-input {
  display: none;
}

.detail-meta {
  display: grid;
  gap: 8px;
  padding: 14px 16px;
  border-radius: 12px;
  background: #f9fafb;
}

.meta-item {
  margin: 0;
  color: #111827;
  font-size: 14px;
  line-height: 20px;
}

.members-section {
  display: grid;
  gap: 12px;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  color: #111827;
  font-size: 14px;
  font-weight: 600;
}

.section-tip {
  color: #6b7280;
  font-size: 12px;
  font-weight: 400;
}

.member-list {
  display: grid;
  gap: 10px;
  max-height: 320px;
  overflow: auto;
}

.member-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  background: #fff;
}

.member-main {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.member-text {
  min-width: 0;
}

.member-name-row {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.member-name {
  overflow: hidden;
  color: #111827;
  font-size: 14px;
  font-weight: 600;
  line-height: 20px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.member-subtext {
  color: #6b7280;
  font-size: 12px;
  line-height: 18px;
}

.action-icons {
  display: flex;
  align-items: center;
  gap: 14px;
  color: #111827;
  font-size: 20px;
}

.load-more {
  justify-self: flex-start;
}

@media (max-width: 768px) {
  .section-header,
  .member-row {
    align-items: flex-start;
    flex-direction: column;
  }

  .member-main {
    width: 100%;
  }
}
</style>
