import { computed, onBeforeUnmount, ref } from 'vue';
import { loadSignalR } from '../libs/signalrClient';

interface PushOptions {
  fetchConversations: () => Promise<void>;
  pullLatestMessages: () => Promise<void>;
  markRead: () => Promise<void>;
  hasConversation: () => boolean;
  getConversationId: () => number;
  getToken: () => string;
  getBaseUrl: () => string;
}

export function useChatPush(options: PushOptions) {
  const polling = ref(false);
  const realtimeConnected = ref(false);
  const lastSyncAt = ref<string>('');
  const syncError = ref('');
  let timer: ReturnType<typeof setInterval> | null = null;
  let connection: any = null;

  const statusText = computed(() => {
    if (syncError.value) return `推送异常：${syncError.value}`;
    if (realtimeConnected.value) return 'SignalR 实时连接中';
    if (!polling.value) return '推送已停止';
    if (!lastSyncAt.value) return '轮询连接中...';
    return `轮询推送中（最近同步：${new Date(lastSyncAt.value).toLocaleTimeString()}）`;
  });

  async function tick() {
    try {
      await options.fetchConversations();
      if (options.hasConversation()) {
        await options.pullLatestMessages();
        await options.markRead();
      }
      lastSyncAt.value = new Date().toISOString();
      syncError.value = '';
    } catch (error: any) {
      syncError.value = error?.response?.data?.message || error?.message || '同步失败';
    }
  }

  function startPolling() {
    if (timer) return;
    polling.value = true;
    void tick();
    timer = setInterval(() => {
      void tick();
    }, 5000);
  }

  function stopPolling() {
    polling.value = false;
    if (timer) {
      clearInterval(timer);
      timer = null;
    }
  }

  async function connectRealtime() {
    const token = options.getToken();
    if (!token) throw new Error('缺少登录令牌，无法建立 SignalR 连接');

    const signalR = await loadSignalR();
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${options.getBaseUrl()}/hubs/chat`, {
        accessTokenFactory: () => options.getToken(),
        transport:
          signalR.HttpTransportType.WebSockets |
          signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('chat:message-updated', async (payload: any) => {
      await options.fetchConversations();
      if (payload?.conversationId === options.getConversationId()) {
        await options.pullLatestMessages();
        await options.markRead();
      }
    });

    connection.onclose(() => {
      realtimeConnected.value = false;
      startPolling();
    });

    await connection.start();
    realtimeConnected.value = true;
    stopPolling();

    await subscribeConversation(options.getConversationId());
  }

  async function subscribeConversation(conversationId: number) {
    if (!conversationId || !connection || !realtimeConnected.value) return;
    try {
      await connection.invoke('SubscribeConversation', conversationId);
    } catch (error: any) {
      syncError.value = error?.message || '订阅会话失败';
    }
  }

  async function disconnectRealtime() {
    if (!connection) return;
    try {
      await connection.stop();
    } finally {
      connection = null;
      realtimeConnected.value = false;
    }
  }

  async function startPush() {
    try {
      await connectRealtime();
      syncError.value = '';
    } catch (error: any) {
      syncError.value =
        error?.message || 'SignalR 不可用，已自动降级为轮询模式';
      startPolling();
    }
  }

  async function stopPush() {
    stopPolling();
    await disconnectRealtime();
  }

  async function togglePush() {
    if (polling.value || realtimeConnected.value) {
      await stopPush();
    } else {
      await startPush();
    }
  }

  onBeforeUnmount(() => {
    void stopPush();
  });

  return {
    polling,
    realtimeConnected,
    syncError,
    statusText,
    startPush,
    stopPush,
    togglePush,
    subscribeConversation,
  };
}
