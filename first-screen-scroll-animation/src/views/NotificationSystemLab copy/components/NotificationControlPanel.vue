<script setup lang="ts">
import type { NotificationDirection, NotificationDirectionOption, NotificationForm } from "../types";

interface Props {
  directions: NotificationDirectionOption[];
}

defineProps<Props>();

const form = defineModel<NotificationForm>({ required: true });

const emit = defineEmits<{
  submit: [];
}>();

const quick = (
  content: string,
  color: string,
  direction: NotificationDirection,
) => {
  form.value.content = content;
  form.value.color = color;
  form.value.direction = direction;
};
</script>

<template>
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

        <button type="button" class="submit-btn" @click="emit('submit')">
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
</template>

<style scoped lang="scss">
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

@media (max-width: 640px) {
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
