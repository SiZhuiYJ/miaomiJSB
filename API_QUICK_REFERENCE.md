# DailyCheck API 速查表

## 🚀 快速开始

### 基础信息
- **基础URL**: `/mm/`
- **认证方式**: Bearer Token
- **数据格式**: JSON

### 常用状态码
- `200`: 成功
- `201`: 创建成功  
- `204`: 更新/删除成功
- `400`: 参数错误
- `401`: 未认证
- `404`: 资源不存在

---

## 🔐 认证接口

### 用户注册
```
POST /auth/register
```
```json
{
  "email": "user@example.com",
  "password": "password123",
  "nickName": "昵称",
  "code": "123456"
}
```

### 登录方式
```
POST /auth/login           # 邮箱密码登录
POST /auth/login-account   # 账号密码登录  
POST /auth/login-email-code # 邮箱验证码登录
```

### 令牌管理
```
POST /auth/refresh         # 刷新Token
GET  /auth/me             # 获取用户信息
```

### 账户管理
```
POST /auth/change-password # 修改密码
POST /auth/profile         # 更新资料
POST /auth/account         # 修改账号名
POST /auth/deactivate      # 注销账户
```

---

## 📋 计划管理

### 计划操作
```
GET  /plans               # 获取计划列表
POST /plans               # 创建计划
POST /plans/update        # 更新计划
POST /plans/delete        # 删除计划
```

### 创建计划示例
```json
{
  "title": "学习计划",
  "description": "每日学习打卡",
  "startDate": "2024-01-15",
  "timeSlots": [
    {
      "slotName": "上午",
      "startTime": "09:00:00",
      "endTime": "11:00:00",
      "orderNum": 1
    }
  ]
}
```

---

## ⏰ 打卡管理

### 打卡操作
```
POST /checkins/daily      # 当日打卡
POST /checkins/retro      # 补打卡
GET  /checkins/calendar   # 月度日历
GET  /checkins/detail     # 打卡详情
```

### 打卡示例
```json
{
  "planId": 1,
  "imageUrls": ["image_url_1", "image_url_2"],
  "note": "今日学习心得",
  "timeSlotId": 101
}
```

---

## 📁 文件管理

### 文件操作
```
POST /files/avatar        # 上传头像
GET  /files/users/{id}/{key} # 获取头像
POST /files/images        # 上传图片
GET  /files/images/{key}  # 获取图片
```

### 上传格式
- **Content-Type**: multipart/form-data
- **文件大小**: ≤ 10MB
- **支持格式**: JPG/PNG/GIF等常见图片格式

---

## 📱 前端集成示例

### JavaScript/Fetch 示例
```javascript
// 登录
const login = async (email, password) => {
  const response = await fetch('/mm/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ email, password })
  });
  return response.json();
};

// 带认证的请求
const apiCall = async (url, options = {}) => {
  const token = localStorage.getItem('accessToken');
  return fetch(`/mm${url}`, {
    ...options,
    headers: {
      'Authorization': `Bearer ${token}`,
      ...options.headers
    }
  });
};

// 获取计划列表
const getPlans = () => apiCall('/plans').then(res => res.json());

// 当日打卡
const dailyCheckin = (data) => {
  return apiCall('/checkins/daily', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  });
};
```

### Axios 示例
```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: '/mm',
  timeout: 10000
});

// 添加认证拦截器
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// 使用示例
const createPlan = (planData) => {
  return api.post('/plans', planData);
};

const uploadAvatar = (file) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/files/avatar', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  });
};
```

---

## ⚠️ 重要注意事项

### 安全提醒
1. **Token保护**: 妥善保管Access Token和Refresh Token
2. **密码安全**: 使用强密码，定期更换
3. **及时刷新**: Access Token过期前主动刷新
4. **文件验证**: 上传真实有效的图片文件

### 性能优化
1. **批量操作**: 合理使用批量接口减少请求
2. **缓存利用**: 合理使用客户端缓存
3. **图片压缩**: 上传适当尺寸的图片
4. **错误重试**: 实现网络异常重试机制

### 错误处理
```javascript
const handleApiError = (error) => {
  if (error.response?.status === 401) {
    // Token过期，尝试刷新或重新登录
    refreshToken().catch(() => {
      // 刷新失败，跳转登录页
      window.location.href = '/login';
    });
  }
  throw error;
};
```

---

## 📞 技术支持

- **文档**: [API_DOCUMENTATION.md](./API_DOCUMENTATION.md)
- **Swagger**: [openapi.yaml](./openapi.yaml)
- **邮箱**: support@dailycheck.com
- **更新时间**: 2024-01-15

---
*版本: v1.0.0*