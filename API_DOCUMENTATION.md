# DailyCheck API 接口文档

## 概述

DailyCheck 是一个完整的打卡管理系统，提供用户认证、打卡计划管理、打卡记录管理、文件上传等核心功能。本文档详细描述了所有可用的API接口及其使用方法。

## 基础信息

- **API版本**: v1
- **基础URL**: `/mm/`
- **认证方式**: JWT Bearer Token
- **数据格式**: JSON
- **编码格式**: UTF-8

## 认证机制

### 双Token机制
系统采用双Token认证机制：
- **Access Token**: 短期令牌（默认30分钟），用于日常API访问
- **Refresh Token**: 长期令牌（默认7天），用于刷新Access Token

### 认证头格式
```
Authorization: Bearer <access_token>
```

## 错误响应格式

```json
{
  "message": "错误描述信息",
  "errorCode": "错误代码",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## 状态码说明

| 状态码 | 说明 |
|--------|------|
| 200 | 请求成功 |
| 201 | 创建成功 |
| 204 | 更新/删除成功 |
| 400 | 请求参数错误 |
| 401 | 未授权/认证失败 |
| 403 | 禁止访问 |
| 404 | 资源不存在 |
| 409 | 资源冲突 |
| 429 | 请求过于频繁 |
| 500 | 服务器内部错误 |

---

# 认证相关接口

## 用户注册

**POST** `/mm/auth/register`

创建新用户账号并自动登录。

### 请求参数
```json
{
  "email": "user@example.com",
  "password": "StrongPass123!",
  "nickName": "用户名",
  "userAccount": "custom_account",
  "code": "123456"
}
```

### 响应示例
```json
{
  "userId": 123456,
  "email": "user@example.com",
  "nickName": "用户名",
  "userAccount": "custom_account",
  "avatarKey": null,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "accessTokenExpiresAt": "2024-01-15T11:00:00Z",
  "refreshTokenExpiresAt": "2024-01-22T10:30:00Z"
}
```

## 邮箱密码登录

**POST** `/mm/auth/login`

使用邮箱和密码登录系统。

### 请求参数
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

## 账号密码登录

**POST** `/mm/auth/login-account`

使用自定义账号名和密码登录。

### 请求参数
```json
{
  "userAccount": "custom_account",
  "password": "password123"
}
```

## 邮箱验证码登录

**POST** `/mm/auth/login-email-code`

通过邮箱验证码无密码登录。

### 请求参数
```json
{
  "email": "user@example.com",
  "code": "123456"
}
```

## 刷新令牌

**POST** `/mm/auth/refresh`

使用Refresh Token获取新的双Token。

### 请求参数
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIs..."
}
```

## 发送邮箱验证码

**POST** `/mm/auth/email-code`

向指定邮箱发送验证码。

### 请求参数
```json
{
  "email": "user@example.com",
  "actionType": "register"
}
```

### actionType 说明
- `register` / `signup`: 用户注册
- `login`: 无密码登录
- `change-password`: 修改密码
- `deactivate`: 账号注销
- `reset-password`: 密码重置

## 获取当前用户信息

**GET** `/mm/auth/me`

获取当前登录用户的基本信息。

### 响应示例
```json
{
  "userId": 123456,
  "email": "user@example.com",
  "nickName": "用户名",
  "userAccount": "custom_account",
  "avatarKey": "avatar_key_123"
}
```

## 修改密码

**POST** `/mm/auth/change-password`

修改用户登录密码。

### 请求参数
```json
{
  "oldPassword": "old_password",
  "newPassword": "new_password123",
  "code": "123456"
}
```

> 注：oldPassword 和 code 至少提供一个进行身份验证

## 更新个人资料

**POST** `/mm/auth/profile`

更新用户昵称和头像。

### 请求参数
```json
{
  "nickName": "新昵称",
  "avatarKey": "avatar_file_key"
}
```

## 账号名修改状态检查

