<script setup lang="ts">
import ProgressiveAvatar from '@/components/ProgressiveAvatar.vue';
import { createAvatarUploadFormData } from '@/utils/avatar';
import { computed, ref } from 'vue';
import { ElMessageBox } from 'element-plus';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores';
import IsPin from './IsPin.vue';
import type {
  ConversationDetail,
  ConversationMember,
  GroupMuteMode,
  UserRole,
} from '../types';
import {
  disbandConversation,
  inviteConversationMembers,
  kickConversationMember,
  muteConversationMember,
  unmuteConversationMember,
  updateConversationMemberRole,
} from '../api/conversations';
import { uploadConversationAvatar as uploadConversationAvatarApi } from '../api/files';
import {
  groupRoleMap,
  formatChatTime,
  getConversationAvatarSources,
  getConversationAvatarText,
  getConversationDisplayTitle,
  getMemberAvatarSources,
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
const kickingUserId = ref<number | null>(null);
const mutingUserId = ref<number | null>(null);
const disbanding = ref(false);
const inviting = ref(false);

const title = computed(() => getConversationDisplayTitle(props.conversation, user.value?.userId));
const avatarSources = computed(() => getConversationAvatarSources(props.conversation));
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
const canInviteMembers = computed(
  () => isGroupConversation.value && ['owner', 'admin'].includes(currentMemberRole.value || ''),
);
const canKickMembers = computed(
  () => isGroupConversation.value && ['owner', 'admin'].includes(currentMemberRole.value || ''),
);
const canMuteMembers = computed(
  () => isGroupConversation.value && ['owner', 'admin'].includes(currentMemberRole.value || ''),
);
const canDisbandConversation = computed(
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

function getMemberName(member: ConversationMember) {
  return member.nickName || member.userAccount || String(member.userId);
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
    const formData = await createAvatarUploadFormData(file);
    const { data } = await uploadConversationAvatarApi(props.conversation.id, formData);
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

function isMemberMuted(member: ConversationMember) {
  if (!member.isMuted) return false;
  if (!member.mutedUntil) return true;
  return Date.parse(member.mutedUntil) > Date.now();
}

function formatMuteStatus(member: ConversationMember) {
  if (!isMemberMuted(member)) return '正常发言';
  if (member.mutedMode === 'permanent' || !member.mutedUntil) return '永久禁言中';
  return `禁言至 ${formatChatTime(member.mutedUntil)}`;
}

function getMuteOptions() {
  return [
    { label: '10 分钟', mode: 'temporary' as GroupMuteMode, durationMinutes: 10 },
    { label: '1 小时', mode: 'temporary' as GroupMuteMode, durationMinutes: 60 },
    { label: '24 小时', mode: 'temporary' as GroupMuteMode, durationMinutes: 60 * 24 },
    { label: '永久禁言', mode: 'permanent' as GroupMuteMode, durationMinutes: undefined },
  ];
}

async function handleInviteMembers() {
  if (!canInviteMembers.value || inviting.value) return;

  try {
    const { value } = await ElMessageBox.prompt(
      '请输入待邀请用户 ID，多个 ID 用英文逗号分隔（例如：1001,1002）',
      '邀请成员',
      {
        inputPlaceholder: '用户ID列表',
        confirmButtonText: '确认邀请',
        cancelButtonText: '取消',
      },
    );

    const memberUserIds = value
      .split(',')
      .map((item: string) => Number(item.trim()))
      .filter((item: number) => Number.isFinite(item) && item > 0);

    if (!memberUserIds.length) {
      ElMessage.warning('请输入有效的用户 ID');
      return;
    }

    inviting.value = true;
    const { data } = await inviteConversationMembers(props.conversation.id, { memberUserIds });
    applyConversationDetail(data);
    emit('update:conversation');
    ElMessage.success('邀请已发起');
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(getErrorMessage(error, '邀请成员失败'));
    }
  } finally {
    inviting.value = false;
  }
}

async function handleKickMember(member: ConversationMember) {
  if (!canKickMembers.value || member.memberRole === 'owner') return;

  const confirmText = `确认将「${getMemberName(member)}」移出群聊吗？`;
  try {
    await ElMessageBox.confirm(confirmText, '踢出成员', {
      type: 'warning',
      confirmButtonText: '确认踢出',
      cancelButtonText: '取消',
    });

    kickingUserId.value = member.userId;
    const { data } = await kickConversationMember(props.conversation.id, member.userId);
    applyConversationDetail(data);
    emit('update:conversation');
    ElMessage.success('成员已移出群聊');
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(getErrorMessage(error, '踢出成员失败'));
    }
  } finally {
    kickingUserId.value = null;
  }
}

async function handleMuteMember(member: ConversationMember) {
  if (!canMuteMembers.value || member.memberRole === 'owner') return;

  const options = getMuteOptions();
  const label = await ElMessageBox.prompt(
    `请选择禁言模式：${options.map((item) => item.label).join(' / ')}`,
    `禁言 ${getMemberName(member)}`,
    {
      inputPlaceholder: '例如：10 分钟 / 1 小时 / 24 小时 / 永久禁言',
      confirmButtonText: '确认',
      cancelButtonText: '取消',
    },
  ).then((res: { value: string }) => res.value).catch(() => '');

  if (!label) return;
  const targetOption = options.find((item) => item.label === label.trim());
  if (!targetOption) {
    ElMessage.warning('禁言选项无效，请按照提示输入');
    return;
  }

  mutingUserId.value = member.userId;
  try {
    const { data } = await muteConversationMember(props.conversation.id, member.userId, {
      mode: targetOption.mode,
      durationMinutes: targetOption.durationMinutes,
    });
    applyConversationDetail(data);
    emit('update:conversation');
    ElMessage.success(targetOption.mode === 'permanent' ? '已永久禁言' : '成员已禁言');
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '禁言失败'));
  } finally {
    mutingUserId.value = null;
  }
}

