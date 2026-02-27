<script setup lang="ts">
// 2495550774@qq.com
import { ref, onMounted } from "vue";
import { ElConfigProvider, dayjs } from "element-plus";
import { setNotificationInstance } from "@/utils/notification";
import NotificationSystem from "@/components/NotificationSystem/index.vue";
import { notify, notifySuccess, notifyWarning, notifyError } from "@/utils/notification";
// @ts-ignore
import zhCn from "element-plus/dist/locale/zh-cn.mjs";
// @ts-ignore
dayjs.en.weekStart = 1;

const notificationSystemRef = ref(null);
// 初始化主题
function initTheme(): void {
  const saved = localStorage.getItem("theme");
  let next: "dark" | "light" = "light";
  if (saved === "light" || saved === "dark") {
    next = saved;
  }
  document.documentElement.setAttribute("data-theme", next);
  if (next === "dark") {
    document.documentElement.classList.add("dark");
  } else {
    document.documentElement.classList.remove("dark");
  }
}

onMounted(async () => {
  setNotificationInstance(notificationSystemRef.value);
  initTheme();

  // 测试通知系统（减少测试数量以避免性能问题）
  notify({ content: "欢迎使用每日打卡系统", color: "#10b981", duration: 30000 });

  // 延迟测试不同类型的通知
  setTimeout(() => {
    notifySuccess("登录成功！");
  }, 2000);
  setTimeout(() => {
    notifyWarning("请注意打卡时间");
  }, 4000);
  setTimeout(() => {
    notifyError("网络连接异常");
  }, 6000);
});

import { computed, nextTick, shallowReactive } from 'vue'

import type { ButtonInstance, DialogTransition } from 'element-plus'

type GlobalConfig = {
  alignCenter: boolean
  draggable: boolean
  overflow: boolean
  transition?: DialogTransition
}

const config = shallowReactive<GlobalConfig>({
  alignCenter: true,
  draggable: false,
  overflow: false,
})
const enableTransition = ref(true)
const isObjectTransition = ref(false)

const buttonRef = ref<ButtonInstance>()

const ANIMATION_DURATION = 300

const globalConfig = computed<GlobalConfig>(() => {
  let transition: DialogTransition | undefined
  if (enableTransition.value) {
    if (isObjectTransition.value) {
      transition = {
        css: false,
        onBeforeEnter(el) {
          nextTick(() => {
            if (buttonRef.value) {
              const buttonRect = buttonRef.value.ref!.getBoundingClientRect()
              const dialogEl = el.querySelector('.el-dialog') as HTMLElement

              if (dialogEl) {
                const dialogRect = dialogEl.getBoundingClientRect()

                const offsetX =
                  buttonRect.left +
                  buttonRect.width / 2 -
                  (dialogRect.left + dialogRect.width / 2)
                const offsetY =
                  buttonRect.top +
                  buttonRect.height / 2 -
                  (dialogRect.top + dialogRect.height / 2)

                dialogEl.style.transform = `translate(${offsetX}px, ${offsetY}px) scale(0.3)`
                dialogEl.style.opacity = '0'
              }
            }
          })
        },
        onEnter(el, done) {
          nextTick(() => {
            const dialogEl = el.querySelector('.el-dialog') as HTMLElement
            if (dialogEl) {
              // force reflow
              dialogEl.offsetHeight

              dialogEl.style.transition = `all ${ANIMATION_DURATION}ms cubic-bezier(0.4, 0, 1, 1)`
              dialogEl.style.transform = 'translate(0, 0) scale(1)'
              dialogEl.style.opacity = '1'

              // wait for animation to complete, then cleanup inline styles to avoid affecting drag
              setTimeout(() => {
                dialogEl.style.transition = ''
                dialogEl.style.transform = ''
                dialogEl.style.opacity = ''
                done()
              }, ANIMATION_DURATION)
            } else {
              done()
            }
          })
        },
        onLeave(el, done) {
          const dialogEl = el.querySelector('.el-dialog') as HTMLElement
          if (dialogEl) {
            if (buttonRef.value) {
              const buttonRect = buttonRef.value.ref!.getBoundingClientRect()
              const dialogRect = dialogEl.getBoundingClientRect()

              const currentTransform = dialogEl.style.transform
              let dragOffsetX = 0
              let dragOffsetY = 0

              // avoid draggable effect
              if (currentTransform) {
                const translateMatch = currentTransform.match(
                  /translate\(([^,]+),\s*([^)]+)\)/
                )
                if (translateMatch && translateMatch[1] && translateMatch[2]) {
                  dragOffsetX = Number.parseFloat(translateMatch[1])
                  dragOffsetY = Number.parseFloat(translateMatch[2])
                }
              }

              const offsetX =
                buttonRect.left +
                buttonRect.width / 2 -
                (dialogRect.left + dialogRect.width / 2) +
                dragOffsetX
              const offsetY =
                buttonRect.top +
                buttonRect.height / 2 -
                (dialogRect.top + dialogRect.height / 2) +
                dragOffsetY

              dialogEl.style.transition = `all ${ANIMATION_DURATION}ms cubic-bezier(0.4, 0, 1, 1)`
              dialogEl.style.transform = `translate(${offsetX}px, ${offsetY}px) scale(0.3)`
              dialogEl.style.opacity = '0'

              // wait for animation to complete, then cleanup inline styles
              setTimeout(() => {
                dialogEl.style.transition = ''
                dialogEl.style.transform = ''
                dialogEl.style.opacity = ''
                done()
              }, ANIMATION_DURATION)
            } else {
              done()
            }
          } else {
            done()
          }
        },
      }
    } else {
      transition = 'dialog-bounce'
    }
  }
  return {
    alignCenter: config.alignCenter,
    draggable: config.draggable,
    overflow: config.overflow,
    transition,
  }
})
</script>

<template>
  <!-- Element Plus全局配置组件 -->
  <el-config-provider :locale="zhCn" :dialog="globalConfig">
    <!-- 路由视图容器 -->
    <router-view></router-view>
    <NotificationSystem ref="notificationSystemRef" />
  </el-config-provider>
</template>
<style></style>