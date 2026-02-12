太棒了！就用「本地文件 + 外部配置」这个最实用又不复杂的方案 ✅

下面给你一份 **完整、可直接复制粘贴使用** 的代码包，包含：

- 📦 完整的 `EmailService.cs`
- 🗂 推荐的目录结构
- 📄 模板文件示例（HTML + TXT）
- ⚙️ `appsettings.json` 配置
- 🔌 `Program.cs` 注册方式

---

## ✅ 最终目标结构

```bash
YourApp/
├── YourApp.csproj
├── Program.cs
└── config/
    └── templates/
        └── email/
            ├── verification.html   # HTML模板
            └── verification.txt    # 纯文本模板
```

> 💡 `config/` 是个独立文件夹，不在项目内编译，支持随时修改不重启！

---

## 📁 文件一：`EmailService.cs`（完整版）

```csharp
// EmailService.cs
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace YourApp.Services
{
    public interface IEmailService
    {
        Task<bool> SendVerificationCodeAsync(string toEmail, string code);
    }

    public class EmailService : IEmailService
    {
        private readonly string _templateRoot;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromAddress;
        private readonly string _appName;
        private readonly int _expiryMinutes;
        private readonly bool _enableEasterEggs;

        private static readonly Random _random = new();

        private static readonly string[] Quotes =
        {
            "验证码只有5分钟有效期，但你的努力会一直发光。—— Qwen",
            "每一次验证，都是通向新世界的钥匙。🗝️",
            "生活就像验证码，看起来乱七八糟，其实都有意义。",
            "今天也要勇敢地输入正确答案呀！💪",
            "别怕输错，大不了重来一次；人生如此，验证码亦然。",
            "世界上最短的诗，是你输入的那串数字。🔢",
            "如果累了，就停下来喝口水，我们等你回来继续验证。🥤",
            "不是所有代码都会报错，比如你现在看到的这句 ❤️"
        };

        public EmailService(IConfiguration config)
        {
            // 模板根目录：默认 ./config/templates，可配置
            _templateRoot = config["Email:Templates:Root"] 
                ?? Path.Combine(Directory.GetCurrentDirectory(), "config", "templates");

            var smtpConfig = config.GetSection("Smtp");
            _fromAddress = smtpConfig["FromAddress"] ?? throw new ArgumentException("缺少发件邮箱配置");
            _appName = config["AppName"] ?? "我们的应用";
            _expiryMinutes = int.Parse(config["Email:ExpiryMinutes"] ?? "5");
            _enableEasterEggs = bool.Parse(config["Features:EnableEasterEggs"] ?? "true");

            _smtpClient = new SmtpClient(smtpConfig["Host"])
            {
                Port = int.Parse(smtpConfig["Port"] ?? "587"),
                Credentials = new NetworkCredential(
                    smtpConfig["UserAccount"],
                    smtpConfig["Password"]),
                EnableSsl = bool.Parse(smtpConfig["EnableSsl"] ?? "true")
            };
        }

        public async Task<bool> SendVerificationCodeAsync(string toEmail, string code)
        {
            var subject = $"【{_appName}】您的登录验证码";

            // 动态内容
            var greeting = _enableEasterEggs ? GetGreeting() : "";
            var quote = _enableEasterEggs ? GetRandomQuote() : "";

            try
            {
                // 读取模板（安全路径检查）
                var htmlTemplate = await ReadTemplateAsync("email/verification.html");
                var textTemplate = await ReadTemplateAsync("email/verification.txt");

                // 替换变量
                var htmlBody = htmlTemplate
                    .Replace("{{code}}", WebUtility.HtmlEncode(code))
                    .Replace("{{appName}}", WebUtility.HtmlEncode(_appName))
                    .Replace("{{expiryMinutes}}", _expiryMinutes.ToString())
                    .Replace("{{greeting}}", WebUtility.HtmlEncode(greeting))
                    .Replace("{{quote}}", $"<em style='color:#1677ff;'>“{WebUtility.HtmlEncode(quote)}”</em>")
                    .Replace("{{currentTime}}", DateTime.Now.ToString("HH:mm"));

                var textBody = textTemplate
                    .Replace("{{code}}", code)
                    .Replace("{{appName}}", _appName)
                    .Replace("{{expiryMinutes}}", _expiryMinutes.ToString())
                    .Replace("{{greeting}}", greeting)
                    .Replace("{{quote}}", quote)
                    .Replace("{{currentTime}}", DateTime.Now.ToString("HH:mm"));

                using var message = new MailMessage();
                message.From = new MailAddress(_fromAddress, _appName);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.IsBodyHtml = true;

                // 多部分邮件
                var altViews = message.AlternateViews;
                altViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
                altViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));

                await _smtpClient.SendMailAsync(message).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                // 建议替换为 ILogger<T>
                Console.WriteLine($"[EmailService] 发送失败: {ex.Message}");
                return false;
            }
        }

        #region 私有方法

        private string GetGreeting()
        {
            var hour = DateTime.Now.Hour;
            return hour switch
            {
                >= 5 and < 9 => "🌅 早安！新的一天开始了，元气满满出发吧！",
                >= 9 and < 12 => "🌤 上午好！阳光正好，适合做点有意义的事～",
                >= 12 and < 14 => "🍜 中午好！别忘了吃口热饭，犒劳辛苦的自己。",
                >= 14 and < 18 => "🌇 下午好！咖啡续杯了吗？继续冲鸭！",
                >= 18 and < 21 => "🌃 晚上好！结束了一天忙碌，慢慢放松下来吧～",
                >= 21 and < 24 => "🌙 夜深了，世界安静下来，你也该休息啦～",
                _ => "🌌 半夜还在忙吗？记得照顾好自己，有人牵挂你哦～"
            };
        }

        private string GetRandomQuote() => Quotes[_random.Next(Quotes.Length)];

        private async Task<string> ReadTemplateAsync(string relativePath)
        {
            var filePath = Path.Combine(_templateRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var fullPath = Path.GetFullPath(filePath);
            var rootPath = Path.GetFullPath(_templateRoot);

            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                throw new SecurityException("非法路径访问，防止目录穿越攻击");

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"模板文件未找到: {filePath}");

            return await File.ReadAllTextAsync(fullPath);
        }

        #endregion
    }

    // 简单去HTML标签扩展（生产建议用 HtmlAgilityPack）
    public static class StringExtensions
    {
        public static string StripHtml(this string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input ?? "", "<.*?>", "");
        }
    }
}
```

