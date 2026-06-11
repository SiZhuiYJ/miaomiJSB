<script setup lang="ts">
import { computed, nextTick, reactive, ref } from "vue";
import gsap from "gsap";
import MessageItem from "./MessageItem.vue";
import type { NotificationMessage } from "./types";

const messages = ref<NotificationMessage[]>([]);
const itemRefs = ref<Record<number, HTMLElement>>({});

let messageIdCounter = 0;

const activeMessages = computed(() => messages.value.slice(0, 5));

const setItemRef = (el: HTMLElement | null, id: number) => {
  if (el) itemRefs.value[id] = el;
  else delete itemRefs.value[id];
};

const animateLayout = (callback: () => void) => {
  const state = new Map<number, number>();
  activeMessages.value.forEach((msg) => {
    const el = itemRefs.value[msg.id];
    if (el) state.set(msg.id, el.getBoundingClientRect().top);
  });

  callback();

  nextTick(() => {
    const moves: Array<{
      id: number;
      el: HTMLElement;
      deltaY: number;
      distance: number;
    }> = [];

    state.forEach((oldTop, id) => {
      const el = itemRefs.value[id];
      if (!el) return;

      const newTop = el.getBoundingClientRect().top;
      const deltaY = oldTop - newTop;
      if (deltaY !== 0) {
        moves.push({ id, el, deltaY, distance: Math.abs(deltaY) });
      }
    });

    if (moves.length > 0) {
      moves.sort((a, b) => b.distance - a.distance);
      const tl = gsap.timeline({
        defaults: { duration: 0.46, ease: "power3.out", force3D: true },
      });

      moves.forEach((m, idx) => {
        const staggerDelay = Math.min(0.08, idx * 0.02);
        tl.fromTo(
          m.el,
          { y: m.deltaY, opacity: 0.95, filter: "blur(2px)" },
          {
            y: 0,
            opacity: 1,
            filter: "blur(0px)",
            onStart: () => (m.el.style.willChange = "transform, opacity"),
            onComplete: () => (m.el.style.willChange = ""),
          },
          Math.max(0, staggerDelay - 0.06),
        );
      });
    }
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
    duration = 5000,
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
      gsap.to(el, { scale: 1.05, duration: 0.1, yoyo: true, repeat: 1 });
    }
    return;
  }

  animateLayout(() => {
    const id = ++messageIdCounter;
    const newMessage = reactive<NotificationMessage>({
      id,
      content,
      color,
      duration,
      closable,
      direction,
      count: 1,
      progress: 1,
      isRemoving: false,
      tween: null,
    });

    messages.value.unshift(newMessage);

    nextTick(() => {
      const el = itemRefs.value[id];
      if (!el) return;

      gsap.fromTo(
        el,
        { y: -80, opacity: 0, scale: 0.96, filter: "blur(6px)" },
        {
          y: 0,
          opacity: 1,
          scale: 1,
          filter: "blur(0px)",
          duration: 0.52,
          ease: "power4.out",
        },
      );
      startCountdown(newMessage, duration);
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

    const tl = gsap.timeline({
      onComplete: () => {
        messages.value = messages.value.filter((m) => m.id !== id);
      },
    });

    tl.to(el, {
      scale: 0,
      opacity: 0,
      filter: "blur(6px)",
      paddingTop: 0,
      paddingBottom: 0,
      transformOrigin: "center center",
      duration: 0.35,
      ease: "power2.in",
    }).to(
      el,
      { height: 0, marginBottom: 0, duration: 0.25, ease: "power2.in" },
      "-=0.15",
    );
  });
};

defineExpose({
  addMessage,
  removeMessage,
});
</script>

<template>
  <div class="notification-container">
    <MessageItem
      v-for="(msg, index) in activeMessages"
      :key="msg.id"
      :message="msg"
      :index="index"
      @register="setItemRef"
      @close="removeMessage"
    />

    <div v-if="messages.length > 5" class="hidden-count-badge">
      + {{ messages.length - 5 }} 条未显示通知
    </div>
  </div>
</template>

<style scoped lang="scss">
.notification-container {
  position: fixed;
  top: 20px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 9999;
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  pointer-events: none;
  padding: 12px;
  box-sizing: border-box;
}

.hidden-count-badge {
  font-size: 10px;
  color: #94a3b8;
  margin-top: 0.5rem;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 9999px;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
}
</style>
