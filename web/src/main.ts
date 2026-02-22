import { createApp } from "vue";
import "element-plus/dist/index.css";
import "element-plus/theme-chalk/dark/css-vars.css";
import * as ElementPlusIconsVue from "@element-plus/icons-vue";
import "./styles/index.scss";
import App from "./App.vue";
// element-plus暗黑主题样式
import "element-plus/theme-chalk/dark/css-vars.css";
import "element-plus/theme-chalk/src/loading.scss";

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

initTheme();

const app = createApp(App);

// 路由管理 - Vue Router
import router from "./routers";

// 状态管理 - Pinia
import pinia from "./stores";

// 自定义指令集合
import directives from "./directives";

// 注册SVG图标（Vite插件生成的虚拟模块）
import "virtual:svg-icons-register";

// 注册Vue Router路由系统
app.use(router);

// 注册Pinia状态管理
app.use(pinia);

// 注册全局自定义指令
app.use(directives);

for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component);
}

app.mount("#app");
