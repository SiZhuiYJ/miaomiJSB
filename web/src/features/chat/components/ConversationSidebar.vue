<script setup lang="ts">
import ProgressiveAvatar from "@/components/ProgressiveAvatar.vue";
import {
  getConversationAvatarSources as getConversationAssetSources,
  getUserAvatarSources,
} from "@/utils/avatar";
import { computed, onMounted, ref, watch } from "vue";
import {
  createGroupJoinRequest,
  searchGroupConversation,
} from "../api/conversations";
import {
  acceptFriendRequest,
  createFriendRequest,
  getFriendRequests,
  getFriends,
  rejectFriendRequest,
  searchFriendUsers,
} from "../api/friends";
import type {
  ConversationSummary,
  FriendRequest,
  FriendSearchResult,
  Friendship,
  GroupSearchResult,
} from "../types";
import {
  formatChatTime,
  getConversationAvatarSources,
  getConversationAvatarUrl,
  getConversationAvatarText,
  getConversationDisplayTitle,
  getMessagePreview,
} from "../utils/chat";

type SidebarActionCommand = "groupJoin" | "addFriend" | "friendRequest";

const createConversationType = defineModel<"direct" | "group">("createConversationType", {
  required: true,
});
const createTitle = defineModel<string>("createTitle", {
  required: true,
});
const createMemberUserIds = defineModel<number[]>("createMemberUserIds", {
  required: true,
});

