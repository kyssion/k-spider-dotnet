using KSpider.Spider.Verify;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Spider;

/// <summary>
///     验证处理管线回归 ( 全部离线 ) : 用假识别器与假策略构造独立管线实例 ,
///     覆盖"通过后落会话 / 全部失败进冷却 / 策略禁止 / 超时降级 / 并发复用"几条关键路径。
/// </summary>
[TestClass]
public class VerificationPipelineTest
{
    private const string ChallengeMarker = "CHALLENGE";
    private const string Host = "www.example.com";
    private const string Url = "https://www.example.com/news/list";

    private static readonly IReadOnlyDictionary<string, string> HtmlHeaders =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Content-Type"] = "text/html" };

    private static VerificationProbe Probe(bool challenged, int retryAfterSeconds = 0)
    {
        var headers = new Dictionary<string, string>(HtmlHeaders, StringComparer.OrdinalIgnoreCase);
        if (retryAfterSeconds > 0) headers["Retry-After"] = retryAfterSeconds.ToString();
        var body = challenged ? $"<html>{ChallengeMarker}</html>" : "<html>normal page</html>";
        return new VerificationProbe(Url, challenged ? 403 : 200, body, headers);
    }

    private static (VerificationPipeline Pipeline, VerificationSessionStore Store) BuildPipeline(
        VerificationKind kind, VerificationPolicy? policy = null, params IVerificationSolver[] solvers)
    {
        var store = new VerificationSessionStore();
        var effectivePolicy = policy ?? VerificationPolicy.Default;
        var pipeline = new VerificationPipeline([new FakeDetector(kind)], solvers, store, _ => effectivePolicy);
        return (pipeline, store);
    }

    [TestMethod]
    public async Task NormalResponseIsNotChallenged()
    {
        var callLog = new List<string>();
        var solver = new FakeSolver(callLog, 10, true);
        var (pipeline, store) = BuildPipeline(VerificationKind.JsCookieGate, null, solver);

        var outcome = await pipeline.HandleAsync(Probe(false));

        Assert.IsFalse(outcome.Challenged);
        Assert.AreEqual(0, solver.CallCount);
        Assert.IsNull(store.Get(Host));
    }

    [TestMethod]
    public async Task SolvedChallengeStoresSessionForReplay()
    {
        var solver = new FakeSolver(new List<string>(), 10, true);
        var (pipeline, store) = BuildPipeline(VerificationKind.JsCookieGate, null, solver);

        var outcome = await pipeline.HandleAsync(Probe(true));

        Assert.IsTrue(outcome.Solved);
        Assert.AreEqual(VerificationKind.JsCookieGate, outcome.Challenge?.Kind);
        var session = store.Get(Host);
        Assert.IsNotNull(session);
        Assert.AreEqual("sid=abc", session.CookieHeader);
        Assert.AreEqual(0, pipeline.BlockedHosts().Count);
    }

    [TestMethod]
    public async Task SolversRunByCostAscendingAndStopAtFirstSuccess()
    {
        var callLog = new List<string>();
        var expensive = new FakeSolver(callLog, 50, true);
        var cheap = new FakeSolver(callLog, 10, false);
        // 注册顺序故意与代价相反 : 管线应按 Cost 升序试 , 而不是按注册顺序
        var (pipeline, _) = BuildPipeline(VerificationKind.JsCookieGate, null, expensive, cheap);

        var outcome = await pipeline.HandleAsync(Probe(true));

        Assert.IsTrue(outcome.Solved);
        Assert.IsTrue(callLog.SequenceEqual(["FakeSolver(Cost=10)", "FakeSolver(Cost=50)"]));
    }

    [TestMethod]
    public async Task AllSolversFailedEntersCooldownAndAsksForManualAttention()
    {
        var solver = new FakeSolver(new List<string>(), 10, false, needsManualAttention: true);
        var (pipeline, _) = BuildPipeline(VerificationKind.JsCookieGate,
            new VerificationPolicy { CooldownSeconds = 600 }, solver);

        var first = await pipeline.HandleAsync(Probe(true));

        Assert.IsFalse(first.Solved);
        Assert.IsTrue(first.NeedsManualAttention);
        Assert.AreEqual(1, solver.CallCount);
        Assert.AreEqual(1, pipeline.BlockedHosts().Count);

        // 冷却期内不再尝试 ( 不对目标站持续施压 ) , 但仍如实报告被拦住
        var second = await pipeline.HandleAsync(Probe(true));

        Assert.IsFalse(second.Solved);
        Assert.AreEqual(VerificationKind.JsCookieGate, second.Challenge?.Kind);
        Assert.AreEqual(1, solver.CallCount);
    }

