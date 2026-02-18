/**
 * IP访问记录表，用于安全审计、流量分析和行为追踪
 */
export interface IpAccessLog {
    /**
     * 自增主键，唯一标识每条记录
     */
    Id: number;

    /**
     * 通过IP和时间戳生成的哈希值，用于匿名化标识
     */
    IpId: string | null;

    /**
     * 客户端IP地址（支持IPv4/IPv6），示例："203.0.113.45" 或 "2001:db8::1"
     */
    IpAddress: string;

    /**
     * 客户端浏览器/设备信息，示例："Mozilla/5.0 (Windows NT 10.0; Win64; x64)..."
     */
    UserAgent: string | null;

    /**
     * 请求体内容（敏感信息需脱敏），示例：{"username":"test","password":"***"}
     */
    RequestBody: any | null;

    /**
     * 请求时间（精确到毫秒），示例："2023-10-25 14:30:45.123"
     */
    RequestTime: string;

    /**
     * HTTP请求方法，示例：GET、POST、PUT、DELETE
     */
    RequestMethod: string;

    /**
     * 完整请求路径（含查询参数），示例："/api/login?token=abc123"
     */
    RequestUrl: string;

    /**
     * HTTP协议版本，示例："HTTP/1.1" 或 "HTTP/2"
     */
    HttpVersion: string | null;

    /**
     * 服务器响应状态码，示例：200（成功）、404（未找到）、500（服务器错误）
     */
    ResponseStatus: number | null;

    /**
     * 服务器处理请求耗时（毫秒），示例：125
     */
    ResponseTimeMs: number | null;

    /**
     * 来源页面URL（可选），示例："https://example.com/home"
     */
    Referer: string | null;

    /**
     * 请求头信息（JSON格式），示例：{"Accept-Language": "en-US", "Cookie": "..."}
     */
    Headers: any | null;

    /**
     * IP地理位置信息（JSON格式），示例：{"country": "CN", "city": "Beijing"}
     */
    GeoLocation: any | null;

    /**
     * 设备类型（通过User-Agent解析），示例：Mobile、Desktop、Tablet
     */
    DeviceType: string | null;

    /**
     * 操作系统名称及版本，示例：Windows 10、iOS 16.5
     */
    OsName: string | null;

    /**
     * 浏览器名称及版本，示例：Chrome 118、Firefox 119
     */
    BrowserName: string | null;

    /**
     * 是否为爬虫/机器人请求，TRUE/FALSE
     */
    IsBot: boolean | null;

    /**
     * 威胁等级（0-5），0=正常，3=可疑，5=攻击行为
     */
    ThreatLevel: number | null;

    /**
     * 用户会话ID（如有），示例："sess_abc123xyz"
     */
    SessionId: string | null;

    /**
     * 关联用户ID（如已登录），示例："usr_456"
     */
    UserId: string | null;

    /**
     * 备注信息（如攻击类型），示例："SQL Injection Attempt"
     */
    ExtraNotes: string | null;

}
export interface IpInfo {
    AS: {
        Info: string;
        Name: string;
        Number: number;
    };
    Addr: string;
    Country:
    {
        Code: string;
        Name: string;
    };
    IP: string;
    Regions: string[];

    RegionsShort: string[];

    RegisteredCountry: {
        Code: string;
        Name: string;
    };
    Type: string;
}