**GET** `/mm/auth/account/status`

检查当前用户是否可以修改账号名。

### 响应示例
```json
{
  "canUpdate": true,
  "nextUpdateAt": "2025-01-15T10:30:00Z"
}
```

## 更新账号名

**POST** `/mm/auth/account`

修改用户自定义账号名（每年限一次）。

### 请求参数
```json
{
  "userAccount": "new_account_name"
}
```

## 账号注销

**POST** `/mm/auth/deactivate`

永久删除用户账户。

### 请求参数
```json
{
  "code": "123456"
}
```

---

# 打卡计划管理接口

## 获取计划列表

**GET** `/mm/plans`

获取当前用户的所有打卡计划。

### 响应示例
```json
[
  {
    "id": 1,
    "checkinMode": 1,
    "title": "每日学习计划",
    "description": "坚持每日学习打卡",
    "startDate": "2024-01-01",
    "endDate": "2024-12-31",
    "isActive": true,
    "timeSlots": [
      {
        "id": 101,
        "slotName": "早晨",
        "startTime": "08:00:00",
        "endTime": "10:00:00",
        "orderNum": 1,
        "isActive": true
      }
    ]
  }
]
```

## 创建打卡计划

**POST** `/mm/plans`

创建新的打卡计划。

### 请求参数
```json
{
  "title": "新计划标题",
  "description": "计划描述",
  "startDate": "2024-01-15",
  "endDate": "2024-12-31",
  "timeSlots": [
    {
      "slotName": "上午",
      "startTime": "09:00:00",
      "endTime": "11:00:00",
      "orderNum": 1,
      "isActive": true
    }
  ]
}
```

## 更新打卡计划

**POST** `/mm/plans/update`

更新现有打卡计划信息。

### 请求参数
```json
{
  "id": 1,
  "title": "更新后的标题",
  "description": "更新后的描述",
  "isActive": true,
  "startDate": "2024-01-15",
  "endDate": "2024-12-31",
  "timeSlots": [
    {
      "slotName": "上午",
      "startTime": "09:00:00",
      "endTime": "11:00:00",
      "orderNum": 1,
      "isActive": true
    }
  ]
}
```

## 删除打卡计划

**POST** `/mm/plans/delete`

删除指定的打卡计划（软删除）。

### 请求参数
```json
{
  "PlanId": 1
}
```

---

# 打卡记录管理接口

## 当日打卡

**POST** `/mm/checkins/daily`

记录用户当天的打卡行为。

### 请求参数
```json
{
  "planId": 1,
  "imageUrls": ["image_url_1", "image_url_2"],
  "note": "今日学习心得",
  "timeSlotId": 101
}
```

## 补打卡

**POST** `/mm/checkins/retro`

补录历史日期的打卡记录。

### 请求参数
```json
{
  "planId": 1,
  "date": "2024-01-10",
  "imageUrls": ["image_url_1"],
  "note": "补打卡说明",
  "timeSlotId": 101
}
```

## 获取月度打卡日历

**GET** `/mm/checkins/calendar`

获取指定计划的月度打卡状态。

### 请求参数
- `planId`: 打卡计划ID
- `year`: 年份（如：2024）
- `month`: 月份（1-12）

### 响应示例
```json
[
  {
    "date": "2024-01-15",
    "status": 1,
    "checkinMode": 1,
    "timeSlots": [
      {
        "checkinId": 123,
        "timeSlotId": 101,
        "status": 1
      }
    ]
  }
]
```

## 获取打卡详情

**GET** `/mm/checkins/detail`

获取指定日期的详细打卡记录。

### 请求参数
- `planId`: 打卡计划ID
- `date`: 日期（格式：yyyy-MM-dd）

### 响应示例
```json
[
  {
    "date": "2024-01-15",
    "status": 1,
    "note": "今日学习打卡",
    "imageUrls": ["url1", "url2"],
    "timeSlotId": 101
  }
]
```

