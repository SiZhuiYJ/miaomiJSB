<script setup lang="ts">
import ProgressiveAvatar from "@/components/ProgressiveAvatar.vue";
import { computed, onMounted, ref, watch } from "vue";
import {
  acceptFriendRequest,
  createFriendRequest,
  getFriendRequests,
  getFriends,
  rejectFriendRequest,
} from "../api/friends";
import type { ConversationSummary, FriendRequest, Friendship } from "../types";
import {
  formatChatTime,
  getConversationAvatarSources,
  getConversationAvatarUrl,
  getConversationAvatarText,
  getConversationDisplayTitle,
  getMessagePreview,
} from "../utils/chat";

const createConversationType = defineModel<"direct" | "group">("createConversationType", {
  required: true,
});
const createTitle = defineModel<string>("createTitle", {
  required: true,
});
const createMemberUserIds = defineModel<number[]>("createMemberUserIds", {
  required: true,
});

defineProps<{
  selectedConversationId: number;
  conversations: ConversationSummary[];
  isMobile: boolean;
}>();

const emit = defineEmits<{
  createConversation: [];
  selectConversation: [item: ConversationSummary];
}>();

const createConversationTypeOptions = [
  { value: "direct", label: "单聊" },
  { value: "group", label: "群聊" },
];

const friendRequestDialogVisible = ref(false);
const friendRequestLoading = ref(false);
const friendRequestSubmitting = ref(false);
const handlingRequestId = ref<number | null>(null);
const friendRequests = ref<FriendRequest[]>([]);

const friendLoading = ref(false);
const friends = ref<Friendship[]>([]);

const friendRequestStatusMap: Record<FriendRequest["requestStatus"], string> = {
  pending: "待处理",
  accepted: "已通过",
  rejected: "已拒绝",
  cancelled: "已取消",
  expired: "已过期",
};

const sortedFriendRequests = computed(() =>
  [...friendRequests.value].sort(
    (left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt),
  ),
);

const friendOptions = computed(() =>
  friends.value
    .map((item) => ({
      userId: item.friend.userId,
      label:
        item.friend.nickName?.trim() ||
        item.friend.userAccount?.trim() ||
        String(item.friend.userId),
      meta: item.friend.userAccount?.trim() || `ID: ${item.friend.userId}`,
    }))
    .sort((left, right) => left.label.localeCompare(right.label, "zh-CN")),
);

const directMemberUserId = computed<number | undefined>({
  get: () => createMemberUserIds.value[0],
  set: (value) => {
    createMemberUserIds.value = typeof value === "number" ? [value] : [];
  },
});

const canCreateConversation = computed(() =>
  createConversationType.value === "direct"
    ? createMemberUserIds.value.length === 1
    : createMemberUserIds.value.length > 0,
);

watch(
  () => createConversationType.value,
  (value) => {
    if (value === "direct" && createMemberUserIds.value.length > 1) {
      createMemberUserIds.value = createMemberUserIds.value.slice(0, 1);
    }

    if (value === "direct") {
      createTitle.value = "";
    }
  },
);

watch(
  () => createMemberUserIds.value,
  (value) => {
    const normalized = value
      .filter((item) => Number.isFinite(item) && item > 0)
      .filter((item, index, array) => array.indexOf(item) === index);

    if (
      normalized.length !== value.length ||
      normalized.some((item, index) => item !== value[index])
    ) {
      createMemberUserIds.value = normalized;
    }
  },
  { deep: true },
);

function getErrorMessage(error: unknown, fallback: string) {
  if (typeof error === "object" && error !== null) {
    const responseMessage = (
      error as { response?: { data?: { message?: unknown } } }
    ).response?.data?.message;
    if (typeof responseMessage === "string" && responseMessage.trim()) {
      return responseMessage;
    }

    const message = (error as { message?: unknown }).message;
    if (typeof message === "string" && message.trim()) {
      return message;
    }
  }

  return fallback;
}

function getFriendLabel(request: FriendRequest) {
  const target = request.direction === "received" ? request.requester : request.receiver;
  return target.nickName || target.userAccount || String(target.userId);
}

function getFriendMeta(request: FriendRequest) {
  const target = request.direction === "received" ? request.requester : request.receiver;
  return target.userAccount || `ID: ${target.userId}`;
}

function getRequestStatusType(status: FriendRequest["requestStatus"]) {
  switch (status) {
    case "pending":
      return "warning";
    case "accepted":
      return "success";
    case "rejected":
      return "danger";
    default:
      return "info";
  }
}

async function loadFriends() {
  friendLoading.value = true;
  try {
    const { data } = await getFriends();
    friends.value = data;
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "加载好友列表失败"));
  } finally {
    friendLoading.value = false;
  }
}

async function loadRequests() {
  friendRequestLoading.value = true;
  try {
    const { data } = await getFriendRequests();
    friendRequests.value = data;
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "加载好友申请失败"));
  } finally {
    friendRequestLoading.value = false;
  }
}

