import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()], // 开发服务器配置
  server: {
    proxy: {
      "/mm": {
        target: "https://8.137.127.7",
        // target: "http://localhost:5210",
        headers: {
          host: "check.meowmemoirs.cn",
        },
        //* 忽略https证书错误 */
        changeOrigin: true,
        // 允许代理服务器使用自签名证书
        secure: false,
      },
    },
    watch: {
      usePolling: true, // 启用轮询
      interval: 100, // 轮询间隔（毫秒）
    },
  },
});
