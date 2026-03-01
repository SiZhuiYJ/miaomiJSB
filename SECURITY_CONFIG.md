# 安全配置说明

## .gitignore 安全规则概述

本项目的 `.gitignore` 文件包含了全面的安全配置规则，用于防止敏感信息泄露到代码仓库中。

## 主要安全类别

### 1. JWT 和认证配置
- `api/appsettings.Production.json` - 生产环境配置文件
- `api/appsettings.*.json` - 所有环境配置文件
- `api/secrets.json` - 密钥配置文件
- `api/*.secrets` - 密钥相关文件

### 2. 环境变量文件
- `.env` - 基础环境变量
- `.env.*` - 各种环境变量文件
- `.env.local` - 本地环境变量
- `.env.*.local` - 本地特定环境变量

### 3. 数据库配置
- `api/ConnectionStrings.json` - 数据库连接字符串
- `api/connectionstrings.*` - 数据库连接配置
- `api/appsettings.json` - 包含数据库密码的实际配置文件

### 4. API 密钥和凭证
- `api/keys/` - API 密钥目录
- `api/credentials/` - 凭证文件目录
- `api/tokens/` - 认证令牌目录
- `api/certs/` - 证书文件目录
- `api/private/` - 私有配置目录

### 5. 加密证书和私钥
- `*.pem` - PEM 格式证书
- `*.key` - 私钥文件
- `*.crt` - CRT 证书文件
- `*.cert` - 证书文件
- `*.p12` - PKCS#12 格式文件
- `*.pfx` - PFX 格式文件
- `*.der` - DER 格式文件

### 6. SSH 密钥
- `.ssh/` - SSH 配置目录
- `id_rsa*` - RSA 密钥对
- `id_dsa*` - DSA 密钥对
- `id_ecdsa*` - ECDSA 密钥对
- `id_ed25519*` - Ed25519 密钥对

### 7. 第三方服务密钥
- Google API 密钥
- AWS 凭证
- Azure Key Vault 配置

### 8. 容器和编排工具密钥
- Docker secrets
- Kubernetes secrets
- 云服务商配置

### 9. OAuth 和认证令牌
- OAuth 令牌存储
- 访问令牌数据库
- 刷新令牌文件

### 10. 敏感日志和文档
- 包含密码的日志文件
- 敏感信息文本文件
- 数据库导出文件

## 重要提醒

⚠️ **请注意**：`api/appsettings.json` 文件目前包含以下敏感信息：
- 数据库连接字符串（用户名和密码）
- JWT 密钥
- SMTP 邮件服务器密码
- Redis 连接密码

建议：
1. 将敏感配置移到环境变量中
2. 使用密钥管理服务（如 Azure Key Vault, AWS Secrets Manager）
3. 创建 `appsettings.json.example` 作为模板文件
4. 在部署时通过 CI/CD 注入实际的密钥值

## 最佳实践

1. **永远不要提交真实的密钥到版本控制**
2. **使用配置模板文件**（如 `appsettings.json.example`）
3. **通过环境变量或密钥管理服务注入敏感配置**
4. **定期轮换密钥和密码**
5. **实施最小权限原则**

## 验证配置

可以使用以下命令检查是否有敏感文件被意外提交：

```bash
# 检查暂存区的敏感文件
git diff --cached --name-only | grep -E "(appsettings|\.env|\.key|\.pem)"

# 检查工作区的敏感文件
git ls-files | grep -E "(appsettings|\.env|\.key|\.pem)"
```

如发现敏感文件已被提交，请立即撤销提交并采取补救措施。