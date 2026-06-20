<script setup lang="ts">
import { gsap } from 'gsap';
import { onBeforeUnmount, onMounted, useTemplateRef } from "vue";
import type { CSSProperties } from "vue";
import type { NotificationMessage } from "./types";

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
const messageSurfaceElement = useTemplateRef("messageSurfaceElement");

let timeline: gsap.core.Timeline | null = null;

const getLuminance = (hex: string) => {
  const normalized = hex.replace("#", "");
  const r = parseInt(normalized.slice(0, 2), 16);
  const g = parseInt(normalized.slice(2, 4), 16);
  const b = parseInt(normalized.slice(4, 6), 16);

  return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
};

const getDynamicTextColor = (msg: NotificationMessage) => {
  const luminance = getLuminance(msg.color);
  // return luminance < 0.6 ? "#ffffff" : "#1e293b";
  return luminance < 0.6 ? "#ffffff" : "#ffffff";
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

const animateMessagePress = (scale: number, duration: number, ease: string) => {
  const surfaceEl = messageSurfaceElement.value;
  if (!surfaceEl || props.message.isRemoving) return;

  gsap.to(surfaceEl, {
    scale,
    duration,
    ease,
    overwrite: "auto",
  });
};

const shrinkMessage = () => {
  animateMessagePress(0.96, 0.1, "power2.out");
};

const restoreMessage = () => {
  animateMessagePress(1, 0.18, "back.out(2.4)");
};

const closeMessage = () => {
  restoreMessage();
  emit("remove", props.message.id);
};

onMounted(() => {
  // 先把 SVG filter 恢
  //     // state 是这条消息自己的滤镜动画状态，不能复用全局 filterState 对象。
  const state = { ...filterState };

  // 先把 SVG filter 恢复到初始 blob 状态，避免复用 DOM 时继承上次 identity 状态。
  applyMessageFilterState(state);

  gsap.timeline()
    // 初始化气泡滤镜和标签位置，确保重复播放时不会继承上一次的终态。
    .set(itemElement.value, {
      filter: `url(#${`filter-${props.message.id}`})`,
    })
    // SVG filter 不能从 url() 平滑过渡到 none，所以先把滤镜参数动到 identity。
    // 0.6s 开始收束，和入场动画重叠，视觉上是气泡落下后逐渐变清晰。
    .to(state, {
      blur: 0,
      alpha: 1,
      offset: 0,
      duration: 0.6,
      ease: 'power2.out',
      onUpdate: () => applyMessageFilterState(state),
      onComplete: () => {
        // 参数到达 identity 后再切换 filter:none，此时视觉上已经没有跳变。
        applyMessageFilterState(state);
        gsap.set(itemElement.value, { filter: 'none' });
      },
    })
  gsap.to('.round', {
    top: '0',
    duration: 0.6,
  });

  emit("setItemRef", itemElement.value, props.message.id);
});

onBeforeUnmount(() => {
  emit("setItemRef", null, props.message.id);
});
</script>

<template>
  <div ref="itemElement" class="notification-item" :style="getItemBaseStyle(message, index)">
    <div class="round"></div>
    <div class="message-body" :style="{ '--top-index': `${index}`, }">
      <div ref="messageSurfaceElement" class="message-surface">
        <div class="bg-layer-persistent"></div>
        <div class="fg-layer-progress" :style="getProgressStyle(message)"></div>
        <div v-if="message.direction === 'spotlight'" class="spotlight-glow" :style="getSpotlightStyle(message)"></div>

        <div class="content-wrapper" :style="{ color: getDynamicTextColor(message) }">
          <div v-if="message.closable" class="spacer"></div>
          <span class="text-content" :title="message.content">
            {{ message.content }}
          </span>
          <span v-if="message.count > 1" class="count-badge" :style="{ backgroundColor: getBadgeBg(message) }">
            <p>*</p>
            {{ message.count }}
          </span>
          <button v-if="message.closable" class="close-btn" type="button" aria-label="关闭通知"
            @pointerdown="shrinkMessage" @pointerup="restoreMessage" @pointercancel="restoreMessage"
            @pointerleave="restoreMessage" @click="closeMessage">
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
  </div>

</template>

<style scoped lang="scss">
.notification-item {
  pointer-events: auto;
  position: absolute;
  top: calc(var(--item-height) - var(--item-height) * 2);
  min-width: 50%;
  max-width: calc(100vw - 40px);
  min-width: 140px;
  min-height: var(--item-height);
  background-color: currentColor;
  user-select: none;
  will-change: transform, opacity;
}

.round {
  display: flex;
  position: absolute;
  bottom: -5px;
  height: 10px;
  width: 100%;
  border-radius: 50%;
  background-color: currentColor;
}

.message-body {
  --top-index: 0;
  position: relative;
  top: calc(var(--item-height) * (var(--top-index) + 1) + var(--item-gap));
  left: 50%;
  transform: translateX(-50%);
  width: fit-content;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  will-change: transform, opacity, filter;
}

.message-surface {
  position: relative;
  width: fit-content;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 10px 10px;
  border-radius: var(--main-radius);
  background-color: #fff;
  box-shadow: 0 8px 20px -6px rgba(0, 0, 0, 0.12);
  overflow: hidden;
  transform-origin: center center;
  will-change: transform;
}

.bg-layer-persistent {
  position: absolute;
  inset: 0;
  z-index: 1;
  opacity: 0.3;
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
  display: flex;
  font-weight: 900;
  padding: 1px 6px;
  border-radius: 8px;
  font-size: 0.7rem;
  box-shadow: 0 0 2px rgb(255 255 255);
  flex-shrink: 0;
  transition: transform 0.3s;
  animation: smoothShake 1.2s infinite;

  >p {
    font-weight: 400;
  }

  &:hover {
    opacity: 1;
    animation: none;
    transform: scale(1.1);
  }
}

@keyframes smoothShake {
  0% {
    transform: rotate(0deg) scale(1);
  }

  10% {
    transform: rotate(9deg) scale(1);
  }

  20% {
    transform: rotate(-9deg) scale(1);
  }

  30% {
    transform: rotate(6deg) scale(1);
  }

  40% {
    transform: rotate(-6deg) scale(1);
  }

  50% {
    transform: rotate(0deg) scale(1);
  }

  100% {
    transform: rotate(0deg) scale(1);
  }
}

.close-btn {
  $close-color: #ffffff;
  $border-color: #000000;
  $size: 20px;
  $plus-size: 14px;
  $plus-thickness: 2px;
  $border-width: 1px;
  $transition-duration: 0.2s;
  $rotate-duration: 0.5s;
  $scale-factor: 1.2;

  --close-color: #{$close-color};
  --border-color: #{$border-color};

  opacity: 1;
  width: $size;
  height: $size;
  border-radius: 50%;
  border-width: 5px;
  border-spacing: 5px;
  border: $border-width dashed transparent;
  background-color: transparent;
  background-image:
    linear-gradient($close-color, $close-color),
    linear-gradient($close-color, $close-color);
  background-repeat: no-repeat;
  background-position: center;
  background-size:
    $plus-size $plus-thickness,
    $plus-thickness $plus-size;
  transition:
    background-color $transition-duration ease,
    box-shadow $transition-duration ease,
    transform $rotate-duration ease;
  cursor: pointer;
  transform: rotate(45deg);

  &:hover {
    border: $border-width dashed var(--close-color);
    transform: rotate(225deg) scale($scale-factor);
  }

  &::after {
    content: "";
    position: absolute;
    inset: 0;
    border-radius: inherit;
    box-shadow: 0 0 0 6px var(--close-color);
    opacity: 0;
    transition: 0.3s;
  }

  &:focus-visible {
    outline: 0;
  }

  &:hover,
  &:focus {
    border-color: var(--close-color);
  }

  &:active {

    &::after {
      transition: 0s;
      box-shadow: none;
      opacity: 0.4;
    }
  }
}

@media (max-width: 640px) {
  .notification-item {
    min-width: 120px;
  }

  .message-surface {
    padding: 8px 8px;
  }
}
</style>
