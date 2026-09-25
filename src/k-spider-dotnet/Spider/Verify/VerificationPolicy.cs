namespace KSpider.Spider.Verify;

/// <summary>
///     过验证策略 : 按主机 ( 源 ) 控制"允不允许自动过验证、允许用哪些手段、失败后歇多久"。
///     默认策略对全部源生效 , 需要单独调整的源在 <see cref="VerificationRegistry" /> 里加一行覆盖。
/// </summary>
public sealed class VerificationPolicy
{
    /// <summary>
    ///     默认放行的验证类型 : 只含"浏览器跑一遍就能自动完成"的 JS / Cloudflare 类挑战。
    ///     滑块 / 图形 / 短信验证码默认**不**自动处理 —— 它们要求的是人机确认 ,
    ///     由部署方在确认自己有权抓取该站后按源显式放开 ( 见 <see cref="WithKinds" /> ) ,
    ///     默认状态下这类挑战会被识别出来并升级告警 , 而不是悄悄返回空数据。
    ///     限流与载荷级风控也不在列 : 它们没有任何自动手段 , 唯一合理的响应是退避重试
    ///     ( 见 VerificationKindTraits.ResolvesByWaiting ) , 放进来只会让"放行自动处理"这句话失真。
    /// </summary>
    public static readonly IReadOnlyCollection<VerificationKind> DefaultAllowedKinds =
    [
        VerificationKind.AccessDenied,
        VerificationKind.ServerGate,
        VerificationKind.VerifyRedirect,
        VerificationKind.CloudflareChallenge,
        VerificationKind.JsCookieGate,
        VerificationKind.BrowserCheck
    ];

    /// <summary>默认策略 : 开启自动过验证 , 放行 <see cref="DefaultAllowedKinds" /></summary>
    public static VerificationPolicy Default { get; } = new();

    /// <summary>关闭自动过验证 : 只识别、只告警 , 不启动任何处理手段</summary>
    public static VerificationPolicy Disabled { get; } = new() { Enabled = false };

    /// <summary>
    ///     在默认放行集之上**追加**验证类型 ( 例如把滑块纳入自动处理 )。
    ///     是追加而不是替换 : 只想收窄时直接构造 <c>new VerificationPolicy { AllowedKinds = [...] }</c> ,
    ///     用本方法却把默认项挤掉 , 会让该源的 Cloudflare / JS 门禁自动通过静默失效。
    /// </summary>
    public static VerificationPolicy WithKinds(params VerificationKind[] kinds)
    {
        return new VerificationPolicy { AllowedKinds = [.. DefaultAllowedKinds, .. kinds] };
    }

    public bool Enabled { get; init; } = true;

    public IReadOnlyCollection<VerificationKind> AllowedKinds { get; init; } = DefaultAllowedKinds;

    /// <summary>
    ///     单次请求最多发几次 ( 含过验证后的重放 ) , 防止无限重试。
    ///     默认 2 即"过一次 + 重放一次" ; 重放后仍被拦说明会话没被服务端接受 , 再解一遍大概率同样失败 ,
    ///     所以不继续加次数 , 交给冷却期与下一轮任务。
    /// </summary>
    public int MaxSolveAttempts { get; init; } = 2;

    /// <summary>单个通过策略的执行上限 , 超时按失败处理 ( 避免浏览器卡死拖住整个任务 )</summary>
    public int SolveTimeoutSeconds { get; init; } = 60;

    /// <summary>同一主机处理失败后的冷却时间 : 冷却期内不再尝试 , 避免对目标站持续施压</summary>
    public int CooldownSeconds { get; init; } = 600;

    /// <summary>已通过验证的会话本地缓存时长上限 ( cookie 真实有效期由服务端决定 )</summary>
    public int SessionTtlSeconds { get; init; } = 1800;

    /// <summary>浏览器打开挑战页后等待挑战自动清除的秒数</summary>
    public int BrowserWaitSeconds { get; init; } = 15;

    public bool Allows(VerificationKind kind)
    {
        return Enabled && AllowedKinds.Contains(kind);
    }
}
