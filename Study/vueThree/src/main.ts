import './assets/main.css'

import { createApp } from 'vue'

import App from './App.vue'
import router from './router'
// 状态管理 - Pinia
import pinia from "./stores";
import 'pinia-plugin-persistedstate'

import Vconsole from 'vconsole'
const app = createApp(App)

app.use(pinia)
app.use(router)
const vconsole = new Vconsole()
app.mount('#app')
