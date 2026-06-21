# unlock-music-vite

项目文档：[项目结构和功能实现文档](docs/project-structure-and-implementation.md)

Unlock Music 的 Vite + Vue 3 迁移版本。

## 开发

```sh
npm install
npm run dev
```

## 构建

```sh
npm run build
```

`npm run build` 会先执行 `vue-tsc --build`，再执行 `vite build`。

## 迁移说明

- UI 入口已从默认 Vite 模板替换为 Unlock Music 文件处理界面。
- 旧解密模块已接入 Vite 构建，并补齐浏览器端 `Buffer` polyfill。
- 解密算法依赖按旧项目 API 固定到兼容版本。
- JOOX 的 `@unlock-music/joox-crypto` 包不在 npm 可用范围内，当前仍保持原有禁用行为。