    [TestMethod]
    public async Task DisabledPolicyOnlyDetects()
    {
        var solver = new FakeSolver(new List<string>(), 10, true);
        var (pipeline, _) = BuildPipeline(VerificationKind.JsCookieGate, VerificationPolicy.Disabled, solver);

        var outcome = await pipeline.HandleAsync(Probe(true));

        Assert.IsTrue(outcome.Challenged);
        Assert.IsFalse(outcome.Solved);
        Assert.IsFalse(outcome.NeedsManualAttention);
        Assert.AreEqual(0, solver.CallCount);
    }

    [TestMethod]
    public async Task KindBlockedByPolicyAsksForManualAttention()
    {
        var solver = new FakeSolver(new List<string>(), 10, true);
        // 默认策略不放行滑块 : 识别得出来 , 但要求人工介入而不是悄悄跳过
        var (pipeline, _) = BuildPipeline(VerificationKind.SliderCaptcha,
            VerificationPolicy.WithKinds(VerificationKind.CloudflareChallenge), solver);

        var outcome = await pipeline.HandleAsync(Probe(true));

        Assert.IsFalse(outcome.Solved);
        Assert.IsTrue(outcome.NeedsManualAttention);
        Assert.AreEqual(0, solver.CallCount);
    }

    [TestMethod]
    public async Task SolverTimeoutFallsThroughToNextSolver()
    {
        var callLog = new List<string>();
        var slow = new FakeSolver(callLog, 10, true, delayMs: 3000);
        var fast = new FakeSolver(callLog, 20, true);
        var (pipeline, _) = BuildPipeline(VerificationKind.JsCookieGate,
            new VerificationPolicy { SolveTimeoutSeconds = 1 }, slow, fast);

        var outcome = await pipeline.HandleAsync(Probe(true));

        Assert.IsTrue(outcome.Solved);
        Assert.IsTrue(callLog.SequenceEqual(["FakeSolver(Cost=10)", "FakeSolver(Cost=20)"]));
    }

    [TestMethod]
    public async Task ConcurrentCallReusesFreshSession()
    {
        var solver = new FakeSolver(new List<string>(), 10, true, delayMs: 400);
        var (pipeline, _) = BuildPipeline(VerificationKind.JsCookieGate, null, solver);

        // 两次并发处理同一主机 : 后进临界区的那次应直接复用刚取得的会话 , 不重复起浏览器
        var first = pipeline.HandleAsync(Probe(true));
        await Task.Delay(100);
        var second = pipeline.HandleAsync(Probe(true));
        var outcomes = await Task.WhenAll(first, second);

        Assert.IsTrue(outcomes.All(outcome => outcome.Solved));
        Assert.AreEqual(1, solver.CallCount);
        Assert.AreEqual(1, outcomes.Count(outcome => outcome.Message.Contains("并发调用")));
    }

    [TestMethod]
    public async Task RateLimitedBacksOffByRetryAfterWithoutEscalating()
    {
        // 假策略只声明能处理 JS 门禁 : 限流没有任何自动手段 , 只能退避
        var solver = new FakeSolver(new List<string>(), 10, true, kinds: [VerificationKind.JsCookieGate]);
        var (pipeline, _) = BuildPipeline(VerificationKind.RateLimited,
            new VerificationPolicy { CooldownSeconds = 600 }, solver);

        var startedAt = DateTime.Now;
        var outcome = await pipeline.HandleAsync(Probe(true, retryAfterSeconds: 60));

        Assert.IsFalse(outcome.Solved);
        Assert.AreEqual(0, solver.CallCount);
        // 限流等一会儿自己就好 , 升级成"需要人工介入"是误导
        Assert.IsFalse(outcome.NeedsManualAttention);
        var block = pipeline.BlockedHosts().Single();
        Assert.AreEqual(VerificationKind.RateLimited, block.Kind);
        // 源站给的 Retry-After(60s) 比策略默认冷却(600s) 更了解自己的限流窗口 : 按它退避 , 不被默认值盖过去
        Assert.IsTrue(block.Until < startedAt.AddSeconds(120),
            $"冷却应按 Retry-After(60s) 退避 , 实际至 {block.Until:HH:mm:ss}");
    }

