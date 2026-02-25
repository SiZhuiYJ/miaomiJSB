import { createApp } from "vue";
import App from "./App.vue";

// 导入style样式
import "./styles/index.scss";

// element-plus 主题样式
// import "element-plus/dist/index.css";
import "element-plus/theme-chalk/dark/css-vars.css";
// element-plus 暗黑主题样式
import "element-plus/theme-chalk/src/loading.scss";
// element-plus icon导入
import * as ElementPlusIconsVue from "@element-plus/icons-vue";

const app = createApp(App);

// 路由管理 - Vue Router
import router from "./routers";

// 状态管理 - Pinia
import pinia from "./stores";

// 自定义指令集合
import directives from "./directives";

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
