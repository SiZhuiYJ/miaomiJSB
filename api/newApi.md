# DailyCheck API 接口文档

## 文档版本
- 版本：v1.0
- 最后更新：2026-02-20
- 作者：API开发团队

## 通用说明

### 基础配置
- **基础地址**：`https://{host}:{port}`（本地调试通常是 `https://localhost:5001` 或 `https://localhost:7200` 等）
- **API前缀**：所有接口均以 `/mm` 为前缀
- **数据格式**：所有接口返回 JSON 格式数据
- **日期时间格式**：
  - 日期：`yyyy-MM-dd`（对应后端的 `DateOnly`）
  - 时间：`HH:mm:ss`（对应后端的 `TimeOnly`）
  - 日期时间：ISO 8601 格式

### 认证机制
- **JWT Token认证**：受保护接口需要在请求头中携带JWT
  ```
  Authorization: Bearer {access_token}
  ```
- **双Token机制**：
  - `access_token`：短期访问令牌，有效期30分钟
  - `refresh_token`：长期刷新令牌，有效期7天
- **单点登录**：同一用户只能有一个活跃会话，新登录会踢掉旧会话

### 文件上传说明
- **图片格式支持**：`.jpg`、`.jpeg`、`.png`、`.gif`、`.webp`、`.bmp`
- **文件大小限制**：单文件最大10MB
- **格式转换**：所有上传图片自动转换为WebP格式存储
- **访问控制**：
  - 普通图片：仅上传用户可访问
  - 头像图片：公开访问

---

## 认证模块（AuthController）

**基址**：`/mm/Auth`

### 1. 用户注册 `/mm/Auth/register`
**方法**：`POST`  
**认证**：否  

**请求体**（JSON）
```json
{
  "email": "user@example.com",
  "password": "secure_password123",
  "nickName": "用户昵称",
  "userAccount": "unique_account_name",
  "code": "123456"
}
```

**字段说明**：
- `email`：必填，用户邮箱地址，需唯一
- `password`：必填，登录密码，建议包含大小写字母、数字和特殊字符
- `nickName`：可选，用户昵称
- `userAccount`：可选，用户自定义账号名，需全局唯一
- `code`：必填，邮箱验证码（通过发送验证码接口获取）

**成功响应 200**
```json
{
  "userId": 123456,
  "email": "user@example.com",
  "nickName": "用户昵称",
  "userAccount": "unique_account_name",
  "avatarKey": "头像文件标识",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "accessTokenExpiresAt": "2026-02-20T15:30:00Z",
  "refreshTokenExpiresAt": "2026-02-27T15:00:00Z"
}
```

**错误响应**：
- `400 Bad Request`：参数缺失、验证码无效或已过期
- `409 Conflict`：邮箱已被注册或账号名已被占用

---

### 2. 邮箱登录 `/mm/Auth/login`
**方法**：`POST`  
**认证**：否  

**请求体**（JSON）
```json
{
  "email": "user@example.com",
  "password": "secure_password123"
}
```

**成功响应 200**
返回结构与注册接口相同，包含完整的用户信息和认证令牌。

**错误响应**：
- `401 Unauthorized`：邮箱不存在、密码错误或账号被禁用

---

### 3. 账号登录 `/mm/Auth/login-account`
**方法**：`POST`  
**认证**：否  

**请求体**（JSON）
```json
{
  "userAccount": "unique_account_name",
  "password": "secure_password123"
}
```

**成功响应 200**
返回结构与邮箱登录相同。

**错误响应**：
- `400 Bad Request`：账号名为空
- `401 Unauthorized`：账号不存在、密码错误或账号被禁用

---

### 4. 刷新令牌 `/mm/Auth/refresh`
**方法**：`POST`  
**认证**：否（使用body中的refreshToken）

**请求体**（JSON）
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**成功响应 200**
返回新的双Token信息，结构与登录接口相同。

**错误响应**：
- `401 Unauthorized`：refreshToken无效、过期或用户状态异常

---

### 5. 发送邮箱验证码 `/mm/Auth/email-code`
**方法**：`POST`  
**认证**：否  

**请求体**（JSON）
```json
{
  "email": "user@example.com",
  "actionType": "register"
}
```

