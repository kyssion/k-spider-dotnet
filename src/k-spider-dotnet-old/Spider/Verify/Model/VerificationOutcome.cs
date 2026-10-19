namespace KSpider.Spider.Verify.Model;

/// <summary>
///     一次"识别 + 通过"的最终结论
/// </summary>
public sealed class VerificationOutcome
{
    /// <summary>是否识别到验证</summary>
    public bool Challenged { get; init; }

    /// <summary>识别结论 ( Challenged 时非空 )</summary>
    public VerificationChallenge? Challenge { get; init; }

    /// <summary>是否通过 ( 仅 Challenged 时有意义 )</summary>
    public bool Solved { get; init; }

    /// <summary>通过时取得的会话</summary>
    public VerificationSession? Session { get; init; }

    /// <summary>结论说明 , 进日志或异常消息</summary>
    public string Message { get; init; } = "";

    /// <summary>是否需要人工介入 ( 自动手段全部用尽 ) , 供 NewsCheckJob 汇总告警</summary>
    public bool NeedsManualAttention { get; init; }

    /// <summary>响应正常 , 没有识别到任何验证</summary>
    public static VerificationOutcome NotChallenged { get; } = new() { Challenged = false };

    /// <summary>构造"通过"结论</summary>
    public static VerificationOutcome Passed(VerificationChallenge challenge, VerificationSession session, string message)
    {
        return new VerificationOutcome
        {
            Challenged = true, Challenge = challenge, Solved = true, Session = session, Message = message
        };
    }

    /// <summary>构造"被挡"结论</summary>
    public static VerificationOutcome Blocked(VerificationChallenge challenge, string message,
        bool needsManualAttention)
    {
        return new VerificationOutcome
        {
            Challenged = true,
            Challenge = challenge,
            Solved = false,
            Message = message,
            NeedsManualAttention = needsManualAttention
        };
    }
}

/// <summary>
///     当前被验证挡住的源 ( 处于冷却期 ) , 供健康检查汇总告警
/// </summary>
public sealed class VerificationBlock
{
    /// <summary>被挡的主机</summary>
    public required string Host { get; init; }

    /// <summary>拦截类型</summary>
    public required VerificationKind Kind { get; init; }

    /// <summary>冷却截止时刻</summary>
    public required DateTime Until { get; init; }

    /// <summary>进入冷却的原因 ( 上次尝试失败的结论 )</summary>
    public required string Reason { get; init; }
}
