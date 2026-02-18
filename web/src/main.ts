import { createApp } from 'vue';
import { createPinia } from 'pinia';
import 'element-plus/dist/index.css';
import 'element-plus/theme-chalk/dark/css-vars.css';
import './style.css';
import App from './App.vue';

function initTheme(): void {
  const saved = localStorage.getItem('theme');
  let next: 'dark' | 'light' = 'light';
  if (saved === 'light' || saved === 'dark') {
    next = saved;
  }
  document.documentElement.setAttribute('data-theme', next);
  if (next === 'dark') {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
}

initTheme();

const app = createApp(App);
app.use(createPinia());

app.mount('#app');