**actionType支持值**：
- `register/signup`：注册验证码（邮箱必须未注册）
- `login`：登录验证码（邮箱必须已注册）
- `change-password`：修改密码验证码
- `deactivate`：账号注销验证码
- `reset-password`：重置密码验证码

**成功响应 200**
```json
{
  "message": "验证码发送成功"
}
```

**错误响应**：
- `400 Bad Request`：邮箱为空、actionType不合法或邮箱状态不匹配
- `429 Too Many Requests`：请求过于频繁（1分钟内限制1次）
- `500 Internal Server Error`：邮件服务异常

---

### 6. 验证账号名可用性 `/mm/Auth/validate-account`
**方法**：`POST`  
**认证**：否  

**查询参数**：
- `UserAccount`：必填，待验证的账号名

**成功响应 200**
表示账号名可用

**错误响应**：
- `400 Bad Request`：账号名为空
- `409 Conflict`：账号名已被占用

---

### 7. 修改密码 `/mm/Auth/change-password`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "oldPassword": "current_password",
  "newPassword": "new_secure_password123",
  "code": "123456"
}
```

**验证方式**：`oldPassword` 和 `code` 至少提供一个

**成功响应 204 No Content**

**错误响应**：
- `400 Bad Request`：新密码为空或验证信息不足
- `401 Unauthorized`：旧密码错误或账号状态异常
- `404 Not Found`：用户不存在

---

### 8. 注销账号 `/mm/Auth/deactivate`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "code": "123456"
}
```

**说明**：
- 需要提供有效的邮箱验证码
- 注销后用户无法登录，数据软删除
- 清理Redis中的用户令牌

**成功响应 204 No Content**

**错误响应**：
- `400 Bad Request`：验证码无效或已过期
- `404 Not Found`：用户不存在

---

### 9. 更新个人资料 `/mm/Auth/profile`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "nickName": "新昵称",
  "avatarKey": "头像文件标识"
}
```

**说明**：
- 两个字段均为可选，不传则保持原值
- 更新成功后重新生成认证令牌

**成功响应 200**
返回更新后的用户信息和新的认证令牌

---

### 10. 获取当前用户信息 `/mm/Auth/me`
**方法**：`GET`  
**认证**：是  

**成功响应 200**
```json
{
  "userId": 123456,
  "email": "user@example.com",
  "nickName": "用户昵称",
  "userAccount": "unique_account_name",
  "avatarKey": "头像文件标识"
}
```

**错误响应**：
- `401 Unauthorized`：账号被禁用或会话过期
- `404 Not Found`：用户不存在

---

### 11. 检查账号名修改状态 `/mm/Auth/account/status`
**方法**：`GET`  
**认证**：是  

**成功响应 200**
```json
{
  "canUpdate": true,
  "nextUpdateAt": null
}
```

或不可修改时：
```json
{
  "canUpdate": false,
  "nextUpdateAt": "2027-02-20T15:00:00Z"
}
```

---

### 12. 修改账号名 `/mm/Auth/account`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "userAccount": "new_unique_account"
}
```

**规则**：每个用户每年只能修改一次账号名

**成功响应 200**
返回更新后的用户信息和新的认证令牌

**错误响应**：
- `400 Bad Request`：账号名为空
- `403 Forbidden`：修改频率超限（未满一年）
- `409 Conflict`：账号名已被占用

---

## 打卡计划模块（PlansController）

**基址**：`/mm/Plans`  
**认证**：所有接口都需要 `Authorization: Bearer {token}`

### 1. 获取我的计划列表 `/mm/Plans`
**方法**：`GET`  
**认证**：是  

**成功响应 200**
```json
[
  {
    "id": 123456,
    "title": "早起打卡计划",
    "description": "每天7点前起床打卡",
    "startDate": "2026-01-01",
    "endDate": "2026-12-31",
    "isActive": true,
    "timeSlots": [
      {
        "id": 789,
        "slotName": "早晨时段",
        "startTime": "06:00:00",
        "endTime": "08:00:00",
        "orderNum": 1,
        "isActive": true
      }
    ]
  }
]
```

---