async function openFriendRequestDialog() {
  friendRequestDialogVisible.value = true;
  await loadRequests();
}

async function handleAddFriend() {
  if (friendRequestSubmitting.value) return;

  try {
    const { value } = await ElMessageBox.prompt("请输入对方账号", "添加好友", {
      inputPlaceholder: "用户账号",
      confirmButtonText: "发送申请",
      cancelButtonText: "取消",
    });

    const receiverUserAccount = value.trim();
    if (!receiverUserAccount) {
      ElMessage.warning("请输入有效的用户账号");
      return;
    }

    friendRequestSubmitting.value = true;
    const { data } = await createFriendRequest({
      receiverUserAccount,
      requestSource: "account",
    });

    ElMessage.success(
      data.requestStatus === "accepted" ? "已自动成为好友" : "好友申请已发送",
    );

    await loadFriends();
    if (friendRequestDialogVisible.value) {
      await loadRequests();
    }
  } catch (error) {
    if (error !== "cancel") {
      ElMessage.error(getErrorMessage(error, "发送好友申请失败"));
    }
  } finally {
    friendRequestSubmitting.value = false;
  }
}

async function handleAcceptRequest(item: FriendRequest) {
  handlingRequestId.value = item.id;
  try {
    await acceptFriendRequest(item.id);
    ElMessage.success("已通过好友申请");
    await Promise.all([loadRequests(), loadFriends()]);
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "处理好友申请失败"));
  } finally {
    handlingRequestId.value = null;
  }
}

async function handleRejectRequest(item: FriendRequest) {
  try {
    const result = await ElMessageBox.prompt("可填写拒绝原因", "拒绝好友申请", {
      inputPlaceholder: "拒绝原因（可选）",
      inputValidator: () => true,
      confirmButtonText: "确认拒绝",
      cancelButtonText: "取消",
    });

    handlingRequestId.value = item.id;
    await rejectFriendRequest(item.id, {
      rejectReason: result.value?.trim() || undefined,
    });
    ElMessage.success("已拒绝好友申请");
    await loadRequests();
  } catch (error) {
    if (error !== "cancel") {
      ElMessage.error(getErrorMessage(error, "拒绝好友申请失败"));
    }
  } finally {
    handlingRequestId.value = null;
  }
}

onMounted(() => {
  void loadFriends();
});
</script>

