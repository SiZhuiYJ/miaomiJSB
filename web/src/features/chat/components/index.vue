<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useAuthStore } from '@/stores'
import { storeToRefs } from "pinia";
const { user } = storeToRefs(useAuthStore());
import { API_BASE_URL } from '@/config'
import router from "@/routers/index";
import API from '../api';
import type { ConversationDetail, ConversationSummary, MessageSummary } from '../types';

const baseURL = ref(API_BASE_URL)
const token = ref(useAuthStore().accessToken);
const meUserId = ref(useAuthStore().user?.userId);

watch(() => user.value?.userId, (val) => {
  meUserId.value = val
})

const conversations = ref<ConversationSummary[]>([]);
const currentConversation = ref<ConversationDetail | null>(null);
const messages = ref<MessageSummary[]>([]);
const loading = ref(false);
const errorMessage = ref('');

const createConversationType = ref<'direct' | 'group'>('direct');
const createTitle = ref('');
const createMembersText = ref('');
const composeText = ref('');

const selectedConversationId = computed(() => currentConversation.value?.id || 0);

async function loadConversations() {
  if (!token.value) return;
  loading.value = true;
  errorMessage.value = '';
  try {
    conversations.value = (await API.getConversations()).data;
  } catch (error: any) {
    errorMessage.value = error?.response?.data?.message || error?.message || '加载会话失败';
  } finally {
    loading.value = false;
  }
}

async function selectConversation(item: ConversationSummary) {
  loading.value = true;
  errorMessage.value = '';
  try {
    currentConversation.value = (await API.getConversation(item.id)).data;
    messages.value = (await API.getMessages(item.id)).data;
    await markRead();
  } catch (error: any) {
    errorMessage.value = error?.response?.data?.message || error?.message || '加载会话失败';
  } finally {
    loading.value = false;
  }
}

async function createConversation() {
  const memberUserIds = createMembersText.value
    .split(',')
    .map((x) => Number(x.trim()))
    .filter((x) => Number.isFinite(x) && x > 0);

  loading.value = true;
  errorMessage.value = '';
  try {
    console.log(createConversationType.value,
      createTitle.value || undefined,
      memberUserIds,)
    const detail = (await API.createConversation({
      conversationType: createConversationType.value,
      title: createTitle.value || undefined,
      memberUserIds,
    })).data;
    await loadConversations();
    await selectConversation({
      id: detail.id,
      conversationType: detail.conversationType,
      title: detail.title,
      avatarKey: detail.avatarKey,
      isActive: detail.isActive,
      updatedAt: detail.updatedAt,
      unreadCount: 0,
      lastMessage: null,
    });
  } catch (error: any) {
    errorMessage.value = error?.response?.data?.message || error?.message || '创建会话失败';
  } finally {
    loading.value = false;
  }
}

async function sendTextMessage() {
  if (!selectedConversationId.value || !composeText.value.trim()) return;
  loading.value = true;
  errorMessage.value = '';
  try {
    await API.sendMessage(selectedConversationId.value, {
      messageType: 'text',
      content: composeText.value.trim(),
    });
    composeText.value = '';
    messages.value = (await API.getMessages(selectedConversationId.value)).data;
    await loadConversations();
  } catch (error: any) {
    errorMessage.value = error?.response?.data?.message || error?.message || '发送消息失败';
  } finally {
    loading.value = false;
  }
}

async function loadMore() {
  if (!selectedConversationId.value || messages.value.length === 0) return;
  const firstMessage = messages.value[0];
  if (!firstMessage) return;
  const older = (await API.getMessages(selectedConversationId.value, firstMessage.id, 20)).data;
  if (older.length > 0) {
    messages.value = [...older, ...messages.value];
  }
}

async function markRead() {
  if (!selectedConversationId.value) return;
  const lastId = messages.value.length > 0 ? messages.value[messages.value.length - 1]?.id : undefined;
  await API.markRead(selectedConversationId.value, lastId);
  await loadConversations();
}

loadConversations();
</script>

<template>
  <div class="chat-page">
    <header class="toolbar">
      <button @click="router.push('/home')">返回</button>
      <input v-model="baseURL" placeholder="API Base URL，例如 http://localhost:5167" />
      <input v-model="token" placeholder="Bearer Token（不需要 Bearer 前缀）" />
      <input v-model.number="meUserId" type="number" placeholder="当前用户ID（用于区分左右气泡）" />
      <button @click="loadConversations">刷新</button>
    </header>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div class="layout">
      <aside class="left-panel">
        <div class="create-box">
          <h3>创建会话</h3>
          <select v-model="createConversationType">
            <option value="direct">direct 双人</option>
            <option value="group">group 多人</option>
          </select>
          <input v-model="createTitle" placeholder="群聊标题（direct 可不填）" />
          <input v-model="createMembersText" placeholder="成员ID，逗号分隔：8,11" />
          <button @click="createConversation">创建</button>
        </div>

        <ul class="conversation-list">
          <li v-for="item in conversations" :key="item.id" :class="{ active: item.id === selectedConversationId }"
            @click="selectConversation(item)">
            <div class="title-row">
              <strong>{{ item.title || `会话 #${item.id}` }}</strong>
              <span v-if="item.unreadCount" class="badge">{{ item.unreadCount }}</span>
            </div>
            <small>{{ item.lastMessage?.content || item.lastMessage?.messageType || '暂无消息' }}</small>
          </li>
        </ul>
      </aside>

      <main class="main-panel">
        <div v-if="currentConversation" class="conversation-detail">
          <div class="chat-header">
            <h3>{{ currentConversation.title || `会话 #${currentConversation.id}` }}</h3>
            <p class="members">成员：{{currentConversation.members.map((m) => `${m.nickName ||
              m.userId}(${m.memberRole})`).join('、')}}</p>
            <button class="load-more" @click="loadMore">加载更早消息</button>
          </div>
          <el-scrollbar ref="scrollbarRef" wrap-style="height: calc(100vh - 232px)" view-class="message-list"
            view-style="">
            <div v-for="msg in messages" :key="msg.id"
              :class="['msg-item', { mine: meUserId && msg.senderUserId === meUserId }]">
              <div class="meta">#{{ msg.id }} · {{ msg.senderNickName || msg.senderUserId }} · {{ msg.messageType }}
              </div>
              <div class="bubble">{{ msg.content || msg.extra || '[空消息]' }}</div>
            </div>
          </el-scrollbar>

          <!-- <div class="message-list">
            <div v-for="msg in messages" :key="msg.id"
              :class="['msg-item', { mine: meUserId && msg.senderUserId === meUserId }]">
              <div class="meta">#{{ msg.id }} · {{ msg.senderNickName || msg.senderUserId }} · {{ msg.messageType }}
              </div>
              <div class="bubble">{{ msg.content || msg.extra || '[空消息]' }}</div>
            </div>
          </div> -->

          <div class="composer">
            <input v-model="composeText" placeholder="输入文本消息" @keyup.enter="sendTextMessage" />
            <button :disabled="loading" @click="sendTextMessage">发送</button>
            <button :disabled="loading" @click="markRead">标记已读</button>
          </div>
        </div>

        <div v-else class="empty">请选择会话或新建会话</div>
      </main>
    </div>
  </div>
</template>