    [TestMethod]
    public async Task RiskControlBacksOffWithoutEscalating()
    {
        // 载荷级风控同样没有自动手段 ( 默认策略也不放行它 ) : 退避重试 , 不是人工介入
        var solver = new FakeSolver(new List<string>(), 10, true);
        var (pipeline, _) = BuildPipeline(VerificationKind.RiskControl, null, solver);

        var outcome = await pipeline.HandleAsync(Probe(true));

        Assert.IsFalse(outcome.Solved);
        Assert.IsFalse(outcome.NeedsManualAttention);
        Assert.AreEqual(0, solver.CallCount);
        Assert.AreEqual(1, pipeline.BlockedHosts().Count);
    }

    [TestMethod]
    public async Task CookieRelatedChallengeDropsStaleSession()
    {
        var solver = new FakeSolver(new List<string>(), 10, false);
        var (pipeline, store) = BuildPipeline(VerificationKind.JsCookieGate, null, solver);
        store.Set(NewSession("dead"));

        await pipeline.HandleAsync(Probe(true));

        // 被 cookie 类验证拦住 = 这份会话已不被服务端接受 , 丢掉以免冷却期内继续拿死 cookie 去撞
        Assert.IsNull(store.Get(Host));
    }

    [TestMethod]
    public async Task UnrelatedChallengeKeepsSession()
    {
        var solver = new FakeSolver(new List<string>(), 10, false, kinds: [VerificationKind.JsCookieGate]);
        var (pipeline, store) = BuildPipeline(VerificationKind.RateLimited, null, solver);
        store.Set(NewSession("alive"));

        await pipeline.HandleAsync(Probe(true, retryAfterSeconds: 60));

        // 限流与 cookie 无关 : 丢掉有效会话只会让下一个请求白多过一次验证
        Assert.IsNotNull(store.Get(Host));
    }

    [TestMethod]
    public void WithKindsAddsToDefaultsInsteadOfReplacing()
    {
        var policy = VerificationPolicy.WithKinds(VerificationKind.SliderCaptcha);

        Assert.IsTrue(policy.Allows(VerificationKind.SliderCaptcha));
        // 关键 : 追加而不是替换 —— 替换会让该源的 Cloudflare / JS 门禁自动通过静默失效
        Assert.IsTrue(policy.Allows(VerificationKind.CloudflareChallenge));
        Assert.IsTrue(policy.Allows(VerificationKind.JsCookieGate));
        // 没有自动手段的类型仍然不放行
        Assert.IsFalse(policy.Allows(VerificationKind.RateLimited));
        Assert.IsFalse(policy.Allows(VerificationKind.RiskControl));
    }

    private static VerificationSession NewSession(string cookieValue)
    {
        return VerificationSession.Create(Host, [new KeyValuePair<string, string>("sid", cookieValue)], 60);
    }

    /// <summary>假识别器 : 响应体带 CHALLENGE 标记即判为指定类型</summary>
    private sealed class FakeDetector(VerificationKind kind) : IVerificationDetector
    {
        public string Name => "FakeDetector";

        public VerificationChallenge? Detect(VerificationProbe probe)
        {
            if (!probe.BodySample.Contains(ChallengeMarker)) return null;
            return new VerificationChallenge
            {
                Kind = kind,
                DetectorName = Name,
                StatusCode = probe.StatusCode,
                Evidence = "fake challenge marker",
                RetryAfterSeconds = probe.RetryAfterSeconds
            };
        }
    }

    /// <summary>假策略 : 可配置代价 / 成败 / 耗时 , 并把调用顺序记到共享日志里供断言</summary>
    private sealed class FakeSolver(
        List<string> callLog,
        int cost,
        bool succeed,
        bool needsManualAttention = false,
        int delayMs = 0,
        IReadOnlyCollection<VerificationKind>? kinds = null) : IVerificationSolver
    {
        public string Name => $"FakeSolver(Cost={cost})";

        public int Cost => cost;

        public IReadOnlyCollection<VerificationKind> Kinds { get; } =
            kinds ?? Enum.GetValues<VerificationKind>();

        public int CallCount { get; private set; }

        public async Task<VerificationSolveResult> SolveAsync(VerificationSolveRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            callLog.Add(Name);
            if (delayMs > 0) await Task.Delay(delayMs, cancellationToken);
            if (!succeed) return VerificationSolveResult.Failed(Name, "fake failed", needsManualAttention);

            var session = VerificationSession.Create(request.Host,
                new Dictionary<string, string> { ["sid"] = "abc" }, 60);
            return VerificationSolveResult.Solved(Name, session, "fake solved");
        }
    }
}
