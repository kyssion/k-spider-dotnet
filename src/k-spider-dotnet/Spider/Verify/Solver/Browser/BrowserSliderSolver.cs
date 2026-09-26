using KSpider.Common.Logger;
using KSpider.Spider.Verify.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace KSpider.Spider.Verify.Solver.Browser;

/// <summary>
///     滑块验证码策略 : 用浏览器打开验证页 , 先等挑战脚本自动放行 , 等不到再把滑块从起点拖到轨道右端 ,
///     通过后导出 cookie。默认策略**不**放行本策略 ( 见 <see cref="VerificationPolicy.DefaultAllowedKinds" /> ) ——
///     滑块要求的是人机确认 , 只在确认自己有权抓取该站时按源显式放开。
///     两段式的原因 : 阿里云 WAF 类挑战页 ( 如财联社 /detail/* 的拦截页 ) 对干净指纹的浏览器会由脚本自动放行 ,
///     滑块 UI 只是给可疑客户端的降级形态 —— 不先等就会"没找到滑块"而误报失败。
///     注意 : 拖拽轨迹与 DOM 都按常见厂商实现 , 各站差异大且可能叠加行为风控 , 不保证一次通过 ;
///     失败会落到 ManualEscalationSolver 告警 , 不会静默丢数据。
/// </summary>
public sealed class BrowserSliderSolver(IReadOnlyList<IVerificationDetector> detectors) : IVerificationSolver
{
    private static readonly ILogger Log = LogFactory.GetLogger<BrowserSliderSolver>();

    /// <summary>
    ///     各厂商滑块 DOM 差异大 : 按 ( 滑块, 轨道 ) 选择器依次探测 , 命中即用。
    ///     轨道留空表示用滑块父节点当轨道 ; 新增厂商在这里加一组即可。
    ///     aliyunCaptcha-sliding-slider 是新版阿里云 WAF 挑战页 ( AliyunCaptcha.js 动态创建 ,
    ///     轨道由 SDK 内部生成 , 拿不到稳定 id , 用父节点兜底 )。
    /// </summary>
    private static readonly (string Handle, string? Track)[] SliderSelectors =
    [
        ("#nc_1_n1z", ".nc_1_n1z_parent"),
        ("#nc_1_n1z", ".nc_scale"),
        ("#aliyunCaptcha-sliding-slider", null),
        (".geetest_slider_button", ".geetest_slider"),
        (".btn_slide", ".slide-verify"),
        ("[class*=slider] [class*=handle]", null),
        ("[class*=slider] [class*=btn]", null)
    ];

    /// <summary>拖完后等待校验结果的秒数</summary>
    private const int VerifyWaitSeconds = 3;

    /// <summary>策略名 , 进日志</summary>
    public string Name => "BrowserSliderSolver";

    /// <summary>代价 20 : 比自动放行贵在要真的碰滑块 , 且各站 DOM 不同不保证一次通过</summary>
    public int Cost => 20;

    /// <summary>只处理滑块 ; 默认策略不放行 , 确认有权抓取后按源显式放开 ( 见 VerificationPolicy.WithKinds )</summary>
    public IReadOnlyCollection<VerificationKind> Kinds => [VerificationKind.SliderCaptcha];

    /// <summary>
    ///     两段式 : 先每秒等挑战脚本自动放行 ( 干净指纹常被直接放行 ) ,
    ///     期间出现认识的滑块则拖一次 ; 拖完等 VerifyWaitSeconds 再判 , 超时按失败返回。
    /// </summary>
    public async Task<VerificationSolveResult> SolveAsync(VerificationSolveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var gate = await BrowserGate.OpenAsync(request.Url, cancellationToken);

            // 每秒重跑一遍识别器 : 自动放行的瞬间即完成 , 不必碰滑块 ;
            // 页面上出现认识的滑块则拖一次 ( 只拖一次 , 反复拖会被行为风控盯上 )
            string? dragged = null;
            for (var waitedSeconds = 1; waitedSeconds <= request.Policy.BrowserWaitSeconds; waitedSeconds++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                if (!await StillChallengedAsync(gate, request))
                    return await SolvedAsync(gate, request, dragged == null
                        ? $"挑战脚本自动放行 ( 等待 {waitedSeconds}s )"
                        : $"滑块通过 ( {dragged} , 等待 {waitedSeconds}s )");

                if (dragged == null)
                {
                    dragged = await TryDragAsync(gate, cancellationToken);
                    if (dragged != null)
                        await Task.Delay(TimeSpan.FromSeconds(VerifyWaitSeconds), cancellationToken);
                }
            }

            return dragged == null
                ? VerificationSolveResult.Failed(Name,
                    "挑战未自动放行 , 页面上也没找到认识的滑块元素 ( 该厂商 DOM 需在 SliderSelectors 补充 )")
                : VerificationSolveResult.Failed(Name, $"滑块已拖动 ( {dragged} ) 但页面仍被判为挑战页");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (PlaywrightException e)
        {
            return VerificationSolveResult.Failed(Name,
                $"浏览器启动 / 加载失败 : {e.Message} ( 首次使用需执行 playwright install chromium )");
        }
        catch (Exception e)
        {
            return VerificationSolveResult.Failed(Name, $"滑块处理异常 : {e.Message}");
        }
    }

