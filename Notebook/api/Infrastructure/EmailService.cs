using System.Net;
using System.Net.Mail;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace api.Infrastructure;

/// <summary>
/// 邮件发送服务接口。
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 发送邮箱验证码邮件。
    /// </summary>
    /// <param name="toEmail">接收验证码的目标邮箱地址。</param>
    /// <param name="code">要发送的验证码内容。</param>
    /// <param name="actionType">验证码用途提示。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    Task SendVerificationCodeAsync(string toEmail, string code, string? actionType, CancellationToken cancellationToken = default);
}

/// <summary>
/// 基于 SMTP 的邮件发送服务实现。
/// </summary>
public class EmailService : IEmailService
{
    private class GreetingRule
    {
        public int StartHour { get; set; }
        public int EndHour { get; set; }
        public string Text { get; set; } = string.Empty;

        public bool IsMatch(int hour) => hour >= StartHour && hour < EndHour;
    }

    private class SmtpConfig
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserAccount { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string? FromName { get; set; }

        public bool IsValid() => !string.IsNullOrEmpty(Host) && !string.IsNullOrEmpty(UserAccount) && !string.IsNullOrEmpty(Password);
    }

    readonly SmtpConfig _primaryConfig;
    readonly SmtpConfig? _backupConfig;
    readonly string _appName;
    readonly int _expiryMinutes;
    readonly string[] _quotes;
    readonly List<GreetingRule> _greetingRules;

    static readonly string[] DefaultQuotes =
    [
        "验证码只有5分钟有效期，但你的努力会一直发光。",
        "每一次验证，都是通向新世界的钥匙。🗝️",
        "生活就像验证码，看起来乱七八糟，其实都有意义。",
        "今天也要勇敢地输入正确答案呀！💪",
        "别怕输错，大不了重来一次；人生如此，验证码亦然。",
        "世界上最短的诗，是你输入的那串数字。🔢",
        "如果累了，就停下来喝口水，我们等你回来继续验证。🥤",
        "不是所有代码都会报错，比如你现在看到的这句 ❤️"
    ];

    static readonly GreetingRule[] DefaultGreetingRules =
    [
        new GreetingRule { StartHour = 5, EndHour = 9, Text = "🌅 早安！新的一天开始了，元气满满出发吧！" },
        new GreetingRule { StartHour = 9, EndHour = 12, Text = "🌤 上午好！阳光正好，适合做点有意义的事～" },
        new GreetingRule { StartHour = 12, EndHour = 14, Text = "🍜 中午好！别忘了吃口热饭，犒劳辛苦的自己。" },
        new GreetingRule { StartHour = 14, EndHour = 18, Text = "🌇 下午好！咖啡续杯了吗？继续冲鸭！" },
        new GreetingRule { StartHour = 18, EndHour = 21, Text = "🌃 晚上好！结束了一天忙碌，慢慢放松下来吧～" },
        new GreetingRule { StartHour = 21, EndHour = 24, Text = "🌙 夜深了，世界安静下来，你也该休息啦～" },
        new GreetingRule { StartHour = 0, EndHour = 5, Text = "🌌 半夜还在忙吗？记得照顾好自己，有人牵挂你哦～" }
    ];

    /// <summary>
    /// 使用应用配置初始化邮件发送服务。
    /// </summary>
    /// <param name="configuration">应用配置对象。</param>
    public EmailService(IConfiguration configuration)
    {
        _primaryConfig = LoadSmtpConfig(configuration.GetSection("Smtp"));
        
        var backupSection = configuration.GetSection("SmtpBackup");
        if (backupSection.Exists())
        {
            try 
            {
                _backupConfig = LoadSmtpConfig(backupSection);
            }
            catch
            {
                // Ignore backup config errors, just don't use it
                _backupConfig = null;
            }
        }

        var emailSection = configuration.GetSection("Email");
        _appName = emailSection["AppName"] ?? configuration["AppName"] ?? "DailyCheck";
        _expiryMinutes = emailSection.GetValue<int?>("ExpiryMinutes") ?? 5;

        var quotes = emailSection.GetSection("Quotes").Get<string[]>();
        _quotes = quotes != null && quotes.Length > 0
            ? quotes.Where(q => !string.IsNullOrWhiteSpace(q)).ToArray()
            : DefaultQuotes;

        var rules = emailSection.GetSection("Greetings").Get<GreetingRule[]>();
        _greetingRules = (rules ?? Array.Empty<GreetingRule>())
            .Where(r => !string.IsNullOrWhiteSpace(r.Text))
            .Where(r => r.StartHour >= 0 && r.StartHour < 24 && r.EndHour > 0 && r.EndHour <= 24 && r.StartHour != r.EndHour)
            .ToList();

        if (_greetingRules.Count == 0)
            _greetingRules = DefaultGreetingRules.ToList();
    }