### 2. 创建打卡计划 `/mm/Plans`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "title": "健身打卡计划",
  "description": "每周健身3次",
  "startDate": "2026-02-20",
  "endDate": "2026-05-20",
  "timeSlots": [
    {
      "slotName": "晨练",
      "startTime": "06:00:00",
      "endTime": "08:00:00",
      "orderNum": 1,
      "isActive": true
    },
    {
      "slotName": "晚练",
      "startTime": "18:00:00",
      "endTime": "20:00:00",
      "orderNum": 2,
      "isActive": true
    }
  ]
}
```

**字段说明**：
- `title`：必填，计划标题
- `description`：可选，计划详细描述
- `startDate`：可选，默认为当天
- `endDate`：可选，计划结束日期
- `timeSlots`：可选，时间段配置列表

**成功响应 201**
```json
{
  "id": 123457,
  "title": "健身打卡计划",
  "description": "每周健身3次",
  "startDate": "2026-02-20",
  "endDate": "2026-05-20",
  "isActive": true,
  "timeSlots": [...]
}
```

**错误响应**：
- `400 Bad Request`：时间段设置无效（时间重叠或开始时间晚于结束时间）

---

### 3. 更新打卡计划 `/mm/Plans/update`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "id": 123456,
  "title": "更新后的计划标题",
  "description": "更新后的描述",
  "isActive": true,
  "startDate": "2026-02-20",
  "endDate": "2026-06-20",
  "timeSlots": [...]
}
```

**说明**：
- `id` 必填
- 其他字段可选，不传则保持原值
- `timeSlots` 采用全量更新策略

**成功响应 204 No Content**

**错误响应**：
- `400 Bad Request`：参数错误或时间段设置无效
- `404 Not Found`：计划不存在或无权限访问

---

### 4. 删除打卡计划 `/mm/Plans/delete`
**方法**：`POST`  
**认证**：是  

**查询参数**：
- `PlanId`：必填，要删除的计划ID

**说明**：采用软删除机制

**成功响应 204 No Content**

**错误响应**：
- `404 Not Found`：计划不存在或无权限访问

---

## 打卡模块（CheckinsController）

**基址**：`/mm/Checkins`  
**认证**：所有接口都需要 `Authorization: Bearer {token}`

