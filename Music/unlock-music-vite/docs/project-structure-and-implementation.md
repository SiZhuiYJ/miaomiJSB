# 项目结构和功能实现文档

本文档记录 `unlock-music-vite` 的目录结构、核心流程和当前迁移实现，便于后续维护和二次开发。

## 项目定位

`unlock-music-vite` 是从原 `unlock-music` 迁移到 Vue 3 + Vite 的浏览器端本地音乐解锁工具。文件处理、解密、元数据写入和下载都在浏览器内完成，不上传音乐文件。

## 目录结构

```text
unlock-music-vite/
├─ docs/
│  └─ project-structure-and-implementation.md
├─ public/
│  └─ favicon.ico
├─ src/
│  ├─ App.vue
│  ├─ main.ts
│  ├─ shims-fs.ts
│  ├─ assets/
│  ├─ component/
│  ├─ components/
│  ├─ decrypt/
│  ├─ polyfills/
│  ├─ scss/
│  ├─ types/
│  └─ utils/
├─ env.d.ts
├─ index.html
├─ package.json
├─ tsconfig*.json
└─ vite.config.ts
```

## 关键目录说明

`src/App.vue` 是当前主界面入口，负责文件选择、拖拽上传、处理进度、解锁结果、错误状态、播放、下载、删除和编辑弹窗。

`src/main.ts` 创建 Vue 应用，注册 Element Plus，并为旧解锁依赖补齐浏览器环境下的 `Buffer` 和 `process` 全局变量。

`src/decrypt/` 保存各格式解锁模块。`index.ts` 按文件扩展名动态加载具体实现，降低首屏包体积。

`src/decrypt/utils.ts` 提供通用工具，包括文件读取、音频格式探测、封面读取、元数据解析和 MP3/FLAC 标签写入。

`src/utils/storage/` 封装配置存储。浏览器环境默认使用本地存储，Worker 场景可退回内存存储。

`src/polyfills/process.ts` 提供浏览器端 `process` shim，用于兼容旧依赖中的 `process.env` 和 `process.nextTick` 访问。

`src/types/legacy-modules.d.ts` 为缺少类型声明的旧依赖补充最小类型声明。

## 主流程

```text
用户选择或拖拽文件
  ↓
App.vue 收集 File 列表并读取配置
  ↓
逐个调用 decrypt/index.ts 的 Decrypt(file, config)
  ↓
按扩展名动态加载具体解锁模块
  ↓
解锁模块返回 DecryptResult
  ↓
App.vue 写入结果表格，提供播放、下载、编辑和删除
```

## 解锁调度

`src/decrypt/index.ts` 使用文件扩展名分发：

- `.ncm`：网易云音乐文件，走 `decrypt/ncm.ts`。
- `.uc`：网易云缓存，走 `decrypt/ncmcache.ts`。
- `.qmc*`、`.mflac*`、`.mgg*`、`.tkm` 等：QQ 音乐系列，走 `decrypt/qmc.ts` 或相关缓存模块。
- `.kgm`、`.kgma`、`.vpr`：酷狗系列，走 `decrypt/kgm.ts`。
- `.kwm`：酷我音乐，走 `decrypt/kwm.ts`。
- `.xm`、`.x2m`、`.x3m`：虾米或喜马拉雅相关格式。
- `.mp3`、`.flac`、`.m4a`、`.wav`、`.ogg`：原始或可直通格式，走 `raw` 或 `xm` 兼容路径。

调度结果统一补充 `rawExt` 和 `rawFilename`，便于后续下载命名和展示。

## NCM 实现要点

`decrypt/ncm.ts` 完成以下步骤：

1. 校验 NCM 魔数。
2. 解密核心 key data 并构造 key box。
3. 解密音乐元数据，解析歌名、歌手、专辑和封面地址。
4. 通过 key box 解密音频字节。
5. 探测输出音频格式和 MIME。
6. 拉取封面并写入 MP3/FLAC 元数据。
7. 返回浏览器可播放和可下载的 Blob URL。

迁移后已移除 `jimp`，超大封面压缩改用浏览器原生 `createImageBitmap + canvas`，避免 Vite 加载 Node `util/fs` 兼容模块导致运行时报错。

## 元数据和下载

`decrypt/utils.ts` 负责：

- `SniffAudioExt`：根据文件头判断音频格式。
- `GetImageFromURL`：获取远程封面并生成本地 Blob URL。
- `WriteMetaToMp3`：使用 `browser-id3-writer` 写入 ID3 标签。
- `WriteMetaToFlac`：使用 `metaflac-js` 写入 FLAC 标签。
- `ToArrayBuffer`：统一 `ArrayBuffer` 和 `Uint8Array`，避免 Blob 构造类型不兼容。

`utils/utils.ts` 负责：

- 根据用户选择的命名策略生成下载文件名。
- 触发浏览器下载。
- 释放 Blob URL，避免内存泄漏。

## 界面功能

当前主界面提供：

- 点击选择文件。
- 拖拽文件到上传区。
- 批量逐个处理文件。
- 显示当前处理进度。
- 显示解锁结果表格。
- 显示失败文件和错误信息。
- 播放解锁后的音频。
- 下载解锁后的文件。
- 编辑展示用的标题、歌手、专辑和封面。
- 配置 JOOX UUID。
- 页面底部保留原项目二次开发版权信息。

## 浏览器兼容处理

旧解锁模块和依赖最初面向 Webpack/Node polyfill 环境。Vite 默认不提供 Node 全局变量，因此迁移中做了以下处理：

- `main.ts` 注入 `Buffer`。
- `main.ts` 注入浏览器版 `process` shim。
- `ncm.ts` 移除 `jimp`，避免 Node core 模块外部化后运行时报错。
- `tsconfig.app.json` 放宽部分严格类型检查，以兼容旧模块代码。
- `legacy-modules.d.ts` 为旧依赖补充类型声明。

## 已知限制

- `@unlock-music/joox-crypto` 当前未在可用 npm 范围内，JOOX `.ofl_en` 暂未恢复。
- 部分旧依赖在生产构建中仍会提示 `fs/path` 外部化或 `eval` 警告；当前已知场景不影响主流程构建。
- 解锁能力依赖浏览器 API，建议在现代 Chromium、Edge 或 Firefox 中运行。

## 常用命令

```sh
npm install
npm run dev
npm run build
```

`npm run build` 会先执行 `vue-tsc --build`，再执行 `vite build`。