<template>
  <aside class="left-panel">
    <el-scrollbar :view-class="isMobile ? 'mobile' : ''">
      <ul class="conversation-list">
        <li
          v-for="item in conversations"
          :key="item.id"
          class="conversation-item"
          :class="{ active: item.id === selectedConversationId }"
          @click="emit('selectConversation', item)"
        >
          <ProgressiveAvatar
            class="conversation-avatar"
            :src="getConversationAvatarUrl(item)"
            :thumbnail-src="getConversationAvatarSources(item).thumbnailSrc"
            :size="60"
            shape="square"
          >
            {{ getConversationAvatarText(item) }}
          </ProgressiveAvatar>
          <el-badge
            :value="item.unreadCount"
            :show-zero="false"
            :max="99"
            :offset="[-22, 5]"
            :color="item.isMuted ? '#f5f7fa' : ''"
            class="item"
          >
            <div class="title-row">
              <strong>{{ getConversationDisplayTitle(item) }}</strong>
              <el-icon v-if="item.isPinned" class="pinned-icon">
                <Top />
              </el-icon>
              <el-icon v-if="item.isMuted" class="muted-icon">
                <MuteNotification />
              </el-icon>
            </div>
            <el-text :line-clamp="1">
              {{
                item.lastMessage?.isRecalled
                  ? ""
                  : `${item.lastMessage?.messageType === "system"
                    ? "系统"
                    : (item.lastMessage?.senderNickName || item.lastMessage?.senderUserId || "系统")}：`
              }}
              {{ getMessagePreview(item) }}
            </el-text>
            <template #content="{ value }">
              <div class="custom-content">
                <el-icon>
                  <Message />
                </el-icon>
                <span>{{ value }}</span>
              </div>
            </template>
          </el-badge>
        </li>
      </ul>
    </el-scrollbar>

    <div class="create-box">
      <div class="create-box-item create-box-header">
        <div class="friend-actions">
          <el-button
            icon="Plus"
            color="#111827"
            :loading="friendRequestSubmitting"
            @click="handleAddFriend"
          >
            添加好友
          </el-button>
          <el-button plain @click="openFriendRequestDialog">好友申请</el-button>
        </div>
      </div>

      <div class="create-box-item">
        <el-select
          v-model="createConversationType"
          placeholder="会话类型"
          class="conversation-type-select"
        >
          <el-option
            v-for="item in createConversationTypeOptions"
            :key="item.value"
            :label="item.label"
            :value="item.value"
          />
        </el-select>
        <el-input
          v-model="createTitle"
          clearable
          placeholder="群聊标题，可选"
          :disabled="createConversationType === 'direct'"
        />
      </div>

      <div class="create-box-item create-box-stack">
        <el-select
          v-if="createConversationType === 'direct'"
          v-model="directMemberUserId"
          class="member-select"
          filterable
          clearable
          :loading="friendLoading"
          placeholder="选择一位好友发起单聊"
        >
          <el-option
            v-for="item in friendOptions"
            :key="item.userId"
            :label="item.label"
            :value="item.userId"
          >
            <div class="friend-option">
              <span>{{ item.label }}</span>
              <span class="friend-option-meta">{{ item.meta }}</span>
            </div>
          </el-option>
        </el-select>

        <el-select
          v-else
          v-model="createMemberUserIds"
          class="member-select"
          multiple
          filterable
          clearable
          collapse-tags
          collapse-tags-tooltip
          :loading="friendLoading"
          placeholder="选择好友创建群聊"
        >
          <el-option
            v-for="item in friendOptions"
            :key="item.userId"
            :label="item.label"
            :value="item.userId"
          >
            <div class="friend-option">
              <span>{{ item.label }}</span>
              <span class="friend-option-meta">{{ item.meta }}</span>
            </div>
          </el-option>
        </el-select>

        <div class="create-actions">
          <el-text class="create-tip" type="info">
            只能从已添加的好友中发起单聊或创建群聊。
          </el-text>
          <el-button
            icon="ChatRound"
            color="#111827"
            :disabled="!canCreateConversation"
            @click="emit('createConversation')"
          >
            创建
          </el-button>
        </div>
      </div>
    </div>

    <el-dialog v-model="friendRequestDialogVisible" title="好友申请" width="min(92vw, 640px)">
      <div class="friend-request-panel">
        <div class="friend-request-toolbar">
          <el-button plain :loading="friendRequestLoading" @click="loadRequests">刷新</el-button>
        </div>

        <el-empty
          v-if="!friendRequestLoading && sortedFriendRequests.length === 0"
          description="暂无好友申请"
        />

        <el-scrollbar v-else max-height="420px">
          <div
            v-for="item in sortedFriendRequests"
            :key="item.id"
            class="friend-request-row"
          >
            <div class="friend-request-main">
              <div class="friend-request-title">
                <strong>
                  {{
                    item.direction === "received"
                      ? `${getFriendLabel(item)} 请求添加你为好友`
                      : `你向 ${getFriendLabel(item)} 发起了好友申请`
                  }}
                </strong>
                <el-tag size="small" :type="getRequestStatusType(item.requestStatus)">
                  {{ friendRequestStatusMap[item.requestStatus] }}
                </el-tag>
              </div>

              <div class="friend-request-meta">账号：{{ getFriendMeta(item) }}</div>
              <div v-if="item.requestMessage" class="friend-request-meta">
                附言：{{ item.requestMessage }}
              </div>
              <div v-if="item.rejectReason" class="friend-request-meta">
                拒绝原因：{{ item.rejectReason }}
              </div>
              <div class="friend-request-meta">申请时间：{{ formatChatTime(item.createdAt) }}</div>
            </div>

            <div
              v-if="item.direction === 'received' && item.requestStatus === 'pending'"
              class="friend-request-ops"
            >
              <el-button
                size="small"
                type="primary"
                :loading="handlingRequestId === item.id"
                @click="handleAcceptRequest(item)"
              >
                通过
              </el-button>
              <el-button
                size="small"
                type="danger"
                plain
                :loading="handlingRequestId === item.id"
                @click="handleRejectRequest(item)"
              >
                拒绝
              </el-button>
            </div>
          </div>
        </el-scrollbar>
      </div>
    </el-dialog>
  </aside>
</template>

<style scoped lang="scss">
.create-box-header {
  align-items: flex-start;
}

.friend-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.create-box-stack {
  display: grid;
  gap: 10px;
}

.member-select {
  width: 100%;
}

.create-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.create-tip {
  min-width: 0;
}

.friend-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.friend-option-meta {
  color: #9ca3af;
  font-size: 12px;
}

.friend-request-panel {
  display: grid;
  gap: 12px;
}

.friend-request-toolbar {
  display: flex;
  justify-content: flex-end;
}

.friend-request-row {
  display: grid;
  gap: 12px;
  padding: 14px 0;
  border-bottom: 1px solid #e5e7eb;
}

.friend-request-row:last-child {
  border-bottom: none;
}

.friend-request-main {
  display: grid;
  gap: 6px;
}

.friend-request-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.friend-request-meta {
  color: #6b7280;
  font-size: 13px;
  line-height: 18px;
}

.friend-request-ops {
  display: flex;
  gap: 8px;
}

@media (max-width: 768px) {
  .create-actions {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
