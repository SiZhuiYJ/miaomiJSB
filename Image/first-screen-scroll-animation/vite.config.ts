import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  server: {
    host: '0.0.0.0',
    proxy: {
      '/bing': {
        target: 'https://cn.bing.com',
        changeOrigin: true,
        secure: true,
        rewrite: (path) => path.replace(/^\/bing/, ''),
      },
    },
  },
  preview: {
    host: '0.0.0.0',
  },
});
