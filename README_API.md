# DailyCheck API 文档汇总

## 📚 文档列表

本项目提供了完整的API文档体系，包含以下文档：

### 1. 详细接口文档
**文件**: [`API_DOCUMENTATION.md`](./API_DOCUMENTATION.md)

**内容涵盖**:
- 完整的API接口说明
- 详细的请求/响应示例
- 数据模型定义
- 最佳实践建议
- 错误处理指南

**适用场景**: 开发人员完整了解系统API设计

### 2. OpenAPI/Swagger规范
**文件**: [`openapi.yaml`](./openapi.yaml)

**内容涵盖**:
- 标准化的OpenAPI 3.0规范
- 可导入Swagger UI工具
- 支持代码自动生成
- 完整的接口定义和示例

**适用场景**: API测试、文档生成、客户端SDK生成

### 3. 快速参考手册
**文件**: [`API_QUICK_REFERENCE.md`](./API_QUICK_REFERENCE.md)

**内容涵盖**:
- 常用接口速查表
- 前端集成示例代码
- 错误码对照表
- 性能优化建议

**适用场景**: 日常开发快速查阅

### 4. 代码注释增强
**文件**: 
- [`AuthController.cs`](./api/Controllers/AuthController.cs)
- [`CheckinsController.cs`](./api/Controllers/CheckinsController.cs)
- [`PlansController.cs`](./api/Controllers/PlansController.cs)
- [`FilesController.cs`](./api/Controllers/FilesController.cs)

**改进内容**:
- 详细的XML文档注释
- 方法级别详细说明
- 参数和返回值描述
- 业务逻辑解释

**适用场景**: 代码维护、IDE智能提示

## 🚀 使用方式

### 在线查看Swagger文档
API服务启动后，可通过以下URL访问Swagger UI：
```
http://localhost:5210/swagger
```

### 本地文档浏览
直接打开对应的markdown文件即可查看详细内容。

### 集成开发
1. 使用`openapi.yaml`生成客户端SDK
2. 参考`API_QUICK_REFERENCE.md`进行快速开发
3. 查阅`API_DOCUMENTATION.md`了解完整细节

## 📖 文档特色

### 完整性
- 覆盖所有API接口
- 包含详细的错误处理说明
- 提供多种使用场景示例

### 实用性
- 提供实际可运行的代码示例
- 包含前后端集成指导
- 给出性能优化建议

### 标准化
- 遵循RESTful API设计原则
- 使用标准HTTP状态码
- 提供OpenAPI规范文档

## 🔧 技术栈

- **后端框架**: ASP.NET Core 10.0
- **API文档**: Swagger/OpenAPI 3.0
- **认证机制**: JWT双Token
- **数据格式**: JSON
- **文件存储**: 本地文件系统 + WebP优化

## 📞 支持与反馈

如有任何问题或建议，请联系：
- 邮箱: support@dailycheck.com
- 文档更新时间: 2024-01-15
- 当前版本: v1.0.0

---
*Happy Coding! ✨*