    private SmtpConfig LoadSmtpConfig(IConfigurationSection section)
    {
        var config = new SmtpConfig
        {
            Host = section["Host"] ?? throw new InvalidOperationException($"Smtp config Host is required in section {section.Key}"),
            Port = section.GetValue<int?>("Port") ?? 465,
            UserAccount = section["UserAccount"] ?? throw new InvalidOperationException($"Smtp config UserAccount is required in section {section.Key}"),
            Password = section["Password"] ?? throw new InvalidOperationException($"Smtp config Password is required in section {section.Key}"),
            EnableSsl = section.GetValue<bool?>("EnableSsl") ?? true
        };
        config.FromAddress = section["FromAddress"] ?? config.UserAccount;
        config.FromName = section["FromName"];
        
        return config;
    }

    /// <summary>
    /// 发送邮箱验证码邮件。
    /// </summary>
    /// <param name="toEmail">接收验证码的目标邮箱地址。</param>
    /// <param name="code">要发送的验证码内容。</param>
    /// <param name="cancellationToken">取消操作标记。</param>
    public async Task SendVerificationCodeAsync(string toEmail, string code, string? actionType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Email is required", nameof(toEmail));

        var normalizedEmail = toEmail.Trim();
        var subject = $"【{_appName}】您的安全验证码";
        var action = string.IsNullOrWhiteSpace(actionType) ? "验证" : actionType.Trim();
        var greeting = GetGreeting(DateTime.Now.Hour);
        var inspiration = GetRandomQuote();

        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "EmailTemplates", "verification_code.html");
        string templateContent;
        
        if (System.IO.File.Exists(templatePath))
        {
            templateContent = await System.IO.File.ReadAllTextAsync(templatePath, cancellationToken);
        }
        else
        {
            // Fallback to simple HTML if template file is missing
            templateContent = "<html><body><h1>{{AppName}}</h1><p>Verification Code: {{Code}}</p></body></html>";
        }

        var body = templateContent
            .Replace("{{AppName}}", _appName)
            .Replace("{{Code}}", code)
            .Replace("{{ExpiryMinutes}}", _expiryMinutes.ToString())
            .Replace("{{Year}}", DateTime.Now.Year.ToString())
            .Replace("{{ActionType}}", action)
            .Replace("{{UserEmail}}", normalizedEmail)
            .Replace("{{Greeting}}", greeting)
            .Replace("{{Inspiration}}", inspiration);

        using var registration = cancellationToken.Register(() => { }); 
        
        // Try primary
        try
        {
            await SendEmailInternalAsync(_primaryConfig, normalizedEmail, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            // If backup exists, try backup
            if (_backupConfig != null && _backupConfig.IsValid())
            {
                try
                {
                    await SendEmailInternalAsync(_backupConfig, normalizedEmail, subject, body, cancellationToken);
                }
                catch (Exception backupEx)
                {
                    throw new AggregateException("Failed to send email with both primary and backup configurations.", ex, backupEx);
                }
            }
            else
            {
                throw; // Rethrow primary exception if no backup
            }
        }
    }

    private async Task SendEmailInternalAsync(SmtpConfig config, string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(config.Host)
        {
            Port = config.Port,
            EnableSsl = config.EnableSsl,
            Credentials = new NetworkCredential(config.UserAccount, config.Password)
        };

        using var message = new MailMessage
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);
        message.From = string.IsNullOrWhiteSpace(config.FromName)
            ? new MailAddress(config.FromAddress)
            : new MailAddress(config.FromAddress, config.FromName);

        await client.SendMailAsync(message, cancellationToken);
    }

    string GetRandomQuote()
    {
        var quotes = _quotes.Length > 0 ? _quotes : DefaultQuotes;
        var index = Random.Shared.Next(quotes.Length);
        return quotes[index];
    }

    string GetGreeting(int hour)
    {
        var rule = _greetingRules.FirstOrDefault(r => r.IsMatch(hour));
        if (rule != null)
            return rule.Text;

        return DefaultGreetingRules.First(r => r.IsMatch(hour)).Text;
    }
}
