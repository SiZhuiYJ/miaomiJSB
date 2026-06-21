import 'element-plus/dist/index.css';
import './assets/main.css';

import ElementPlus from 'element-plus';
import { Buffer } from 'buffer';
import { createApp } from 'vue';
import App from './App.vue';
import { browserProcess } from './polyfills/process';

window.Buffer = Buffer;
globalThis.Buffer = Buffer;

const legacyProcess = browserProcess as unknown as NodeJS.Process;
window.process = legacyProcess;
globalThis.process = legacyProcess;

createApp(App).use(ElementPlus).mount('#app');
