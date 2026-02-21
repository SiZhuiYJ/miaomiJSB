import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import path from "path";
import AutoImport from "unplugin-auto-import/vite";
import Components from "unplugin-vue-components/vite";
import { ElementPlusResolver } from "unplugin-vue-components/resolvers";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    AutoImport({
      imports: ["vue", "pinia"],
      resolvers: [ElementPlusResolver()],
      dts: "src/auto-imports.d.ts",
    }),
    Components({
      resolvers: [ElementPlusResolver()],
      dts: "src/components.d.ts",
    }),
  ],
  server: {
    proxy: {
      // "/mm": {
      //   target: "https://8.137.127.7",
      //   headers: {
      //     host: "check.meowmemoirs.cn",
      //   },
      //   changeOrigin: true,
      //   secure: false,
      // },
      "/mm": {
        target: "http://localhost:5210",
        headers: {
          host: "check.meowmemoirs.cn",
        },
        changeOrigin: true,
        secure: false,
      },
    },
  },
  /**
   * 路径解析配置
   * 设置模块导入路径别名，提高代码可读性和维护性
   */
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
      "@assets": path.resolve(__dirname, "src/assets"),
      "@components": path.resolve(__dirname, "src/components"),
      "@libs": path.resolve(__dirname, "src/libs"),
      "@utils": path.resolve(__dirname, "src/utils"),
      "@views": path.resolve(__dirname, "src/views"),
    },
  },
});
