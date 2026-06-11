<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import type { CSSProperties } from "vue";
import { darkenColor, toRgba } from "./color";
import type { NotificationMessage } from "./types";

const props = defineProps<{
  message: NotificationMessage;
  index: number;
}>();

const emit = defineEmits<{
  close: [id: number];
  register: [el: HTMLElement | null, id: number];
}>();

const itemElement = ref<HTMLElement | null>(null);

const getLuminance = (hex: string) => {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
};

const dynamicTextColor = computed(() => {
  const luminance = getLuminance(props.message.color);
  return luminance < 0.6 ? "#ffffff" : "#ffffff";
});

const badgeBg = computed(() =>
  dynamicTextColor.value === "#ffffff"
    ? "rgba(255, 255, 255, 0.2)"
    : "rgba(0, 0, 0, 0.06)",
);

const itemBaseStyle = computed(
  () =>
    ({
      background: toRgba(props.message.color, 0.5),
      color: props.message.color,
      zIndex: 100 - props.index,
      "--bgc": toRgba(props.message.color, 0.5),
    }) as CSSProperties,
);

const progressStyle = computed(() => {
  const style: CSSProperties = {
    transform: "scale(1)",
    transformOrigin: "left center",
    opacity: 1,
    filter: "none",
  };

  switch (props.message.direction) {
    case "ltr":
      style.transform = `scaleX(${props.message.progress})`;
      style.transformOrigin = "left center";
      break;
    case "rtl":
      style.transform = `scaleX(${props.message.progress})`;
      style.transformOrigin = "right center";
      break;
    case "ttb":
      style.transform = `scaleY(${props.message.progress})`;
      style.transformOrigin = "center top";
      break;
    case "btt":
      style.transform = `scaleY(${props.message.progress})`;
      style.transformOrigin = "center bottom";
      break;
    case "center":
      style.transform = `scaleX(${props.message.progress})`;
      style.transformOrigin = "center center";
      break;
    case "vSplit":
      style.transform = `scaleY(${props.message.progress})`;
      style.transformOrigin = "center center";
      break;
    case "ripple":
      style.transform = `scaleX(${props.message.progress})`;
      style.transformOrigin = "left center";
      break;
    case "spotlight":
      style.transform = `scaleX(${props.message.progress})`;
      style.transformOrigin = "left center";
      break;
    case "fade":
      style.opacity = props.message.progress;
      style.transform = `scale(${0.98 + 0.02 * props.message.progress})`;
      style.filter = `blur(${(1 - props.message.progress) * 2}px)`;
      break;
  }

  return style;
});

const spotlightStyle = computed(
  () =>
    ({
      left: `${props.message.progress * 100}%`,
      transform: "translateX(-100%)",
      opacity: props.message.progress > 0.1 ? 1 : 0,
      transition: "opacity 0.3s",
    }) as CSSProperties,
);

const closeButtonStyle = computed(
  () =>
    ({
      "--bgc-color": darkenColor(props.message.color, 0.1),
      "--border-color": darkenColor(props.message.color, 0.5),
    }) as CSSProperties,
);

onMounted(() => {
  emit("register", itemElement.value, props.message.id);
});

onBeforeUnmount(() => {
  emit("register", null, props.message.id);
});
</script>

<template>
  <div ref="itemElement" class="notification-item" :style="itemBaseStyle">
    <div class="bg-layer-persistent"></div>
    <div class="fg-layer-progress" :style="progressStyle"></div>
    <div v-if="message.direction === 'spotlight'" class="spotlight-glow" :style="spotlightStyle"></div>

    <div class="content-wrapper" :style="{ color: dynamicTextColor }">
      <div v-if="message.closable" class="spacer"></div>
      <span class="text-content" :title="message.content">{{ message.content }}</span>
      <span v-if="message.count > 1" class="count-badge" :style="{ backgroundColor: badgeBg }">
        <p>*</p>
        {{ message.count }}
      </span>
      <span v-if="message.closable" class="close-btn" :style="closeButtonStyle" @click="emit('close', message.id)">
      </span>
    </div>
  </div>
</template>

<style scoped lang="scss">
.notification-item {
  pointer-events: auto;
  position: relative;
  margin-bottom: 8px;
  padding: 8px 20px;
  border-radius: 22px;
  width: fit-content;
  max-width: calc(100vw - 40px);
  min-width: 140px;
  min-height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 20px -6px rgba(0, 0, 0, 0.12);
  user-select: none;
  background: rgba(0, 0, 0, 0.12);
  overflow: hidden;
  will-change: transform, opacity;
  border: 2px solid currentColor;
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 0 20px 2px var(--bgc);
  }
}

@media (max-width: 640px) {
  .notification-item {
    padding: 6px 16px;
    min-height: 40px;
    min-width: 120px;
  }
}

.bg-layer-persistent {
  position: absolute;
  inset: 0;
  z-index: 1;
  opacity: 0.15;
  background-color: currentColor;
  border-radius: 22px;
}

.fg-layer-progress {
  position: absolute;
  inset: 0;
  z-index: 2;
  background-color: currentColor;
  border-radius: 22px;
  will-change: transform, opacity, filter;
}

.spotlight-glow {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 60px;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.6), transparent);
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
  letter-spacing: -0.025em;
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
  $bgc-color: #000000;
  $border-color: #000000;
  $size: 24px;
  $plus-size: 14px;
  $plus-thickness: 4px;
  $border-width: 1px;
  $transition-duration: 0.2s;
  $rotate-duration: 0.5s;
  $scale-factor: 1.2;

  --close-color: #{$close-color};
  --bgc-color: #{$bgc-color};
  --border-color: #{$border-color};

  opacity: 0.5;
  width: $size;
  height: $size;
  border-radius: 50%;
  border-width: 5px;
  border-spacing: 5px;
  border: $border-width dashed var(--close-color);
  background-color: var(--bgc-color);
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
    opacity: 1;
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
    color: var(--bgc-color);
    border-color: var(--close-color);
  }

  &:active {
    background-color: var(--bgc-color);

    &::after {
      transition: 0s;
      box-shadow: none;
      opacity: 0.4;
    }
  }
}
</style>
