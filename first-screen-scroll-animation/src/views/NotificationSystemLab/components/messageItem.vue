<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, useTemplateRef } from "vue";
import type { CSSProperties } from "vue";
import type { NotificationMessage } from "../types";

interface Props {
  message: NotificationMessage;
  index: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  remove: [id: number];
  setItemRef: [el: HTMLElement | null, id: number];
}>();

const itemElement = useTemplateRef("itemElement");

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

// SVG blob 初始滤镜状态：
// - blur 决定融合模糊强度；
// - alpha/offset 是 feColorMatrix 的 alpha 通道参数，用来制造“粘连”效果。
const filterState = {
  blur: 10,
  alpha: 20,
  offset: -9,
};

// 生成 feColorMatrix values。
// 当 alpha=1 且 offset=0 时，这个矩阵等价于不改变透明度，也就是“无滤镜”状态的一部分。
const getMatrixValues = (alpha: number, offset: number): string => `
    1 0 0 0 0
    0 1 0 0 0
    0 0 1 0 0
    0 0 0 ${alpha.toFixed(3)} ${offset.toFixed(3)}
`;

const initialMatrixValues = getMatrixValues(filterState.alpha, filterState.offset);

// 将当前 JS 状态写回对应 SVG filter 节点。
const applyMessageFilterState = (state: typeof filterState) => {
  const blurElement = document.getElementById(`${props.message.id}-blur`);
  const matrixElement = document.getElementById(`${props.message.id}-matrix`);

  blurElement?.setAttribute('stdDeviation', state.blur.toFixed(3));
  matrixElement?.setAttribute('values', getMatrixValues(state.alpha, state.offset));
};


onMounted(() => {
  emit("setItemRef", itemElement.value, props.message.id);
});

onBeforeUnmount(() => {
  emit("setItemRef", null, props.message.id);
});
</script>

<template>
  <div ref="itemElement" class="notification-item" :style="getItemBaseStyle(message, index)" :title="`${message.id}`">
    <div class="message-body">
      <div class="bg-layer-persistent"></div>
      <div class="fg-layer-progress" :style="getProgressStyle(message)"></div>
      <div v-if="message.direction === 'spotlight'" class="spotlight-glow" :style="getSpotlightStyle(message)"></div>

      <div class="content-wrapper" :style="{ color: getDynamicTextColor(message) }">
        <div v-if="message.closable" class="spacer"></div>
        <span class="text-content" :title="message.content">
          {{ message.content }}
        </span>
        <span v-if="message.count > 1" class="count-badge" :style="{ backgroundColor: getBadgeBg(message) }">
          {{ message.count }}
        </span>
        <button v-if="message.closable" class="close-btn" type="button" aria-label="关闭通知"
          @click="emit('remove', message.id)">
          ×
        </button>
      </div>
    </div>
  </div>
  <svg style="display: none;">
    <defs>
      <filter :id="`filter-${message.id}`" x="-50%" y="-50%" width="200%" height="200%">
        <feGaussianBlur :id="`${message.id}-blur`" in="SourceGraphic" stdDeviation="10" result="blur" />
        <feColorMatrix :id="`${message.id}-matrix`" in="blur" mode="matrix" :values="initialMatrixValues" />
      </filter>
    </defs>
  </svg>
</template>

<style scoped lang="scss">
.notification-item {
  pointer-events: auto;
  position: relative;
  margin-bottom: 8px;
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

.message-body {
  position: relative;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 8px 20px;
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
  background: linear-gradient(90deg,
      transparent,
      rgba(255, 255, 255, 0.6),
      transparent);
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

@media (max-width: 640px) {
  .notification-item {
    padding: 6px 16px;
    min-height: 40px;
    min-width: 120px;
  }
}
</style>
