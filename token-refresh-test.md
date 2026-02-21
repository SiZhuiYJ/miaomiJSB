# Token刷新逻辑测试指南

## 🧪 测试场景

### 1. 正常刷新流程测试
**预期行为**：
- 当访问令牌过期时，自动调用刷新接口
- 刷新成功后更新本地存储的token
- 原始请求重新发送并成功

**测试步骤**：
1. 登录获取token
2. 等待访问令牌过期（或手动修改过期时间）
3. 发起需要认证的API请求
4. 观察控制台日志，应该看到：
   ```
   刷新token undefined isRefreshRequest: false
   尝试刷新token
   ```
5. 刷新成功后，原始请求应自动重试并成功

### 2. 刷新失败处理测试
**预期行为**：
- 刷新令牌也过期时，清除所有认证信息
- 跳转到登录页面
- 显示"登录过期，请重新登录"提示

**测试步骤**：
1. 登录后等待刷新令牌过期（7天）
2. 或手动清除Redis中的refresh_token键
3. 发起API请求
4. 应该看到：
   ```
   刷新请求返回401，清除认证信息
   ```
5. 页面应跳转到登录页

### 3. 自动刷新测试（新增功能）
**预期行为**：
- 在token过期前5分钟自动刷新
- 无需等待401错误发生
- 用户无感知续期

**测试步骤**：
1. 修改后端AccessTokenMinutes为较短时间（如2分钟）
2. 登录后观察控制台
3. 应该在约1分30秒后看到自动刷新日志
4. 检查localStorage中的token是否更新

## 🔧 调试技巧

### 查看当前token状态：
```javascript
// 在浏览器控制台执行
const authStore = useAuthStore()
console.log('Access Token:', authStore.accessToken)
console.log('Refresh Token:', authStore.refreshToken)
console.log('Expires At:', authStore.accessTokenExpiresAt)
```

### 手动触发刷新：
```javascript
// 强制清除token模拟过期
localStorage.removeItem('daily_check_auth')
// 然后发起任意API请求
```

### 检查Redis存储：
```bash
# 连接Redis客户端
redis-cli
# 查看用户token
GET refresh_token:1
GET access_token:1
```

## 📊 性能监控指标

1. **刷新成功率**：统计刷新接口的成功率
2. **平均刷新延迟**：从检测到401到完成刷新的时间
3. **自动刷新触发率**：避免401错误的比例
4. **用户体验指标**：因token过期导致的用户流失率

## ⚠️ 已知限制

1. **并发请求问题**：多个并发401请求可能导致多次刷新
2. **网络延迟**：在网络较差情况下，自动刷新可能不及时
3. **服务器时间同步**：客户端和服务端时间不同步可能影响判断

## ✅ 修复验证清单

- [x] 前端AuthData类型补充时间戳字段
- [x] 添加自动刷新定时器机制  
- [x] 完善token清理逻辑
- [x] 后端移除内存字典依赖，统一使用Redis
- [x] 保持原有的401触发刷新逻辑作为兜底
- [x] 添加完整的错误处理和用户提示