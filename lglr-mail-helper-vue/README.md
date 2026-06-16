# 无尽的拉格朗日公告邮件撰写助手（Vue 版）

该目录是对 [`hunyanjie/lglr_mail_helper`](https://github.com/hunyanjie/lglr_mail_helper) 的前端化改造，使用 Vue 3、TypeScript、Vite 与 SCSS 实现浏览器端公告邮件编辑器。

## 已实现功能

- 彩色公告文本预览与内容编辑
- 选中文本后应用预设或自定义颜色
- 坐标格式 `(123,456)` 自动高亮
- 拉格朗日格式导入与导出
- 输出文本长度实时检查（300 字提示）
- 复制输出与剪贴板覆盖粘贴

## 本地运行

```bash
npm install
npm run dev
```

## 构建

```bash
npm run build
```
