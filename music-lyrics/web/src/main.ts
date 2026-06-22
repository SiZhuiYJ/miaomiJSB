import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { persistedStatePlugin } from './stores/persistedState'

const pinia = createPinia()
pinia.use(persistedStatePlugin)

createApp(App).use(pinia).mount('#app')
