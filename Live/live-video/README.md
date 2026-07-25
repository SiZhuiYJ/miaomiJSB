# Live Video

`live-video` 是从 `live-demo` 拆分出的前后端分离版本。

## 结构

- `api`: ASP.NET Core Web API，提供 `/ws` WebSocket 信令和 `/api/health` 健康检查。
- `web`: Vue 3 + Vite 前端，提供主播推流、观众观看和弹幕界面。

## 本地运行

1. 启动后端：

   ```powershell
   cd api
   dotnet run
   ```

2. 启动前端：

   ```powershell
   cd web
   npm install
   npm run dev
   ```

3. 打开 Vite 输出的地址，默认会通过代理连接 `http://localhost:5092/ws`。

## 配置

前端默认使用当前站点的 `/ws`，开发时由 Vite 代理到后端。部署到不同域名时，在 `web/.env` 中设置：

```dotenv
VITE_API_BASE_URL=http://localhost:5092
```

如果 WebSocket 地址不能由 API 地址推导出来，可以改用：

```dotenv
VITE_SIGNALING_WS_URL=ws://localhost:5092/ws
```
