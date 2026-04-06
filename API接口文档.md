# DailyCheck API 接口文档（RESTful 优化版）

> 更新时间：2026-04-03  
> 服务基路径：`/mm`  
> 认证方式：`Authorization: Bearer <access_token>`（除标注匿名接口外）

---

## 1. RESTful 设计说明

本次已对 `plans` 与 `checkins` 资源进行 RESTful 风格优化，主要包括：

- 资源化路径：使用 `/{resource}/{id}` 结构替代 `update/delete/detail/calendar` 动词路径。
- HTTP 方法语义化：
  - `POST` 创建
  - `GET` 查询
  - `PUT` 更新
  - `DELETE` 删除
- 向后兼容：保留旧路径（如 `POST /mm/plans/update`），并标记为兼容接口，建议客户端迁移到新路径。

---

## 2. 通用约定

### 2.1 请求头

```http
Content-Type: application/json
Authorization: Bearer <token>
```

### 2.2 时间与日期

- `DateOnly`：`yyyy-MM-dd`（如 `2026-04-03`）
- `TimeOnly`：`HH:mm:ss`（如 `08:30:00`）
- 服务内部主要以 UTC 存储时间戳。

### 2.3 状态码约定

- `200 OK`：查询/操作成功
- `201 Created`：创建成功
- `204 No Content`：更新/删除成功，无返回体
- `400 Bad Request`：参数错误/业务校验失败
- `401 Unauthorized`：认证失败或账号不可用
- `404 Not Found`：资源不存在
- `409 Conflict`：冲突（如重复打卡、重复账号）

---

## 3. 认证与账户（Auth）

控制器前缀：`/mm/auth`

### 3.1 匿名接口

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/register` | 邮箱+验证码注册 |
| POST | `/login` | 邮箱密码登录 |
| POST | `/login-account` | 用户名密码登录 |
| POST | `/login-email-code` | 邮箱验证码登录 |
| POST | `/refresh` | 刷新 AccessToken/RefreshToken |
| POST | `/email-code` | 发送邮箱验证码 |
| POST | `/wechat/register` | 微信注册 |
| POST | `/wechat/login` | 微信登录 |
| POST | `/wechat/login-auto` | 微信一键登录（自动注册或登录） |

### 3.2 需认证接口

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/deactivate` | 注销/停用账户 |
| POST | `/change-password` | 修改密码 |
| POST | `/profile` | 更新个人资料 |
| GET | `/me` | 获取当前登录用户资料 |
| GET | `/account/status` | 获取账号状态 |
| POST | `/account` | 更新账号信息 |
| POST | `/` | 登出（使当前会话失效） |
| POST | `/wechat/bind` | 绑定微信账号 |
| DELETE | `/wechat/unbind` | 解绑微信账号 |
| GET | `/bindings` | 获取第三方绑定信息 |

---

## 4. 打卡计划（Plans）

控制器前缀：`/mm/plans`

### 4.1 RESTful 新接口（推荐）

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/` | 获取当前用户所有计划 |
| GET | `/{planId}` | 获取单个计划详情 |
| POST | `/` | 创建计划 |
| PUT | `/{planId}` | 更新计划 |
| DELETE | `/{planId}` | 删除计划（软删除） |

### 4.2 兼容旧接口（不推荐）

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/update` | 兼容旧版更新（建议迁移 PUT） |
| POST | `/delete?planId={id}` | 兼容旧版删除（建议迁移 DELETE） |

### 4.3 创建/更新请求体示例

```json
{
  "title": "每日运动",
  "description": "每天至少30分钟",
  "isActive": true,
  "startDate": "2026-04-03",
  "endDate": "2026-12-31",
  "timeSlots": [
    {
      "slotName": "早晨",
      "startTime": "07:00:00",
      "endTime": "09:00:00",
      "orderNum": 1,
      "isActive": true
    }
  ]
}
```

### 4.4 关键业务规则

- 时间段必须满足 `startTime < endTime`。
- 同一计划内时间段不可重叠。
- 删除为软删除，不清理历史打卡数据。

---

## 5. 打卡记录（Checkins）

控制器前缀：`/mm/checkins`

### 5.1 RESTful 新接口（推荐）

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/` | 创建当日打卡 |
| POST | `/backfill` | 创建补打卡 |
| GET | `/plans/{planId}/calendar?year=2026&month=4` | 查询某计划月度日历状态 |
| GET | `/plans/{planId}/details?date=2026-04-03` | 查询某计划指定日期打卡详情 |

### 5.2 兼容旧接口（不推荐）

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/daily` | 兼容旧版当日打卡 |
| POST | `/retro` | 兼容旧版补打卡 |
| GET | `/calendar?planId=1&year=2026&month=4` | 兼容旧版日历查询 |
| GET | `/detail?planId=1&date=2026-04-03` | 兼容旧版详情查询 |

### 5.3 当日打卡请求体

```json
{
  "planId": 1,
  "imageUrls": [
    "/mm/files/images/a1.webp"
  ],
  "note": "完成晨跑",
  "timeSlotId": 10
}
```

### 5.4 补打卡请求体

```json
{
  "planId": 1,
  "date": "2026-04-01",
  "imageUrls": [
    "/mm/files/images/b2.webp"
  ],
  "note": "补录",
  "timeSlotId": 10
}
```

### 5.5 关键业务规则

- 单次打卡图片数量必须在 `1~3` 张。
- 补打卡日期不能晚于当天。
- 时间段计划必须传 `timeSlotId`。
- 同计划、同日期、同时间段不可重复打卡（冲突返回 `409`）。

---

## 6. 文件接口（Files）

控制器前缀：`/mm/files`

| 方法 | 路径 | 认证 | 说明 |
|---|---|---|---|
| POST | `/avatar` | 是 | 上传头像 |
| GET | `/users/{userId}/{key}` | 否 | 获取公开头像 |
| POST | `/images` | 是 | 上传打卡图片 |
| GET | `/images/{fileKey}` | 是 | 获取私有图片 |

### 6.1 上传限制

- 最大文件：10MB
- 空文件会返回 `400`
- 服务端会进行图片合法性校验并统一处理格式

---

## 7. 迁移建议（客户端）

1. **优先切换到 RESTful 新路径**（尤其是 `plans` 与 `checkins`）。
2. 在一个版本周期内保留旧路径调用兜底，完成灰度后移除。
3. 前端 API SDK 建议按“资源 + 方法”分组：
   - `plansApi.getById(id)` -> `GET /mm/plans/{id}`
   - `plansApi.update(id, body)` -> `PUT /mm/plans/{id}`
   - `checkinsApi.createToday(body)` -> `POST /mm/checkins`
4. 统一错误处理：对 `400/401/404/409` 做细分提示。

---

## 8. OpenAPI/Swagger

开发环境可通过 Swagger 访问在线接口定义（项目内已启用 `api.xml` 注释）。

建议将本文件作为“业务说明文档”，Swagger 作为“实时接口契约文档”联合使用。