---

## 📄 文件二：HTML 模板  
`config/templates/email/verification.html`

```html
<!DOCTYPE html>
<html lang="zh">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
  <title>验证码</title>
</head>
<body style="margin:0; padding:0; font-family: 'Microsoft YaHei', sans-serif; background-color: #f9f9f9;">
  <table width="100%" cellpadding="0" cellspacing="0" style="max-width: 600px; margin: 30px auto; border-collapse: collapse;">
    <tr>
      <td align="center" style="padding: 30px 20px; background-color: #1677ff; color: white; border-radius: 8px 8px 0 0;">
        <h1 style="margin:0; font-size: 24px;">欢迎回来！</h1>
        <p style="margin:10px 0 0; opacity: 0.9; font-size: 14px;">请完成身份验证以继续操作</p>
      </td>
    </tr>

    <tr>
      <td style="padding: 30px; background-color: white; text-align: center; border-left: 1px solid #eee; border-right: 1px solid #eee;">
        <p style="font-size: 16px; color: #333; margin: 0 0 20px;">
          您的验证码为：
        </p>

        <div style="display: inline-block; padding: 15px 25px; font-size: 28px; font-weight: bold; letter-spacing: 12px; 
                    color: #1677ff; background: #f0f6ff; border: 2px dashed #bae0ff; border-radius: 8px; margin: 20px 0;">
          {{code}}
        </div>

        <p style="color: #666; font-size: 14px; margin: 10px 0;">
          有效期 {{expiryMinutes}} 分钟，请于 {{currentTime}} 前输入。如非本人操作，请忽略本邮件。
        </p>

        {{quote}}
      </td>
    </tr>

    <tr>
      <td style="padding: 15px; text-align: center; background-color: #f0f6ff; font-size: 14px; color: #666;">
        {{greeting}}
      </td>
    </tr>

    <tr>
      <td align="center" style="padding: 20px; color: #aaa; font-size: 12px; background-color: #fafafa; border-bottom: 1px solid #eee;">
        🔐 安全提示：请勿将验证码分享给他人。<br/>
        &copy; {{currentTime.Split(' ')[0]}} {{appName}}. 保留所有权利。
      </td>
    </tr>
  </table>
</body>
</html>
```

---

## 📝 文件三：纯文本模板  
`config/templates/email/verification.txt`

```text
{{appName}} 身份验证

您好，

{{greeting}}

您的验证码是：{{code}}
有效期：{{expiryMinutes}} 分钟
当前时间：{{currentTime}}

请尽快完成验证。如果不是您发起的操作，请忽略此邮件。

💡 小彩蛋：{{quote}}

---
安全提醒：请勿向任何人透露该验证码。
{{appName}} 团队 · https://yourapp.com
```

---

## ⚙️ 文件四：`appsettings.json`

```json
{
  "AppName": "星语日记",
  "Features": {
    "EnableEasterEggs": "true"
  },
  "Email": {
    "Templates": {
      "Root": "config/templates"
    },
    "ExpiryMinutes": "5"
  },
  "Smtp": {
    "Host": "smtp.qq.com",
    "Port": "587",
    "UserAccount": "no-reply@xingyu.com",
    "Password": "your-app-password",
    "FromAddress": "no-reply@xingyu.com",
    "EnableSsl": "true"
  }
}
```

---

## 🔌 文件五：`Program.cs` 注册服务（.NET 8+）

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加配置源（确保能读到 appsettings.json）
builder.Services.AddOptions();

// 注册邮件服务
builder.Services.AddSingleton<IEmailService, EmailService>();

var app = builder.Build();

// 测试接口
app.MapGet("/send-test", async (IEmailService emailService) =>
{
    var result = await emailService.SendVerificationCodeAsync("test@example.com", "8849");
    return Results.Ok(new { success = result });
});

app.Run();
```

---

## ✅ 使用说明

1. 在项目根目录创建文件夹 `config/templates/email/`
2. 放入两个模板文件
3. 确保 `appsettings.json` 正确配置 SMTP
4. 运行即可发送带趣味内容的验证码邮件 ✉️✨

---

🎉 **搞定！**

你现在拥有了一个：
- ✅ 内容与代码分离
- ✅ 支持热更新模板
- ✅ 充满人情味
- ✅ 安全可靠的验证码邮件系统