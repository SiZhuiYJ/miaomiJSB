# 认证API文档

## 概述

这是认证模块的API服务集合，包含了用户认证、账户管理等所有相关接口。

## API列表

### 邮箱相关
- `sendEmailCode(data)` - 发送邮箱验证码
- `validateUserAccount(userAccount)` - 验证用户名是否可用

### 登录相关
- `loginWithEmail(data)` - 邮箱登录
- `loginWithAccount(data)` - 账号登录

### 注册相关
- `register(data)` - 用户注册

### 账户管理
- `updateProfile(data)` - 更新用户资料
- `updatePassword(data)` - 更新用户密码
- `deactivateConfirm(data)` - 确认账号注销

## 使用示例

```typescript
import { authApi } from '@/features/auth/api'

// 发送验证码
await authApi.sendEmailCode({
  email: 'user@example.com',
  actionType: 'register'
})

// 邮箱登录
const response = await authApi.loginWithEmail({
  email: 'user@example.com',
  password: 'password123'
})

// 更新资料
await authApi.updateProfile({
  nickName: '新昵称',
  avatarKey: 'avatar-key'
})
```

## 类型定义

从同一目录导出了所有相关的类型定义：

```typescript
import type { 
  ActionType,
  AuthData, 
  PasswordPayload, 
  RegisterRecord 
} from '@/features/auth/api'
```

## 注意事项

1. 所有API方法都返回Promise，需要使用async/await或.then()处理
2. 方法名采用小驼峰命名法（camelCase）
3. 自动携带认证token（通过http拦截器实现）
4. 错误处理需要在调用处进行try/catch

## 版本历史

- v1.0.0: 初始版本，包含基础认证API
- v1.1.0: 重构API命名规范，统一使用小驼峰命名法