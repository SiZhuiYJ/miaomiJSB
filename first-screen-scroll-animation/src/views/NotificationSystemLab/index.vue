<script setup lang="ts">
import { computed, nextTick, reactive, ref } from "vue";
import type { ComponentPublicInstance } from "vue";
import gsap from "gsap";
import MessageItem from "./messageItem.vue";
import type {
  NotificationMessage,
} from "./types.ts";

const messages = ref<NotificationMessage[]>([]);
const itemRefs = ref<Record<number, HTMLElement>>({});

let nextMessageId = 0;

const activeMessages = computed(() => messages.value.slice(0, 5));

const setItemRef = (
  el: Element | ComponentPublicInstance | null,
  id: number,
) => {
  if (el instanceof HTMLElement) {
    itemRefs.value[id] = el;
    return;
  }

  delete itemRefs.value[id];
};

const animateLayout = (callback: () => void) => {
  const state = new Map<number, number>();

  activeMessages.value.forEach((msg) => {
    const el = itemRefs.value[msg.id];
    if (el) state.set(msg.id, el.getBoundingClientRect().top);
  });

  callback();

  nextTick(() => {
    state.forEach((oldTop, id) => {
      const el = itemRefs.value[id];
      if (!el) return;

      const newTop = el.getBoundingClientRect().top;
      const deltaY = oldTop - newTop;

      if (deltaY !== 0) {
        gsap.fromTo(
          el,
          { y: deltaY },
          { y: 0, duration: 0.5, ease: "power4.out" },
        );
      }
    });
  });
};

const startCountdown = (message: NotificationMessage, duration: number) => {
  message.progress = 1;
  message.tween = gsap.to(message, {
    progress: 0,
    duration: duration / 1000,
    ease: "none",
    onComplete: () => removeMessage(message.id),
  });
};

const addMessage = (options: {
  content: string;
  color?: string;
  duration?: number;
  closable?: boolean;
  direction?: NotificationMessage["direction"];
}) => {
  const {
    content,
    color = "#3b82f6",
    duration = 500000,
    closable = true,
    direction = "rtl",
  } = options;

  const existing = messages.value.find(
    (m) =>
      m.content === content &&
      m.color.toLowerCase() === color.toLowerCase() &&
      !m.isRemoving,
  );

  if (existing) {
    existing.count++;
    existing.tween?.kill();
    startCountdown(existing, duration);

    const el = itemRefs.value[existing.id];
    if (el) {
      const bodyEl = el.querySelector<HTMLElement>('.message-body');
      if (bodyEl) {
        gsap.to(bodyEl,
          {
            scale: 1.05,
            duration: 0.1,
            yoyo: true,
            repeat: 1
          });
      }
    }

    return;
  }

  animateLayout(() => {
    const id = ++nextMessageId;
    const newMessage = reactive<NotificationMessage>({
      id,
      content,
      color: color,
      duration,
      closable: closable,
      direction: direction,
      count: 1,
      progress: 1,
      isRemoving: false,
      tween: null,
    });

    messages.value.unshift(newMessage);

    // 下一帧添加动画
    nextTick(() => {
      const el = itemRefs.value[id];
      if (!el) return;

      const bodyEl = el.querySelector<HTMLElement>('.message-body');
      if (!bodyEl) return;
      gsap.fromTo(
        bodyEl,
        {
          y: -20,
          opacity: 0,
          scale: 0.9,
          filter: "blur(4px)"
        },
        {
          y: 0,
          opacity: 1,
          scale: 1,
          filter: "blur(0px)",
          duration: 0.6,
          ease: "back.out(3)",
          onComplete: () => {
            startCountdown(newMessage, duration);
          }
        },
      );
    });
  });
};

const removeMessage = (id: number) => {
  const index = messages.value.findIndex((m) => m.id === id);
  if (index === -1) return;

  const message = messages.value[index];
  if (!message || message.isRemoving) return;

  animateLayout(() => {
    message.isRemoving = true;
    message.tween?.kill();

    const el = itemRefs.value[id];
    if (!el) {
      messages.value = messages.value.filter((m) => m.id !== id);
      return;
    }
    const bodyEl = el.querySelector<HTMLElement>('.message-body');

    if (!bodyEl) {
      messages.value = messages.value.filter((m) => m.id !== id);
      return;
    }

    const tl = gsap.timeline({
      onComplete: () => {
        messages.value = messages.value.filter((m) => m.id !== id);
      },
    });

    tl.to(bodyEl,
      {
        y: 0,
        scale: 0.95,
        opacity: 0,
        filter: "blur(4px)",
        duration: 0.3,
        ease: "power2.in",
      }).to(bodyEl, {
        y: -20,
        height: 0,
        marginBottom: 0,
        duration: 0.2
      }, "-=0.1");
  });
};

defineExpose({
  addMessage,
  removeMessage,
});
</script>

<template>
  <Teleport to="body">
    <div class="notification-container">
      <MessageItem v-for="(msg, index) in activeMessages" :key="msg.id" :message="msg" :index="index"
        @remove="removeMessage" @set-item-ref="setItemRef" />

      <div v-if="messages.length > 5" class="hidden-count-badge">
        + {{ messages.length - 5 }} 条未显示通知
      </div>
    </div>
  </Teleport>
</template>

<style scoped lang="scss">
.notification-container {
  --item-height: 44px;
  --item-gap: 8px;
  --main-radius: 22px;
  --slate-50: #f8fafc;
  --slate-100: #f1f5f9;
  --slate-200: #e2e8f0;
  --slate-300: #cbd5e1;
  --slate-400: #94a3b8;
  --slate-500: #64748b;
  --slate-800: #1e293b;
  --slate-900: #0f172a;
  --blue-500: #3b82f6;
  --blue-600: #2563eb;
  --blue-700: #1d4ed8;
  --rose-50: #fff1f2;
  --rose-600: #e11d48;
  --emerald-50: #ecfdf5;
  --emerald-600: #059669;
  --purple-50: #f5f3ff;
  --purple-600: #7c3aed;

  position: absolute;
  top: 0;
  display: flex;
  justify-content: center;
  z-index: 9999;
  width: 100%;
}

.hidden-count-badge {
  position: absolute;
  top: calc(var(--item-height) * 5 + var(--item-gap));
  font-size: 10px;
  color: var(--slate-400);
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 9999px;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
}
</style>