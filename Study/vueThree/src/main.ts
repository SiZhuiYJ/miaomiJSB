import './assets/main.css'

import { createApp } from 'vue'

import App from './App.vue'
import router from './router'
// 状态管理 - Pinia
import pinia from "./stores";
import 'pinia-plugin-persistedstate'
const app = createApp(App)

app.use(pinia)
app.use(router)

app.mount('#app')
