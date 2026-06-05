<script setup lang="ts">
import GsapView from './views/gsap/index.vue';
import LoadingView from './views/loading/index.vue';
import bubbles from './views/bubbles/index.vue';
import message from './views/message/index.vue';
import { ref } from 'vue';
type DurationValue = string | number;
interface MessageOptions {
  id?: string | number;
  text?: string;
  content?: string;
  x?: string;
  y?: string;
  s?: string;
  color?: string;
  d?: DurationValue;
  duration?: DurationValue;
  delay?: DurationValue;
}
const messageOptions = ref<MessageOptions[]>([
  {
    id: '1',
    text: 'hello world',
    x: '50%',
    y: '50%',
    s: '1',
    color: 'red',
    d: '1s',
    duration: '1s',
    delay: '0.5s',
  },
  {
    id: '2',
    text: 'welcome to use',
    x: '50%',
    y: '60%',
    s: '1',
    color: 'blue',
    d: '1s',
    duration: '1s',
    delay: '1s',
  },
]);
</script>

<template>
  <!-- <message :items="messageOptions" /> -->
  <message />
  <GsapView />
  <LoadingView />
  <!-- <bubbles /> -->
</template>