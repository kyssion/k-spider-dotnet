namespace KSpider.Spider.Verify.Model;

/// <summary>
///     一次识别结果 : 命中了哪种验证、由谁命中、证据是什么。
///     Evidence 进日志 , 排障时据此判断是源站改版还是风控策略变化。
/// </summary>
public sealed class VerificationChallenge
{
    public required VerificationKind Kind { get; init; }

    /// <summary>命中的识别器名 , 用于日志定位</summary>
    public required string DetectorName { get; init; }

    /// <summary>判定依据 ( 命中的特征片段 ) , 只作日志与排障 , 不参与逻辑</summary>
    public string Evidence { get; init; } = "";

    public int StatusCode { get; init; }

    /// <summary>3xx 跳转目标</summary>
    public string? Location { get; init; }

    /// <summary>429 / 503 响应头里的 Retry-After ( 秒 ) , 0 表示没给</summary>
    public int RetryAfterSeconds { get; init; }
}
