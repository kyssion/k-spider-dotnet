using KSpider.Common.Logger;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace KSpider.Spider.Verify.Solver;

/// <summary>
///     浏览器过挑战策略 : 打开被拦截的地址 , 让页面脚本自己把挑战走完 ( JS 挑战 / Cloudflare 挑战
///     都是脚本自动完成的 , 不需要点任何东西 ) , 然后导出放行 cookie 交给 HTTP 链路复用。
///     这是本模块的主力手段 : 代价是一次浏览器启动 ( 秒级 ) , 换来后续请求继续走纯 HTTP。
/// </summary>
public sealed class BrowserChallengeSolver(IReadOnlyList<IVerificationDetector> detectors) : IVerificationSolver
{
    private static readonly ILogger Log = LogFactory.GetLogger<BrowserChallengeSolver>();

    public string Name => "BrowserChallengeSolver";

    public int Cost => 10;

    public IReadOnlyCollection<VerificationKind> Kinds =>
    [
        VerificationKind.CloudflareChallenge,
        VerificationKind.JsCookieGate,
        VerificationKind.BrowserCheck,
        VerificationKind.VerifyRedirect,
        VerificationKind.AccessDenied,
        VerificationKind.ServerGate
    ];

    public async Task<VerificationSolveResult> SolveAsync(VerificationSolveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var gate = await BrowserGate.OpenAsync(request.Url, cancellationToken);
            // 挑战由页面脚本自己完成 , 这里只需等它把放行 cookie 种下 :
            // 每秒取一次当前 DOM 重跑识别器 , 识别不到挑战即视为已放行
            for (var waitedSeconds = 1; waitedSeconds <= request.Policy.BrowserWaitSeconds; waitedSeconds++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                if (await StillChallengedAsync(gate, request)) continue;
                return await SolvedAsync(gate, request, waitedSeconds);
            }

            return VerificationSolveResult.Failed(Name,
                $"浏览器等待 {request.Policy.BrowserWaitSeconds} 秒后挑战仍未解除");
        }
        catch (OperationCanceledException)
        {
            // 超时由管线统一按失败处理 ( 这里不吞 , 让管线记一条统一的超时日志 )
            throw;
        }
        catch (PlaywrightException e)
        {
            return VerificationSolveResult.Failed(Name,
                $"浏览器启动 / 加载失败 : {e.Message} ( 首次使用需执行 playwright install chromium )");
        }
        catch (Exception e)
        {
            return VerificationSolveResult.Failed(Name, $"浏览器过挑战异常 : {e.Message}");
        }
    }

    /// <summary>页面当前 DOM 是否仍被识别为挑战页</summary>
    private async Task<bool> StillChallengedAsync(BrowserGate gate, VerificationSolveRequest request)
    {
        var content = await gate.ContentAsync();
        var probe = BrowserGate.BuildProbe(request.Url, content);
        return detectors.Any(detector => detector.Detect(probe) != null);
    }

    private async Task<VerificationSolveResult> SolvedAsync(BrowserGate gate, VerificationSolveRequest request,
        int waitedSeconds)
    {
        var cookies = await gate.ExportCookiesAsync(request.Host);
        if (cookies.Count == 0)
            return VerificationSolveResult.Failed(Name, "浏览器已放行但没拿到任何 cookie , 无法交给 HTTP 链路复用");

        var session = VerificationSession.Create(request.Host, cookies, request.Policy.SessionTtlSeconds);
        Log.LogInformation(
            "[BrowserChallengeSolver] 通过 {Kind} 拦截 , host : {Host} , 等待 {Waited}s , 导出 cookie {Count} 个",
            request.Challenge.Kind, request.Host, waitedSeconds, cookies.Count);
        return VerificationSolveResult.Solved(Name, session,
            $"浏览器通过 {request.Challenge.Kind} ( 等待 {waitedSeconds}s ) , 导出 cookie {cookies.Count} 个");
    }
}
