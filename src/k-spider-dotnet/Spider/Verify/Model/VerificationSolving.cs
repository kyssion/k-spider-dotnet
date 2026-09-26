namespace KSpider.Spider.Verify.Model;

/// <summary>
///     通过策略的输入
/// </summary>
public sealed class VerificationSolveRequest
{
    /// <summary>识别出的验证类型与证据</summary>
    public required VerificationChallenge Challenge { get; init; }

    /// <summary>原始响应信息 ( 策略可能要再看 BodySample / Url )</summary>
    public required VerificationProbe Probe { get; init; }

    /// <summary>该源当前生效的策略 ( 超时 / 浏览器等待秒数等由它决定 )</summary>
    public required VerificationPolicy Policy { get; init; }

    /// <summary>目标主机 ( Probe 的快捷转发 )</summary>
    public string Host => Probe.Host;

    /// <summary>目标地址 ( Probe 的快捷转发 )</summary>
    public string Url => Probe.Url;
}

/// <summary>
///     通过策略的输出 : 成功时带回可用于重放请求的会话
/// </summary>
public sealed class VerificationSolveResult
{
    /// <summary>哪个策略给出的结果</summary>
    public required string SolverName { get; init; }

    /// <summary>是否取得了可用会话</summary>
    public bool Success { get; init; }

    /// <summary>成功时带回的会话 ( 供管线落缓存与重放请求 )</summary>
    public VerificationSession? Session { get; init; }

    /// <summary>结果说明 , 进日志</summary>
    public string Message { get; init; } = "";

    /// <summary>是否需要人工介入 ( 自动手段都用尽 ) , 由 NewsCheckJob 汇总告警</summary>
    public bool NeedsManualAttention { get; init; }

    /// <summary>构造成功结果</summary>
    public static VerificationSolveResult Solved(string solverName, VerificationSession session, string message)
    {
        return new VerificationSolveResult { SolverName = solverName, Success = true, Session = session, Message = message };
    }

    /// <summary>构造失败结果</summary>
    public static VerificationSolveResult Failed(string solverName, string message, bool needsManualAttention = false)
    {
        return new VerificationSolveResult
        {
            SolverName = solverName,
            Success = false,
            Message = message,
            NeedsManualAttention = needsManualAttention
        };
    }
}