const props = defineProps<{
  selectedConversationId: number;
  conversations: ConversationSummary[];
  isMobile: boolean;
  friendRequestVersion: number;
  friendshipVersion: number;
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
const handlingRequestId = ref<number | null>(null);
const friendRequests = ref<FriendRequest[]>([]);
const friendSearchDialogVisible = ref(false);
const friendSearchLoading = ref(false);
const friendSearchSubmittingId = ref<number | null>(null);
const friendSearchKeyword = ref("");
const friendSearchRequestMessage = ref("");
const friendSearchResults = ref<FriendSearchResult[]>([]);
const friendSearchSearched = ref(false);
const groupJoinDialogVisible = ref(false);
const groupSearchLoading = ref(false);
const groupJoinSubmitting = ref(false);
const groupJoinConversationIdInput = ref("");
const groupJoinRequestMessage = ref("");
const groupSearchResult = ref<GroupSearchResult | null>(null);
const groupSearchSearched = ref(false);

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

const pendingFriendRequestCount = computed(
  () =>
    friendRequests.value.filter(
      (item) => item.direction === "received" && item.requestStatus === "pending",
    ).length,
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

const trimmedFriendSearchKeyword = computed(() => friendSearchKeyword.value.trim());

const canCreateConversation = computed(() =>
  createConversationType.value === "direct"
    ? createMemberUserIds.value.length === 1
    : createMemberUserIds.value.length > 0,
);

const canSearchFriend = computed(() => trimmedFriendSearchKeyword.value.length > 0);

const normalizedGroupJoinConversationId = computed(() => {
  const value = Number.parseInt(groupJoinConversationIdInput.value.trim(), 10);
  return Number.isInteger(value) && value > 0 ? value : 0;
});

const canSearchGroup = computed(() => normalizedGroupJoinConversationId.value > 0);

const canSubmitGroupJoin = computed(
  () =>
    groupSearchResult.value !== null &&
    !groupSearchResult.value.isMember &&
    !groupSearchResult.value.hasPendingJoinRequest,
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

watch(
  () => friendSearchKeyword.value,
  () => {
    friendSearchResults.value = [];
    friendSearchSearched.value = false;
  },
);

watch(
  () => groupJoinConversationIdInput.value,
  () => {
    groupSearchResult.value = null;
    groupSearchSearched.value = false;
  },
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

function resetGroupJoinDialog() {
  groupJoinDialogVisible.value = false;
  groupJoinConversationIdInput.value = "";
  groupJoinRequestMessage.value = "";
  groupSearchResult.value = null;
  groupSearchSearched.value = false;
}

function resetFriendSearchDialog() {
  friendSearchDialogVisible.value = false;
  friendSearchKeyword.value = "";
  friendSearchRequestMessage.value = "";
  friendSearchResults.value = [];
  friendSearchSearched.value = false;
  friendSearchSubmittingId.value = null;
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

async function loadRequests(options: { silent?: boolean } = {}) {
  const { silent = false } = options;
  friendRequestLoading.value = true;
  try {
    const { data } = await getFriendRequests();
    friendRequests.value = data;
  } catch (error) {
    if (!silent) {
      ElMessage.error(getErrorMessage(error, "加载好友申请失败"));
    }
  } finally {
    friendRequestLoading.value = false;
  }
}

async function refreshFriendData(includeRequests = friendRequestDialogVisible.value) {
  const tasks: Promise<unknown>[] = [loadFriends()];
  if (includeRequests) {
    tasks.push(loadRequests());
  }
  await Promise.all(tasks);
}

async function openFriendRequestDialog() {
  friendRequestDialogVisible.value = true;
  await refreshFriendData(true);
}

function openAddFriendDialog() {
  resetFriendSearchDialog();
  friendSearchDialogVisible.value = true;
}

function openGroupJoinDialog() {
  resetGroupJoinDialog();
  groupJoinDialogVisible.value = true;
}

function getFriendSearchName(item: FriendSearchResult) {
  return item.nickName?.trim() || item.userAccount?.trim() || String(item.userId);
}

function getFriendSearchMeta(item: FriendSearchResult) {
  return item.userAccount?.trim() || `ID: ${item.userId}`;
}

function escapeHtml(value: string) {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function highlightKeyword(text: string, keyword: string) {
  if (!text || !keyword) return escapeHtml(text);

  const normalizedText = text.toLocaleLowerCase();
  const normalizedKeyword = keyword.toLocaleLowerCase();
  let start = 0;
  let highlighted = "";

  while (start < text.length) {
    const index = normalizedText.indexOf(normalizedKeyword, start);
    if (index < 0) {
      highlighted += escapeHtml(text.slice(start));
      break;
    }

    highlighted += escapeHtml(text.slice(start, index));
    highlighted += `<span class="match-highlight">${escapeHtml(
      text.slice(index, index + keyword.length),
    )}</span>`;
    start = index + keyword.length;
  }

  return highlighted;
}

function renderFriendSearchName(item: FriendSearchResult) {
  const nickName = item.nickName?.trim();
  const keyword = trimmedFriendSearchKeyword.value;

  if (!nickName || !keyword) {
    return escapeHtml(getFriendSearchName(item));
  }

  if (!nickName.toLocaleLowerCase().includes(keyword.toLocaleLowerCase())) {
    return escapeHtml(getFriendSearchName(item));
  }

  return highlightKeyword(nickName, keyword);
}

function getFriendSearchActionText(item: FriendSearchResult) {
  if (item.isFriend) return "已是好友";
  if (item.hasPendingSentRequest) return "已发送申请";
  if (item.hasPendingReceivedRequest) return "同意并加好友";
  return "添加好友";
}

function getGroupSearchTitle(item: GroupSearchResult) {
  return item.title?.trim() || `群聊 #${item.id}`;
}

function getFriendSearchAvatar(item: FriendSearchResult) {
  return getUserAvatarSources(item.userId, item.avatarKey);
}

function getGroupSearchAvatar(item: GroupSearchResult) {
  return getConversationAssetSources(item.id, item.avatarKey);
}

async function executeFriendSearch(options: { silent?: boolean } = {}) {
  const { silent = false } = options;
  const keyword = trimmedFriendSearchKeyword.value;
  if (!keyword) {
    if (!silent) {
      ElMessage.warning("请输入昵称或账号");
    }
    friendSearchResults.value = [];
    friendSearchSearched.value = false;
    return;
  }

  friendSearchLoading.value = true;
  friendSearchSearched.value = true;
  try {
    const { data } = await searchFriendUsers(keyword);
    friendSearchResults.value = data;
  } catch (error) {
    friendSearchResults.value = [];
    if (!silent) {
      ElMessage.error(getErrorMessage(error, "搜索好友失败"));
    }
  } finally {
    friendSearchLoading.value = false;
  }
}

async function handleSearchFriend() {
  await executeFriendSearch();
}

async function handleSendFriendRequest(item: FriendSearchResult) {
  if (
    friendSearchSubmittingId.value !== null ||
    item.isFriend ||
    item.hasPendingSentRequest
  ) {
    return;
  }

  friendSearchSubmittingId.value = item.userId;
  try {
    const { data } = await createFriendRequest({
      receiverUserId: item.userId,
      requestMessage: friendSearchRequestMessage.value.trim() || undefined,
      requestSource: "search",
    });

    ElMessage.success(
      data.requestStatus === "accepted" ? "已自动成为好友" : "好友申请已发送",
    );

    await refreshFriendData(friendRequestDialogVisible.value);
    await executeFriendSearch({ silent: true });
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "发送好友申请失败"));
  } finally {
    friendSearchSubmittingId.value = null;
  }
}

async function executeGroupSearch(options: { silent?: boolean } = {}) {
  const { silent = false } = options;
  const conversationId = normalizedGroupJoinConversationId.value;
  if (!conversationId) {
    if (!silent) {
      ElMessage.warning("请输入有效的群聊 ID");
    }
    groupSearchResult.value = null;
    groupSearchSearched.value = false;
    return;
  }

  groupSearchLoading.value = true;
  groupSearchSearched.value = true;
  try {
    const { data } = await searchGroupConversation(conversationId);
    groupSearchResult.value = data;
  } catch (error) {
    groupSearchResult.value = null;
    if (!silent) {
      ElMessage.error(getErrorMessage(error, "搜索群聊失败"));
    }
  } finally {
    groupSearchLoading.value = false;
  }
}

async function handleSearchGroup() {
  await executeGroupSearch();
}

async function handleSubmitGroupJoin() {
  if (!canSubmitGroupJoin.value || groupJoinSubmitting.value || !groupSearchResult.value) return;

  groupJoinSubmitting.value = true;
  try {
    await createGroupJoinRequest(groupSearchResult.value.id, {
      requestMessage: groupJoinRequestMessage.value.trim() || undefined,
    });
    ElMessage.success("加群申请已提交");
    resetGroupJoinDialog();
  } catch (error) {
    ElMessage.error(getErrorMessage(error, "提交加群申请失败"));
  } finally {
    groupJoinSubmitting.value = false;
  }
}

async function handleAcceptRequest(item: FriendRequest) {
  handlingRequestId.value = item.id;
  try {
    await acceptFriendRequest(item.id);
    ElMessage.success("已通过好友申请");
    await refreshFriendData(true);
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

function handleActionCommand(command: SidebarActionCommand): void {
  switch (command) {
    case "groupJoin":
      openGroupJoinDialog();
      break;
    case "addFriend":
      openAddFriendDialog();
      break;
    case "friendRequest":
      void openFriendRequestDialog();
      break;
  }
}

onMounted(() => {
  void loadFriends();
  void loadRequests({ silent: true });
});

watch(
  () => props.friendshipVersion,
  () => {
    void refreshFriendData(friendRequestDialogVisible.value);
  },
);

watch(
  () => props.friendRequestVersion,
  () => {
    void loadRequests({ silent: !friendRequestDialogVisible.value });
  },
);
</script>

<template>
  <aside class="left-panel">
    <el-scrollbar :view-class="isMobile ? 'mobile' : ''">
      <ul class="conversation-list">
        <li v-for="item in conversations" :key="item.id" class="conversation-item"
          :class="{ active: item.id === selectedConversationId }" @click="emit('selectConversation', item)">
          <ProgressiveAvatar class="conversation-avatar" :src="getConversationAvatarUrl(item)"
            :thumbnail-src="getConversationAvatarSources(item).thumbnailSrc" :size="60" shape="square">
            {{ getConversationAvatarText(item) }}
          </ProgressiveAvatar>
          <el-badge :value="item.unreadCount" :show-zero="false" :max="99" :offset="[-22, 5]"
            :color="item.isMuted ? '#f5f7fa' : ''" class="item">
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

        <el-dropdown placement="bottom-end" trigger="click" @command="handleActionCommand">
          <span class="social-dropdown-trigger">
            <el-badge :value="pendingFriendRequestCount" :show-zero="false" :max="99" :offset="[-6, 6]">
              <el-button color="#111827">
                社交操作
                <el-icon class="el-icon--right">
                  <ArrowDown />
                </el-icon>
              </el-button>
            </el-badge>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item :disabled="friendSearchSubmittingId !== null" command="addFriend">
                添加好友
              </el-dropdown-item>
              <el-dropdown-item :disabled="groupJoinSubmitting" command="groupJoin">
                申请加群
              </el-dropdown-item>
              <el-dropdown-item command="friendRequest">
                <div class="social-dropdown-item">
                  <span>好友申请</span>
                  <el-badge :value="pendingFriendRequestCount" :show-zero="false" :max="99" />
                </div>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>

      <div class="create-box-item">
        <el-select v-model="createConversationType" placeholder="会话类型" class="conversation-type-select">
          <el-option v-for="item in createConversationTypeOptions" :key="item.value" :label="item.label"
            :value="item.value" />
        </el-select>
        <el-input v-model="createTitle" clearable placeholder="群聊标题，可选"
          :disabled="createConversationType === 'direct'" />
      </div>

      <div class="create-box-item create-box-stack">
        <el-select v-if="createConversationType === 'direct'" v-model="directMemberUserId" class="member-select"
          filterable clearable :loading="friendLoading" placeholder="选择一位好友发起单聊">
          <el-option v-for="item in friendOptions" :key="item.userId" :label="item.label" :value="item.userId">
            <span>{{ item.label }}（{{ item.meta }}）</span>
          </el-option>
        </el-select>

        <el-select v-else v-model="createMemberUserIds" class="member-select" multiple filterable clearable
          collapse-tags collapse-tags-tooltip :loading="friendLoading" placeholder="选择好友创建群聊">
          <el-option v-for="item in friendOptions" :key="item.userId" :label="item.label" :value="item.userId">
            <span>{{ item.label }}（{{ item.meta }}）</span>
          </el-option>
        </el-select>

        <!-- <div class="create-actions">
          <el-text class="create-tip" type="info">
            只能从已添加的好友中发起单聊或创建群聊。
          </el-text> -->
        <el-button icon="ChatRound" color="#111827" :disabled="!canCreateConversation"
          @click="emit('createConversation')">
          创建
        </el-button>
        <!-- </div> -->
      </div>
    </div>

    <el-dialog
      v-model="friendSearchDialogVisible"
      title="搜索好友"
      width="min(92vw, 560px)"
      :close-on-click-modal="friendSearchSubmittingId === null"
      :close-on-press-escape="friendSearchSubmittingId === null"
      :show-close="friendSearchSubmittingId === null"
      @closed="resetFriendSearchDialog"
    >
      <div class="friend-search-panel">
        <div class="search-toolbar">
          <el-input
            v-model="friendSearchKeyword"
            clearable
            placeholder="输入昵称或账号搜索用户"
            :disabled="friendSearchLoading || friendSearchSubmittingId !== null"
            @keyup.enter="handleSearchFriend"
          >
            <template #append>
              <el-button
                :loading="friendSearchLoading"
                :disabled="!canSearchFriend || friendSearchSubmittingId !== null"
                @click="handleSearchFriend"
              >
                搜索
              </el-button>
            </template>
          </el-input>
        </div>

        <el-input
          v-model="friendSearchRequestMessage"
          type="textarea"
          :rows="3"
          maxlength="255"
          show-word-limit
          placeholder="好友申请附言（可选）"
          :disabled="friendSearchSubmittingId !== null"
        />

        <p class="search-tip">账号需完全匹配，昵称支持包含搜索，命中部分会高亮显示。</p>

        <el-empty
          v-if="friendSearchSearched && !friendSearchLoading && friendSearchResults.length === 0"
          description="未找到匹配的用户"
        />

        <el-scrollbar v-else-if="friendSearchResults.length > 0" max-height="320px">
          <div v-for="item in friendSearchResults" :key="item.userId" class="search-result-row">
            <div class="search-result-main">
              <ProgressiveAvatar
                class="search-result-avatar"
                :src="getFriendSearchAvatar(item).src"
                :thumbnail-src="getFriendSearchAvatar(item).thumbnailSrc"
                :size="44"
                shape="square"
              >
                {{ getFriendSearchName(item).slice(0, 1) }}
              </ProgressiveAvatar>
              <div class="search-result-text">
                <div class="search-result-title">
                  <strong v-html="renderFriendSearchName(item)"></strong>
                  <el-tag v-if="item.isFriend" size="small" type="success">已是好友</el-tag>
                  <el-tag v-else-if="item.hasPendingSentRequest" size="small" type="warning">已发送申请</el-tag>
                  <el-tag v-else-if="item.hasPendingReceivedRequest" size="small" type="primary">
                    对方已申请
                  </el-tag>
                </div>
                <div class="search-result-meta">账号：{{ getFriendSearchMeta(item) }}</div>
                <div class="search-result-meta">用户 ID：{{ item.userId }}</div>
              </div>
            </div>

            <div class="search-result-ops">
              <el-button
                size="small"
                color="#111827"
                :loading="friendSearchSubmittingId === item.userId"
                :disabled="item.isFriend || item.hasPendingSentRequest"
                @click="handleSendFriendRequest(item)"
              >
                {{ getFriendSearchActionText(item) }}
              </el-button>
            </div>
          </div>
        </el-scrollbar>
      </div>
    </el-dialog>

    <el-dialog v-model="friendRequestDialogVisible" title="好友申请" width="min(92vw, 640px)">
      <div class="friend-request-panel">
        <div class="friend-request-toolbar">
          <el-button plain :loading="friendRequestLoading || friendLoading" @click="refreshFriendData(true)">
            刷新
          </el-button>
        </div>

        <el-empty v-if="!friendRequestLoading && sortedFriendRequests.length === 0" description="暂无好友申请" />

        <el-scrollbar v-else max-height="420px">
          <div v-for="item in sortedFriendRequests" :key="item.id" class="friend-request-row">
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

            <div v-if="item.direction === 'received' && item.requestStatus === 'pending'" class="friend-request-ops">
              <el-button size="small" type="primary" :loading="handlingRequestId === item.id"
                @click="handleAcceptRequest(item)">
                通过
              </el-button>
              <el-button size="small" type="danger" plain :loading="handlingRequestId === item.id"
                @click="handleRejectRequest(item)">
                拒绝
              </el-button>
            </div>
          </div>
        </el-scrollbar>
      </div>
    </el-dialog>
    <el-dialog v-model="groupJoinDialogVisible" title="申请加入群聊" width="min(92vw, 520px)"
      :close-on-click-modal="!groupJoinSubmitting" :close-on-press-escape="!groupJoinSubmitting"
      :show-close="!groupJoinSubmitting" @closed="resetGroupJoinDialog">
      <div class="group-join-panel">
        <div class="search-toolbar">
          <el-input
            v-model="groupJoinConversationIdInput"
            placeholder="输入群聊 ID，精确搜索"
            clearable
            :disabled="groupJoinSubmitting || groupSearchLoading"
            @keyup.enter="handleSearchGroup"
          >
            <template #append>
              <el-button
                :loading="groupSearchLoading"
                :disabled="!canSearchGroup || groupJoinSubmitting"
                @click="handleSearchGroup"
              >
                搜索
              </el-button>
            </template>
          </el-input>
        </div>

        <p class="group-join-tip">仅支持按群聊 ID 完全匹配搜索。</p>

        <el-empty
          v-if="groupSearchSearched && !groupSearchLoading && !groupSearchResult"
          description="未找到匹配的群聊"
        />

        <div v-else-if="groupSearchResult" class="group-search-card">
          <div class="search-result-main">
            <ProgressiveAvatar
              class="search-result-avatar"
              :src="getGroupSearchAvatar(groupSearchResult).src"
              :thumbnail-src="getGroupSearchAvatar(groupSearchResult).thumbnailSrc"
              :size="48"
              shape="square"
            >
              {{ getGroupSearchTitle(groupSearchResult).slice(0, 1) }}
            </ProgressiveAvatar>
            <div class="search-result-text">
              <div class="search-result-title">
                <strong>{{ getGroupSearchTitle(groupSearchResult) }}</strong>
                <el-tag v-if="groupSearchResult.isMember" size="small" type="success">你已在群内</el-tag>
                <el-tag v-else-if="groupSearchResult.hasPendingJoinRequest" size="small" type="warning">
                  申请待处理
                </el-tag>
              </div>
              <div class="search-result-meta">群聊 ID：{{ groupSearchResult.id }}</div>
              <div class="search-result-meta">成员数：{{ groupSearchResult.memberCount }}</div>
            </div>
          </div>
        </div>

        <el-input v-model="groupJoinRequestMessage" type="textarea" :rows="4" maxlength="255" show-word-limit
          placeholder="申请附言（可选）" :disabled="groupJoinSubmitting || !canSubmitGroupJoin" />
        <p class="group-join-tip">搜索到群聊后可提交申请，等待群主或管理员审核。</p>
      </div>

      <template #footer>
        <div class="dialog-footer">
          <el-button :disabled="groupJoinSubmitting" @click="resetGroupJoinDialog">取消</el-button>
          <el-button color="#111827" :loading="groupJoinSubmitting" :disabled="!canSubmitGroupJoin"
            @click="handleSubmitGroupJoin">
            提交申请
          </el-button>
        </div>
      </template>
    </el-dialog>
  </aside>
</template>

<style scoped lang="scss">
.create-box-header {
  justify-content: flex-end;
  // align-items: flex-start;
}

.friend-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.social-dropdown-trigger {
  display: inline-flex;
  align-items: center;
}

.social-dropdown-item {
  min-width: 112px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.friend-search-panel {
  display: grid;
  gap: 14px;
}

.search-toolbar {
  display: flex;
}

.search-tip {
  margin: 0;
  color: #6b7280;
  font-size: 13px;
  line-height: 18px;
}

.search-result-row {
  display: grid;
  gap: 12px;
  padding: 14px 0;
  border-bottom: 1px solid #e5e7eb;
}

.search-result-row:last-child {
  border-bottom: none;
}

.search-result-main {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 12px;
  align-items: center;
}

.search-result-avatar {
  flex-shrink: 0;
}

.search-result-text {
  min-width: 0;
  display: grid;
  gap: 6px;
}

.search-result-title {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.search-result-meta {
  color: #6b7280;
  font-size: 13px;
  line-height: 18px;
  word-break: break-all;
}

:deep(.match-highlight) {
  color: #b91c1c;
  background: #fee2e2;
  border-radius: 4px;
  padding: 0 2px;
}

.search-result-ops {
  display: flex;
  justify-content: flex-end;
}

.group-search-card {
  padding: 14px 16px;
  border: 1px solid #e5e7eb;
  border-radius: 12px;
  background: #f9fafb;
}

.create-box-stack {
  display: flex;
  align-items: center;
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

.group-join-panel {
  display: grid;
  gap: 14px;
}

.group-join-tip {
  margin: 0;
  color: #6b7280;
  font-size: 13px;
  line-height: 18px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

@media (max-width: 768px) {
  .create-actions {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
