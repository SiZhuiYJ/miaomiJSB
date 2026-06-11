<script setup lang="ts">
import { computed, nextTick, reactive, ref } from "vue";
import type { ComponentPublicInstance, CSSProperties } from "vue";
import gsap from "gsap";

type NotificationDirection =
  | "ltr"
  | "rtl"
  | "ttb"
  | "btt"
  | "center"
  | "vSplit"
  | "ripple"
  | "spotlight"
  | "fade";

interface NotificationMessage {
  id: number;
  content: string;
  color: string;
  duration: number;
  closable: boolean;
  direction: NotificationDirection;
  count: number;
  progress: number;
  isRemoving: boolean;
  tween: gsap.core.Tween | null;
}

interface NotificationForm {
  content: string;
  color: string;
  duration: number;
  closable: boolean;
  direction: NotificationDirection;
}

const messages = ref<NotificationMessage[]>([]);
const itemRefs = ref<Record<number, HTMLElement>>({});
const form = reactive<NotificationForm>({
  content: "新增：纵向双向收拢动画",
  color: "#f43f5e",
  duration: 5000,
  closable: true,
  direction: "vSplit",
});

const directions: Array<{ n: string; v: NotificationDirection }> = [
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

const quick = (
  content: string,
  color: string,
  direction: NotificationDirection,
) => {
  form.content = content;
  form.color = color;
  form.direction = direction;
};

const getLuminance = (hex: string) => {
  const normalized = hex.replace("#", "");
  const r = parseInt(normalized.slice(0, 2), 16);
  const g = parseInt(normalized.slice(2, 4), 16);
  const b = parseInt(normalized.slice(4, 6), 16);

  return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
};

const getDynamicTextColor = (msg: NotificationMessage) => {
  const luminance = getLuminance(msg.color);
  return luminance < 0.6 ? "#ffffff" : "#1e293b";
};

const getBadgeBg = (msg: NotificationMessage) =>
  getDynamicTextColor(msg) === "#ffffff"
    ? "rgba(255, 255, 255, 0.2)"
    : "rgba(0, 0, 0, 0.06)";

const getItemBaseStyle = (msg: NotificationMessage, index: number) =>
  ({
    color: msg.color,
    zIndex: 100 - index,
  }) as CSSProperties;

const getProgressStyle = (msg: NotificationMessage) => {
  const style: CSSProperties = {
    transform: "scale(1)",
    transformOrigin: "left center",
    opacity: 1,
    filter: "none",
  };

  switch (msg.direction) {
    case "ltr":
      style.transform = `scaleX(${msg.progress})`;
      style.transformOrigin = "left center";
      break;
    case "rtl":
      style.transform = `scaleX(${msg.progress})`;
      style.transformOrigin = "right center";
      break;
    case "ttb":
      style.transform = `scaleY(${msg.progress})`;
      style.transformOrigin = "center top";
      break;
    case "btt":
      style.transform = `scaleY(${msg.progress})`;
      style.transformOrigin = "center bottom";
      break;
    case "center":
      style.transform = `scaleX(${msg.progress})`;
      style.transformOrigin = "center center";
      break;
    case "vSplit":
      style.transform = `scaleY(${msg.progress})`;
      style.transformOrigin = "center center";
      break;
    case "ripple":
    case "spotlight":
      style.transform = `scaleX(${msg.progress})`;
      style.transformOrigin = "left center";
      break;
    case "fade":
      style.opacity = msg.progress;
      style.transform = `scale(${0.98 + 0.02 * msg.progress})`;
      style.filter = `blur(${(1 - msg.progress) * 2}px)`;
      break;
  }

  return style;
};

const getSpotlightStyle = (msg: NotificationMessage) =>
  ({
    left: `${msg.progress * 100}%`,
    transform: "translateX(-100%)",
    opacity: msg.progress > 0.1 ? 1 : 0,
    transition: "opacity 0.3s",
  }) as CSSProperties;

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
    <div class="notification-container">
      <div
        v-for="(msg, index) in activeMessages"
        :key="msg.id"
        :ref="(el) => setItemRef(el, msg.id)"
        class="notification-item"
        :style="getItemBaseStyle(msg, index)"
      >
        <div class="bg-layer-persistent"></div>
        <div class="fg-layer-progress" :style="getProgressStyle(msg)"></div>
        <div
          v-if="msg.direction === 'spotlight'"
          class="spotlight-glow"
          :style="getSpotlightStyle(msg)"
        ></div>

        <div
          class="content-wrapper"
          :style="{ color: getDynamicTextColor(msg) }"
        >
          <div v-if="msg.closable" class="spacer"></div>
          <span class="text-content" :title="msg.content">
            {{ msg.content }}
          </span>
          <span
            v-if="msg.count > 1"
            class="count-badge"
            :style="{ backgroundColor: getBadgeBg(msg) }"
          >
            {{ msg.count }}
          </span>
          <button
            v-if="msg.closable"
            class="close-btn"
            type="button"
            aria-label="关闭通知"
            @click="removeMessage(msg.id)"
          >
            ×
          </button>
        </div>
      </div>

      <div v-if="messages.length > 5" class="hidden-count-badge">
        + {{ messages.length - 5 }} 条未显示通知
      </div>
    </div>

    <section class="page-container">
      <div class="control-panel">
        <h2 class="panel-header">
          <span class="status-dot"></span>
          动画实验室 v2.2
        </h2>

        <div class="form-group">
          <div>
            <label class="label" for="notification-content">通知内容</label>
            <textarea
              id="notification-content"
              v-model="form.content"
              class="input-textarea"
            ></textarea>
          </div>

          <div class="grid-2">
            <div>
              <label class="label" for="notification-color">主题色</label>
              <div class="color-input-container">
                <input
                  id="notification-color"
                  v-model="form.color"
                  type="color"
                  class="color-input"
                />
              </div>
            </div>
            <div>
              <label class="label" for="notification-duration">持续 (ms)</label>
              <input
                id="notification-duration"
                v-model.number="form.duration"
                type="number"
                min="1"
                class="input-number"
              />
            </div>
          </div>

          <div>
            <span class="label">进度动画风格</span>
            <div class="grid-3">
              <button
                v-for="opt in directions"
                :key="opt.v"
                type="button"
                :class="['style-btn', form.direction === opt.v ? 'active' : '']"
                @click="form.direction = opt.v"
              >
                {{ opt.n }}
              </button>
            </div>
          </div>

          <button type="button" class="submit-btn" @click="addMessage">
            发送通知
          </button>
        </div>

        <div class="quick-actions">
          <button
            type="button"
            class="quick-btn quick-rose"
            @click="quick('纵向双向收拢测试', '#f43f5e', 'vSplit')"
          >
            纵向双收
          </button>
          <button
            type="button"
            class="quick-btn quick-emerald"
            @click="quick('横向双向收拢测试', '#10b981', 'center')"
          >
            横向双收
          </button>
          <button
            type="button"
            class="quick-btn quick-purple"
            @click="quick('精致光效聚焦', '#8b5cf6', 'spotlight')"
          >
            聚光灯
          </button>
        </div>
      </div>
    </section>
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

.notification-container {
  position: fixed;
  top: env(safe-area-inset-top, 20px);
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

.notification-item {
  pointer-events: auto;
  position: relative;
  margin-bottom: 8px;
  padding: 8px 20px;
  border-radius: var(--main-radius);
  width: fit-content;
  max-width: calc(100vw - 40px);
  min-width: 140px;
  min-height: var(--item-height);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 20px -6px rgba(0, 0, 0, 0.12);
  user-select: none;
  background: #fff;
  overflow: hidden;
  will-change: transform, opacity;
}

.bg-layer-persistent {
  position: absolute;
  inset: 0;
  z-index: 1;
  opacity: 0.15;
  background-color: currentColor;
  border-radius: var(--main-radius);
}

.fg-layer-progress {
  position: absolute;
  inset: 0;
  z-index: 2;
  background-color: currentColor;
  border-radius: var(--main-radius);
  will-change: transform, opacity, filter;
}

.spotlight-glow {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 60px;
  background: linear-gradient(
    90deg,
    transparent,
    rgba(255, 255, 255, 0.6),
    transparent
  );
  z-index: 3;
  pointer-events: none;
}

.content-wrapper {
  position: relative;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  transition: color 0.5s ease;
  min-width: 0;
}

.text-content {
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  letter-spacing: 0;
  flex: 1 1 auto;
  min-width: 0;
}

.spacer {
  width: 18px;
  flex-shrink: 0;
}

.count-badge {
  font-weight: 900;
  padding: 1px 6px;
  border-radius: 8px;
  font-size: 0.7rem;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
  flex-shrink: 0;
}

.close-btn {
  cursor: pointer;
  opacity: 0.5;
  font-size: 1rem;
  transition: all 0.2s;
  width: 18px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  padding: 0;
  border: 0;
  color: inherit;
  background: transparent;
}

.close-btn:hover {
  opacity: 1;
  transform: rotate(90deg) scale(1.2);
}

.page-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  box-sizing: border-box;
}

.control-panel {
  background: #fff;
  border-radius: 24px;
  border: 1px solid var(--slate-100);
  padding: 32px;
  width: 100%;
  max-width: 28rem;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  overflow-y: auto;
  max-height: 90vh;
  box-sizing: border-box;
}

.panel-header {
  font-size: 1.25rem;
  font-weight: 900;
  margin-bottom: 1.5rem;
  color: var(--slate-800);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.status-dot {
  width: 0.625rem;
  height: 0.625rem;
  background-color: var(--blue-500);
  border-radius: 9999px;
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.label {
  font-size: 10px;
  font-weight: 900;
  color: var(--slate-400);
  text-transform: uppercase;
  letter-spacing: 0.1em;
  margin-bottom: 0.5rem;
  display: block;
}

.input-textarea {
  width: 100%;
  background-color: var(--slate-50);
  border: none;
  border-radius: 1rem;
  padding: 1rem;
  font-size: 0.875rem;
  outline: none;
  transition: all 0.15s ease-in-out;
  resize: none;
  height: 4rem;
  box-sizing: border-box;
  font-family: inherit;
}

.input-textarea:focus {
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.2);
}

.grid-2 {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}

.color-input-container {
  display: flex;
  align-items: center;
  background-color: var(--slate-50);
  border-radius: 1rem;
  padding: 0.375rem;
  height: 3rem;
  box-sizing: border-box;
}

.color-input {
  width: 100%;
  height: 100%;
  cursor: pointer;
  background-color: transparent;
  border: none;
  padding: 0;
}

.input-number {
  width: 100%;
  height: 3rem;
  background-color: var(--slate-50);
  border: none;
  border-radius: 1rem;
  padding: 0 1rem;
  font-size: 0.875rem;
  outline: none;
  box-sizing: border-box;
  font-family: inherit;
}

.grid-3 {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.5rem;
}

.style-btn {
  font-size: 10px;
  padding: 0.625rem 0;
  border-radius: 0.75rem;
  border: 1px solid var(--slate-200);
  transition: all 0.3s;
  background-color: #fff;
  color: var(--slate-500);
  cursor: pointer;
  width: 100%;
}

.style-btn:hover {
  border-color: var(--slate-300);
}

.style-btn.active {
  background-color: var(--blue-600);
  border-color: var(--blue-600);
  color: #fff;
  font-weight: 700;
  box-shadow:
    0 10px 15px -3px rgba(0, 0, 0, 0.1),
    0 4px 6px -2px rgba(0, 0, 0, 0.05);
}

.submit-btn {
  width: 100%;
  background-color: var(--blue-600);
  color: #fff;
  padding: 1rem 0;
  border-radius: 1rem;
  font-weight: 900;
  font-size: 0.875rem;
  border: none;
  cursor: pointer;
  transition: all 0.15s;
  box-shadow: 0 10px 20px -5px rgba(37, 99, 235, 0.4);
}

.submit-btn:hover {
  background-color: var(--blue-700);
}

.submit-btn:active {
  transform: scale(0.96);
}

.quick-actions {
  margin-top: 2rem;
  padding-top: 1.5rem;
  border-top: 1px solid var(--slate-100);
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  justify-content: center;
}

.quick-btn {
  font-size: 10px;
  font-weight: 700;
  padding: 0.5rem 1rem;
  border-radius: 0.75rem;
  border: none;
  cursor: pointer;
}

.quick-rose {
  background-color: var(--rose-50);
  color: var(--rose-600);
}

.quick-emerald {
  background-color: var(--emerald-50);
  color: var(--emerald-600);
}

.quick-purple {
  background-color: var(--purple-50);
  color: var(--purple-600);
}

.hidden-count-badge {
  font-size: 10px;
  color: var(--slate-400);
  margin-top: 0.5rem;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 9999px;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
}

@media (max-width: 640px) {
  .notification-item {
    padding: 6px 16px;
    min-height: 40px;
    min-width: 120px;
  }

  .control-panel {
    padding: 24px;
  }
}

@keyframes pulse {
  0%,
  100% {
    opacity: 1;
  }

  50% {
    opacity: 0.5;
  }
}
</style>
