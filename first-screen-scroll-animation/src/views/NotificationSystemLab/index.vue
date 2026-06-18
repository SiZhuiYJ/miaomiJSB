<script setup lang="ts">
import { computed, nextTick, reactive, ref } from "vue";
import type { ComponentPublicInstance } from "vue";
import gsap from "gsap";
import NotificationControlPanel from "./components/NotificationControlPanel.vue";
import NotificationMessageList from "./components/NotificationMessageList.vue";
import type {
  NotificationDirectionOption,
  NotificationForm,
  NotificationMessage,
} from "./types";

const messages = ref<NotificationMessage[]>([]);
const itemRefs = ref<Record<number, HTMLElement>>({});
const form = reactive<NotificationForm>({
  content: "新增：纵向双向收拢动画",
  color: "#f43f5e",
  duration: 5000,
  closable: true,
  direction: "vSplit",
});

const directions: NotificationDirectionOption[] = [
  { n: "左向右收", v: "ltr" },
  { n: "右向左收", v: "rtl" },
  { n: "上向下收", v: "ttb" },
  { n: "下向上收", v: "btt" },
  { n: "横向双收", v: "center" },
  { n: "纵向双收", v: "vSplit" },
  { n: "波纹推移", v: "ripple" },
  { n: "聚光灯", v: "spotlight" },
  { n: "柔和消融", v: "fade" },
];

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

const addMessage = () => {
  const content = form.content.trim();
  if (!content) return;

  const duration = Math.max(1, Number(form.duration) || 5000);
  const existing = messages.value.find(
    (m) =>
      m.content === content &&
      m.color.toLowerCase() === form.color.toLowerCase() &&
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
    const id = ++nextMessageId;
    const newMessage = reactive<NotificationMessage>({
      id,
      content,
      color: form.color,
      duration,
      closable: form.closable,
      direction: form.direction,
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
        { y: -20, opacity: 0, scale: 0.9, filter: "blur(4px)" },
        {
          y: 0,
          opacity: 1,
          scale: 1,
          filter: "blur(0px)",
          duration: 0.6,
          ease: "back.out(1.6)",
        },
      );
      startCountdown(newMessage, duration);
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

    const tl = gsap.timeline({
      onComplete: () => {
        messages.value = messages.value.filter((m) => m.id !== id);
      },
    });

    tl.to(el, {
      scale: 0.95,
      opacity: 0,
      filter: "blur(4px)",
      duration: 0.3,
      ease: "power2.in",
    }).to(el, { height: 0, marginBottom: 0, duration: 0.2 }, "-=0.1");
  });
};
</script>

<template>
  <main class="notification-system-lab">
    <NotificationMessageList :messages="messages" :active-messages="activeMessages" @remove="removeMessage"
      @set-item-ref="setItemRef" />
    <NotificationControlPanel v-model="form" :directions="directions" @submit="addMessage" />
  </main>
</template>

<style scoped lang="scss">
.notification-system-lab {
  --item-height: 44px;
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

  min-height: 100vh;
  background-color: var(--slate-50);
  color: var(--slate-900);
  font-family:
    ui-sans-serif,
    system-ui,
    -apple-system,
    BlinkMacSystemFont,
    "Segoe UI",
    Roboto,
    "Helvetica Neue",
    Arial,
    sans-serif;
}
</style>
