namespace KSpider.Spider.Verify.Model;

/// <summary>
///     验证 ( 反爬 ) 类型 : 识别器与通过策略共用的统一词汇表。
///     两侧只依赖这个枚举 , 互不引用具体实现 —— 新增一种验证方式时在这里加值 ,
///     由识别器返回它、由通过策略声明自己能处理它即可。
///     Model/ 目录收纳被识别器、通过策略、管线与门面共同引用的纯数据词汇 , 一概念一文件 ;
///     有行为的组件 ( 编排 / 状态 / HTTP ) 不进这里。
/// </summary>
public enum VerificationKind
{
    /// <summary>拒绝访问 ( 403 / 401 ) , 常见于 WAF 直接拦截或需要带 cookie</summary>
    AccessDenied = 1,

    /// <summary>频率限制 ( 429 ) , 只能退避 , 不存在"通过"一说</summary>
    RateLimited = 2,

    /// <summary>网关拦截 ( 503 且带 Retry-After ) , 多为 WAF 限流而非服务真的挂了</summary>
    ServerGate = 3,

    /// <summary>3xx 跳转到验证 / 风控页</summary>
    VerifyRedirect = 4,

    /// <summary>Cloudflare 类 JS 挑战 ( cf-mitigated / challenge-platform / Turnstile )</summary>
    CloudflareChallenge = 10,

    /// <summary>JS 计算 cookie 型门禁 ( 加速乐 __jsl_clearance / 阿里云盾 acw_sc__v2 / 瑞数 ) , 浏览器执行脚本后自动通过</summary>
    JsCookieGate = 11,

    /// <summary>通用"正在验证浏览器 / 请开启 JavaScript"页</summary>
    BrowserCheck = 12,

    /// <summary>滑块验证码</summary>
    SliderCaptcha = 20,

    /// <summary>图形验证码 ( 需识图 , 本模块只识别与升级 )</summary>
    ImageCaptcha = 21,

    /// <summary>短信 / 手机验证码</summary>
    SmsCaptcha = 22,

    /// <summary>HTTP 200 但载荷里报风控 / 鉴权失败 ( 静默失效 )</summary>
    RiskControl = 30
}

/// <summary>
///     验证类型的共性判断 , 供管线与通过策略共用。
///     放在枚举旁边 : 新增枚举值时就该顺手想清楚它属不属于这两类。
/// </summary>
public static class VerificationKindTraits
{
    /// <summary>
    ///     只能靠"等"恢复的类型 ( 限流 / 载荷级风控 ) : 没有任何自动手段 ,
    ///     也不该升级成"需要人工介入" —— 退避一段时间自己就好了。
    /// </summary>
    public static bool ResolvesByWaiting(VerificationKind kind)
    {
        return kind is VerificationKind.RateLimited or VerificationKind.RiskControl;
    }

    /// <summary>
    ///     与 cookie 有关的类型 : 被这类拦住说明手上的会话已不被接受 , 该丢掉 ,
    ///     否则冷却期内每个请求都继续拿着死 cookie 去撞。
    ///     限流 / 风控与 cookie 无关 , 丢掉有效会话反而让下一个请求白多过一次验证。
    /// </summary>
    public static bool IsCookieRelated(VerificationKind kind)
    {
        return kind is VerificationKind.AccessDenied or VerificationKind.VerifyRedirect
            or VerificationKind.CloudflareChallenge or VerificationKind.JsCookieGate
            or VerificationKind.BrowserCheck;
    }
}