    /// <summary>页面当前 DOM 是否仍被识别为挑战页 ; 导航瞬间取 DOM 可能抛上下文销毁 , 按仍在挑战处理 , 下一轮再查</summary>
    private async Task<bool> StillChallengedAsync(BrowserGate gate, VerificationSolveRequest request)
    {
        string content;
        try
        {
            content = await gate.ContentAsync();
        }
        catch (PlaywrightException)
        {
            return true;
        }

        var probe = BrowserGate.BuildProbe(request.Url, content);
        return detectors.Any(detector => detector.Detect(probe) != null);
    }

    /// <summary>导出目标主机 cookie 组装会话 ; 一个都拿不到按失败处理</summary>
    private async Task<VerificationSolveResult> SolvedAsync(BrowserGate gate, VerificationSolveRequest request,
        string message)
    {
        var cookies = await gate.ExportCookiesAsync(request.Host);
        if (cookies.Count == 0) return VerificationSolveResult.Failed(Name, "已放行但没拿到 cookie");

        var session = VerificationSession.Create(request.Host, cookies, request.Policy.SessionTtlSeconds);
        Log.LogInformation("[BrowserSliderSolver] 通过滑块验证 , host : {Host} , {Message} , cookie {Count} 个",
            request.Host, message, cookies.Count);
        return VerificationSolveResult.Solved(Name, session, message);
    }

    /// <summary>按选择器表逐个探测滑块 , 命中即拖动 ; 返回拖动说明 , 没找到返回 null</summary>
    private static async Task<string?> TryDragAsync(BrowserGate gate, CancellationToken cancellationToken)
    {
        foreach (var (handleSelector, trackSelector) in SliderSelectors)
        {
            var handle = gate.Page.Locator(handleSelector).First;
            var handleBox = await SafeBoxAsync(handle);
            if (handleBox == null) continue;

            var track = trackSelector == null
                ? handle.Locator("xpath=..")
                : gate.Page.Locator(trackSelector).First;
            var trackBox = await SafeBoxAsync(track);
            if (trackBox == null) continue;

            // 留 2px 余量 : 顶到最右端常被校验判为异常轨迹
            var distance = trackBox.Width - handleBox.Width - 2;
            if (distance <= 0) continue;

            var startX = handleBox.X + handleBox.Width / 2;
            var startY = handleBox.Y + handleBox.Height / 2;
            await DragAsync(gate.Page, startX, startY, distance, cancellationToken);
            return $"{handleSelector} 拖动 {distance:F0}px";
        }

        return null;
    }

    /// <summary>取元素外框 ; 选择器在当前页面不适用 ( 未命中 / 命中多个 / 不可见 ) 时返回 null</summary>
    private static async Task<LocatorBoundingBoxResult?> SafeBoxAsync(ILocator locator)
    {
        try
        {
            if (await locator.CountAsync() == 0) return null;
            return await locator.BoundingBoxAsync();
        }
        catch (PlaywrightException)
        {
            return null;
        }
    }

    /// <summary>
    ///     人类化拖拽 : 先快后慢的速度曲线 + 纵向抖动 + 松手前微调。
    ///     匀速直线到终点是最容易被风控识别的轨迹特征 ( 多数滑块会校验速度曲线与抖动 )
    /// </summary>
    private static async Task DragAsync(IPage page, float startX, float startY, double distance,
        CancellationToken cancellationToken)
    {
        await page.Mouse.MoveAsync(startX, startY);
        await page.Mouse.DownAsync();

        const int steps = 24;
        for (var step = 1; step <= steps; step++)
        {
            var progress = (double)step / steps;
            var eased = 1 - Math.Pow(1 - progress, 3);
            var x = startX + distance * eased + Random.Shared.Next(-2, 3);
            var y = startY + Random.Shared.Next(-2, 3);
            await page.Mouse.MoveAsync((float)x, (float)y);
            await Task.Delay(Random.Shared.Next(8, 30), cancellationToken);
        }

        // 过冲一点再回位 , 模拟松手前的微调
        await page.Mouse.MoveAsync((float)(startX + distance + 3), startY);
        await Task.Delay(80, cancellationToken);
        await page.Mouse.MoveAsync((float)(startX + distance), startY);
        await page.Mouse.UpAsync();
    }
}
