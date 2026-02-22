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
      "/mm": {
        target: "https://8.137.127.7",
        // target: "http://localhost:5210",
        headers: {
          host: "check.meowmemoirs.cn",
        },
        changeOrigin: true,
        secure: false,
      },
    },
  },
  /**
   * 构建配置
   */
  build: {
    // 指定生成静态资源的存放目录
    assetsDir: "assets",
    // 指定输出目录
    outDir: "dist",
    // 指定压缩器
    minify: "terser",
    // 启用CSS代码分割
    cssCodeSplit: true,

    /**
     * Rollup打包选项
     */
    rollupOptions: {
      output: {
        /**
         * 自定义chunks分组策略
         * 将node_modules中的依赖分别打包成独立文件
         */
        manualChunks(id) {
          if (id.includes("node_modules")) {
            // 让每个插件都打包成独立的文件
            return id
              .toString()
              .split("node_modules/")[1]
              .split("/")[0]
              .toString();
          }
        },
      },
    },

    /**
     * Terser压缩选项
     */
    // terserOptions: {
    //   compress: {
    //     // 是否删除console语句
    //     drop_console: false,
    //     // 是否删除debugger语句
    //     drop_debugger: true,
    //   },
    // },
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