### 1. 当日打卡 `/mm/Checkins/daily`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "planId": 123456,
  "timeSlotId": 789,
  "imageUrls": [
    "/mm/files/images/file_key_1",
    "/mm/files/images/file_key_2"
  ],
  "note": "今天的打卡备注"
}
```

**字段说明**：
- `planId`：必填，打卡计划ID
- `timeSlotId`：可选，时间段ID（对于有时段的计划必填）
- `imageUrls`：必填，1-3张图片URL
- `note`：可选，打卡备注

**成功响应 200**
```json
{
  "message": "打卡成功"
}
```

**错误响应**：
- `400 Bad Request`：计划未开始、时间不在范围内、图片数量不符要求
- `404 Not Found`：计划不存在
- `409 Conflict`：当天已打卡（重复打卡）

---

### 2. 补打卡 `/mm/Checkins/retro`
**方法**：`POST`  
**认证**：是  

**请求体**（JSON）
```json
{
  "planId": 123456,
  "date": "2026-02-18",
  "timeSlotId": 789,
  "imageUrls": ["/mm/files/images/file_key_1"],
  "note": "补打卡备注"
}
```

**说明**：
- `date` 必须是过去日期
- 区分正常打卡(status=1)和补打卡(status=2)

**成功响应 200**
```json
{
  "message": "补打卡成功"
}
```

**错误响应**：
- `400 Bad Request`：日期无效、时间验证失败、图片数量不符
- `404 Not Found`：计划不存在
- `409 Conflict`：指定日期已有打卡记录

---

### 3. 获取打卡日历 `/mm/Checkins/calendar`
**方法**：`GET`  
**认证**：是  

**查询参数**：
- `planId`：必填，打卡计划ID
- `year`：必填，年份（如2026）
- `month`：必填，月份（1-12）

**示例请求**：
```
GET /mm/Checkins/calendar?planId=123456&year=2026&month=2
```

**成功响应 200**
```json
[
  {
    "date": "2026-02-01",
    "status": 1,
    "checkinMode": 0
  },
  {
    "date": "2026-02-02",
    "status": 2,
    "checkinMode": 1,
    "timeSlots": [
      {
        "checkinId": 789012,
        "timeSlotId": 789,
        "status": 2
      }
    ]
  }
]
```

**状态说明**：
- `status`：1=正常打卡，2=补打卡
- `checkinMode`：0=默认模式，1=时间段模式

---

### 4. 获取打卡详情 `/mm/Checkins/detail`
**方法**：`GET`  
**认证**：是  

**查询参数**：
- `planId`：必填，打卡计划ID
- `date`：必填，查询日期（格式：yyyy-MM-dd）

**示例请求**：
```
GET /mm/Checkins/detail?planId=123456&date=2026-02-18
```

**成功响应 200**
```json
[
  {
    "date": "2026-02-18",
    "status": 1,
    "note": "今日打卡备注",
    "imageUrls": [
      "/mm/files/images/file_key_1",
      "/mm/files/images/file_key_2"
    ],
    "timeSlotId": 789
  }
]
```

**错误响应**：
- `404 Not Found`：指定日期无打卡记录

---

## 文件管理模块（FilesController）

**基址**：`/mm/Files`  
**认证**：除头像获取外，其他接口都需要认证

### 1. 上传头像 `/mm/Files/avatar`
**方法**：`POST`  
**认证**：是  
**Content-Type**：`multipart/form-data`

**表单字段**：
- `file`：必填，图片文件

**成功响应 200**
```json
{
  "key": "avatar_file_key_123456"
}
```

**说明**：
- 上传成功后自动更新用户头像链接
- 图片转换为WebP格式存储
- 支持公开访问

---

### 2. 获取用户头像 `/mm/Files/users/{userId}/{key}`
**方法**：`GET`  
**认证**：否（公开接口）

**路径参数**：
- `userId`：用户ID
- `key`：头像文件Key

**成功响应 200**
返回图片二进制流，Content-Type为`image/webp`

---

### 3. 上传图片 `/mm/Files/images`
**方法**：`POST`  
**认证**：是  
**Content-Type**：`multipart/form-data`

**表单字段**：
- `file`：必填，图片文件

**成功响应 200**
```json
{
  "url": "/mm/files/images/image_file_key_123456"
}
```

**说明**：
- 仅上传用户可访问
- 返回相对路径URL
- 图片转换为WebP格式

---

### 4. 获取图片 `/mm/Files/images/{fileKey}`
**方法**：`GET`  
**认证**：是

**路径参数**：
- `fileKey`：图片文件标识

**成功响应 200**
返回图片二进制流，Content-Type为`image/webp`

**错误响应**：
- `404 Not Found`：文件不存在或无访问权限

---

## 错误码说明

| 状态码 | 说明 |
|--------|------|
| 200 | 请求成功 |
| 201 | 创建成功 |
| 204 | 操作成功但无返回内容 |
| 400 | 请求参数错误 |
| 401 | 未授权或认证失败 |
| 403 | 禁止访问（如频率限制） |
| 404 | 资源不存在 |
| 409 | 资源冲突（如重复注册） |
| 429 | 请求过于频繁 |
| 500 | 服务器内部错误 |

---

## 使用流程建议

1. **用户注册/登录**
   ```
   POST /mm/Auth/register 或 POST /mm/Auth/login
   → 获取 access_token 和 refresh_token
   ```

2. **创建打卡计划**
   ```
   POST /mm/Plans
   → 创建个人打卡计划
   ```

3. **上传打卡图片**
   ```
   POST /mm/Files/images
   → 上传打卡凭证图片，获取图片URL
   ```

4. **执行打卡**
   ```
   POST /mm/Checkins/daily
   → 完成当日打卡
   ```

5. **查看打卡记录**
   ```
   GET /mm/Checkins/calendar
   → 查看月度打卡日历
   
   GET /mm/Checkins/detail?date=2026-02-18
   → 查看具体日期详情
   ```

6. **令牌续期**
   ```
   POST /mm/Auth/refresh
   → 使用refresh_token获取新的access_token
   ```

---
*文档版本：v1.0 | 最后更新：2026-02-20*