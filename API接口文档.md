# DailyCheck API 接口文档（按最新控制器同步）

> 更新时间：2026-04-06  
> 服务基路径：`/mm`  
> 认证方式：`Authorization: Bearer <access_token>`（标记匿名的接口除外）

---

## 1. 认证与账户（Auth）

控制器：`AuthController`  
路由前缀：`/mm/auth`

### 1.1 匿名接口

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/register` | 邮箱 + 验证码注册 |
| POST | `/login` | 邮箱密码登录 |
| POST | `/login-account` | 用户名密码登录 |
| POST | `/login-email-code` | 邮箱验证码登录 |
| POST | `/refresh` | 使用 refresh token 刷新登录态 |
| POST | `/email-code` | 发送邮箱验证码 |
| POST | `/validate-account?userAccount=xxx` | 校验用户名是否可用 |
| POST | `/wechat/register` | 微信注册 |
| POST | `/wechat/login` | 微信登录（仅已注册） |
| POST | `/wechat/login-auto` | 微信一键登录（自动注册或登录） |

### 1.2 需认证接口

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/deactivate` | 注销/停用账户 |
| POST | `/change-password` | 修改密码 |
| POST | `/profile` | 更新个人资料 |
| GET | `/me` | 获取当前用户资料 |
| GET | `/account/status` | 获取账号更新状态 |
| POST | `/account` | 更新账号（用户名） |
| POST | `/` | 登出（使会话失效） |
| POST | `/wechat/bind` | 绑定微信 |
| DELETE | `/wechat/unbind` | 解绑微信 |
| GET | `/bindings` | 获取第三方绑定状态 |

---

## 2. 打卡计划（Plans）

控制器：`PlansController`  
路由前缀：`/mm/plans`

### 2.1 新接口（统一使用）

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/` | 获取当前用户所有计划 |
| GET | `/{planId}` | 获取单个计划详情 |
| POST | `/` | 创建计划 |
| PUT | `/{planId}` | 更新计划 |
| DELETE | `/{planId}` | 删除计划（软删除） |

### 2.2 兼容旧接口（仅兼容，不建议继续使用）

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/update` | 旧版更新接口 |
| POST | `/delete?planId={id}` | 旧版删除接口 |

---

## 3. 打卡记录（Checkins）

控制器：`CheckinsController`  
路由前缀：`/mm/checkins`

### 3.1 新接口（统一使用）

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/` | 当日打卡 |
| POST | `/backfill` | 补打卡 |
| GET | `/plans/{planId}/calendar?year=YYYY&month=MM` | 获取计划月历打卡状态 |
| GET | `/plans/{planId}/details?date=YYYY-MM-DD` | 获取计划某日打卡详情 |

### 3.2 兼容旧接口（仅兼容，不建议继续使用）

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/daily` | 旧版当日打卡 |
| POST | `/retro` | 旧版补打卡 |
| GET | `/calendar?planId=1&year=YYYY&month=MM` | 旧版月历查询 |
| GET | `/detail?planId=1&date=YYYY-MM-DD` | 旧版详情查询 |

---

## 4. 文件（Files）

控制器：`FilesController`  
路由前缀：`/mm/files`

| 方法 | 路径 | 认证 | 说明 |
|---|---|---|---|
| POST | `/avatar` | 是 | 上传头像（返回 `key`） |
| GET | `/users/{userId}/{key}` | 否 | 获取公开头像 |
| POST | `/images` | 是 | 上传业务图片（返回 `url`） |
| GET | `/images/{fileKey}` | 是 | 获取私有图片 |

上传限制：
- 单文件最大 10MB
- 必须是图片文件

---

## 5. 本次客户端迁移结论

- `web` 与 `uni-app` 中 `plans/checkins` 已统一迁移到新接口：
  - `POST /mm/plans/update` -> `PUT /mm/plans/{planId}`
  - `POST /mm/plans/delete?planId=...` -> `DELETE /mm/plans/{planId}`
  - `POST /mm/checkins/daily` -> `POST /mm/checkins`
  - `POST /mm/checkins/retro` -> `POST /mm/checkins/backfill`
  - `GET /mm/checkins/calendar?planId=...` -> `GET /mm/checkins/plans/{planId}/calendar`
  - `GET /mm/checkins/detail?planId=...` -> `GET /mm/checkins/plans/{planId}/details`