---

# 文件管理接口

## 上传头像

**POST** `/mm/files/avatar`

上传用户头像图片。

### 请求格式
- Method: POST
- Content-Type: multipart/form-data
- File size limit: 10MB

### 响应示例
```json
{
  "key": "avatar_file_key_123"
}
```

## 获取用户头像

**GET** `/mm/files/users/{userId}/{key}`

获取指定用户的头像图片（公开访问）。

## 上传普通图片

**POST** `/mm/files/images`

上传普通图片文件（需认证）。

### 请求格式
- Method: POST
- Content-Type: multipart/form-data
- File size limit: 10MB

### 响应示例
```json
{
  "url": "/mm/files/images/image_file_key_123"
}
```

## 获取用户图片

**GET** `/mm/files/images/{fileKey}`

获取用户上传的图片文件（需认证）。

---

# 数据模型定义

## 用户相关模型

### RegisterRequest
```typescript
{
  email: string;        // 邮箱地址
  password: string;     // 登录密码
  nickName?: string;    // 用户昵称（可选）
  userAccount?: string; // 自定义账号名（可选）
  code: string;         // 邮箱验证码
}
```

### AuthResponse
```typescript
{
  userId: number;              // 用户ID
  email: string;               // 邮箱地址
  nickName?: string;           // 用户昵称
  userAccount?: string;        // 自定义账号名
  avatarKey?: string;          // 头像文件Key
  token: string;               // Access Token
  refreshToken: string;        // Refresh Token
  accessTokenExpiresAt: Date;  // Access Token过期时间
  refreshTokenExpiresAt: Date; // Refresh Token过期时间
}
```

## 计划相关模型

### CreatePlanRequest
```typescript
{
  title: string;              // 计划标题
  description?: string;       // 计划描述
  startDate?: Date;           // 开始日期
  endDate?: Date;             // 结束日期
  timeSlots?: TimeSlotDto[];  // 时间段配置
}
```

### TimeSlotDto
```typescript
{
  id?: number;        // 时间段ID（创建时为空）
  slotName?: string;  // 时间段名称
  startTime: string;  // 开始时间（HH:mm:ss）
  endTime: string;    // 结束时间（HH:mm:ss）
  orderNum: number;   // 排序序号
  isActive: boolean;  // 是否启用
}
```

## 打卡相关模型

### DailyCheckinRequest
```typescript
{
  planId: number;         // 打卡计划ID
  imageUrls?: string[];   // 图片URL列表
  note?: string;          // 打卡备注
  timeSlotId?: number;    // 时间段ID（可选）
}
```

### CalendarStatusItem
```typescript
{
  date: Date;                     // 日期
  checkinMode: number;            // 打卡模式（0=默认，1=时间段）
  status?: number;                // 打卡状态
  timeSlots?: TimeSlotStatusItem[]; // 时间段状态详情
}
```

---

# 最佳实践建议

## 安全建议
1. **Token管理**: 妥善保管Access Token和Refresh Token
2. **密码安全**: 使用强密码，定期更换
3. **验证码保护**: 及时使用收到的验证码
4. **文件上传**: 上传真实有效的图片文件

## 性能优化
1. **Token刷新**: 在Access Token过期前主动刷新
2. **批量操作**: 合理使用批量接口减少请求次数
3. **缓存利用**: 合理使用客户端缓存机制
4. **图片优化**: 上传适当尺寸的图片文件

## 错误处理
1. **网络异常**: 实现重试机制
2. **认证失效**: 及时刷新或重新登录
3. **参数验证**: 前端做好参数校验
4. **用户提示**: 提供友好的错误信息

---

# 更新日志

## v1.0.0 (2024-01-15)
- 初始版本发布
- 实现完整的认证系统
- 支持打卡计划管理
- 提供打卡记录功能
- 集成文件上传服务

---
*文档版本: 1.0.0*
*最后更新: 2024-01-15*