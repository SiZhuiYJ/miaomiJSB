<script setup lang="ts">
import type { ComponentPublicInstance, CSSProperties } from "vue";
import type { NotificationMessage } from "../types";

interface Props {
  messages: NotificationMessage[];
  activeMessages: NotificationMessage[];
}

defineProps<Props>();

const emit = defineEmits<{
  remove: [id: number];
  setItemRef: [el: Element | ComponentPublicInstance | null, id: number];
}>();

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
</script>

<template>
  <div class="notification-container">
    <div
      v-for="(msg, index) in activeMessages"
      :key="msg.id"
      :ref="(el) => emit('setItemRef', el, msg.id)"
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

      <div class="content-wrapper" :style="{ color: getDynamicTextColor(msg) }">
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
          @click="emit('remove', msg.id)"
        >
          ×
        </button>
      </div>
    </div>

    <div v-if="messages.length > 5" class="hidden-count-badge">
      + {{ messages.length - 5 }} 条未显示通知
    </div>
  </div>
</template>

<style scoped lang="scss">
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
}
</style>
