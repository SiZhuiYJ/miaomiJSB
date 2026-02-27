import { defineConfig } from "vite";
// 引入Vue插件
import vue from "@vitejs/plugin-vue";
// 引入路径别名解析插件
import path from "path";

// 引入rollup打包可视化分析插件
import { visualizer } from "rollup-plugin-visualizer";

// 引入自动导入插件和组件自动注册插件
import AutoImport from "unplugin-auto-import/vite";
import Components from "unplugin-vue-components/vite";
import { ElementPlusResolver } from "unplugin-vue-components/resolvers";

// 引入SVG图标插件
import svgSprite from "vite-plugin-svg-sprite";
// const { createSvgIconsPlugin } = vitePluginSvgIcons;

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    // 引入Vue插件
    vue(),
    // 打包可视化分析插件，用于分析打包文件大小
    visualizer({ open: true }),
    // 自动导入插件，自动导入Vue和Pinia相关函数，并自动注册Element Plus组件
    AutoImport({
      imports: ["vue", "pinia"],
      resolvers: [ElementPlusResolver()],
      dts: "src/auto-imports.d.ts",
    }),
    // 组件自动注册插件，自动注册Element Plus组件
    Components({
      resolvers: [ElementPlusResolver()],
      dts: "src/components.d.ts",
    }),
    // SVG图标插件，用于处理SVG雪碧图
    svgSprite({
      // 指定存放SVG图标的目录
      include: ["src/assets/icons/**/*.svg", "src/assets/icons/*.svg"],
      // 生成 symbol ID 格式
      symbolId: "icon-[name]",
    }),
  ],
  // 开发服务器配置
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
  /**
   * 构建配置
   */
  build: {
    rollupOptions: {
      output: {
        chunkFileNames: "js/[name]-[hash].js", // 引入文件名的名称
        entryFileNames: "js/[name]-[hash].js", // 包的入口文件名称
        assetFileNames: "[ext]/[name]-[hash].[ext]", // 资源文件像 字体，图片等

        // 最小化拆分包
        manualChunks(id) {
          if (id.includes("node_modules")) {
            return id
              .toString()
              .split("node_modules/")[1]
              .split("/")[0]
              .toString();
          }
        },
      },
    },
    minify: "terser", // 启用 terser 压缩
    terserOptions: {
      compress: {
        // 只删除 console.log
        // pure_funcs: ["console.log"],
        // 删除 debugger
        // drop_debugger: true,
      },
    },
  },
  esbuild: {
    // 删除 所有的console 和 debugger
    // drop: ["console", "debugger"],
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