async function handleUnmuteMember(member: ConversationMember) {
  if (!canMuteMembers.value) return;

  mutingUserId.value = member.userId;
  try {
    const { data } = await unmuteConversationMember(props.conversation.id, member.userId);
    applyConversationDetail(data);
    emit('update:conversation');
    ElMessage.success('已解除禁言');
  } catch (error) {
    ElMessage.error(getErrorMessage(error, '解除禁言失败'));
  } finally {
    mutingUserId.value = null;
  }
}

async function handleDisbandConversation() {
  if (!canDisbandConversation.value || disbanding.value) return;

  try {
    await ElMessageBox.confirm('解散后将无法恢复，确定要解散该群聊吗？', '解散群聊', {
      type: 'warning',
      confirmButtonText: '确认解散',
      cancelButtonText: '取消',
    });

    disbanding.value = true;
    await disbandConversation(props.conversation.id);
    ElMessage.success('群聊已解散');
    emit('update:modelValue', false);
    emit('update:conversation');
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(getErrorMessage(error, '解散群聊失败'));
    }
  } finally {
    disbanding.value = false;
  }
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    title="会话详情"
    width="min(92vw, 700px)"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="conversation-detail-dialog">
      <div class="avatar-section">
        <ProgressiveAvatar
          class="avatar-preview"
          :src="avatarSources.src"
          :thumbnail-src="avatarSources.thumbnailSrc"
          :size="96"
          shape="square"
        >
          {{ getConversationAvatarText(conversation) }}
        </ProgressiveAvatar>

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

      <div v-if="isGroupConversation" class="group-management-section">
        <div class="section-header">
          <span>群管理</span>
          <span class="section-tip">邀请成员 / 踢人 / 禁言 / 解散群聊</span>
        </div>
        <div class="management-actions">
          <el-button color="#111827" plain :disabled="!canInviteMembers" :loading="inviting" @click="handleInviteMembers">
            邀请成员
          </el-button>
          <el-button
            color="#dc2626"
            plain
            :disabled="!canDisbandConversation"
            :loading="disbanding"
            @click="handleDisbandConversation"
          >
            解散群聊
          </el-button>
        </div>
      </div>

      <div class="members-section">
        <div class="section-header">
          <span>成员列表</span>
          <span v-if="canManageMemberRoles" class="section-tip">群主可切换成员和管理员职位</span>
        </div>

        <div class="member-list">
          <div v-for="item in sortedMembers" :key="item.userId" class="member-row">
            <div class="member-main">
              <ProgressiveAvatar
                class="conversation-avatar"
                :src="getMemberAvatarUrl(item)"
                :thumbnail-src="getMemberAvatarSources(item).thumbnailSrc"
                :size="48"
                shape="square"
              >
                {{ item.nickName ? item.nickName.slice(0, 1) : '用' }}
              </ProgressiveAvatar>
              <div class="member-text">
                <div class="member-name-row">
                  <span class="member-name">{{ item.nickName || item.userAccount || item.userId }}</span>
                  <el-tag
                    size="small"
                    :type="item.memberRole === 'owner' ? 'danger' : item.memberRole === 'admin' ? 'warning' : 'info'"
                  >
                    {{ groupRoleMap[item.memberRole] || item.memberRole }}
                  </el-tag>
                  <el-tag v-if="isMemberMuted(item)" size="small" type="danger">{{ formatMuteStatus(item) }}</el-tag>
                </div>
                <div class="member-subtext">加入时间：{{ formatChatTime(item.joinedAt) }}</div>
              </div>
            </div>

            <div class="member-ops">
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

              <el-button
                v-if="canMuteMembers && item.memberRole !== 'owner' && item.userId !== user?.userId"
                size="small"
                type="warning"
                plain
                :loading="mutingUserId === item.userId"
                @click="isMemberMuted(item) ? handleUnmuteMember(item) : handleMuteMember(item)"
              >
                {{ isMemberMuted(item) ? '解除禁言' : '禁言' }}
              </el-button>

              <el-button
                v-if="canKickMembers && item.memberRole !== 'owner' && item.userId !== user?.userId"
                size="small"
                type="danger"
                plain
                :loading="kickingUserId === item.userId"
                @click="handleKickMember(item)"
              >
                踢出群聊
              </el-button>
            </div>
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

.detail-meta,
.group-management-section {
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

.management-actions {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
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
  flex-wrap: wrap;
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

.member-ops {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
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

  .member-main,
  .member-ops {
    width: 100%;
  }

  .member-ops {
    justify-content: flex-start;
  }
}
</style>
