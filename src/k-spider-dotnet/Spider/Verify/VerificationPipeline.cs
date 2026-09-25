using KSpider.Common.Logger;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Verify;

/// <summary>
///     验证处理管线 : 识别 → 按策略选手段 → 逐个尝试 → 落会话 , 并负责"别把目标站打爆"的两件事 ——
///     同一主机串行处理 ( 多源任务并行跑 , 否则会同时起好几个浏览器 ) 、
///     失败后进入冷却期 ( 冷却期内不再尝试 , 只识别并告警 ) 。
///     实例持有冷却与会话状态 , 因此按实例构造 : 生产用 <see cref="VerificationRegistry" /> 里的默认实例 ,
///     测试用假识别器 / 假策略构造独立实例 , 互不干扰。
/// </summary>
public sealed class VerificationPipeline(
    IReadOnlyList<IVerificationDetector> detectors,
    IReadOnlyList<IVerificationSolver> solvers,
    VerificationSessionStore sessionStore,
    Func<string, VerificationPolicy> policyResolver)
{
    private static readonly ILogger Log = LogFactory.GetLogger<VerificationPipeline>();

    private readonly Dictionary<string, DateTime> _cooldownUntil = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SemaphoreSlim> _hostGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VerificationBlock> _lastBlock = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _stateLock = new();

    /// <summary>
    ///     识别响应是否被拦住 : 按注册顺序取第一个命中的识别器
    ///     ( 注册表里特征越具体的越靠前 , 所以 403 挑战页不会被笼统的"拒绝访问"先截胡 )
    /// </summary>
    public VerificationChallenge? Detect(VerificationProbe probe)
    {
        foreach (var detector in detectors)
        {
            var challenge = detector.Detect(probe);
            if (challenge != null) return challenge;
        }

        return null;
    }

    /// <summary>
    ///     处理一次被拦截的响应 : 成功返回可复用的会话 ( 调用方拿它重放请求 ) ,
    ///     失败返回原因并视情况进入冷却期。
    /// </summary>
    public async Task<VerificationOutcome> HandleAsync(VerificationProbe probe,
        CancellationToken cancellationToken = default)
    {
        var challenge = Detect(probe);
        if (challenge == null) return VerificationOutcome.NotChallenged;

        var policy = policyResolver(probe.Host);
        if (!policy.Enabled)
            return VerificationOutcome.Blocked(challenge, "该源未开启自动过验证 ( 只识别不处理 )", false);

        var cooldown = CooldownOf(probe.Host);
        if (cooldown != null)
            return VerificationOutcome.Blocked(challenge,
                $"该源处于验证冷却期 ( 至 {cooldown.Until:HH:mm:ss} , 上次 : {cooldown.Reason} )", false);

        // 被 cookie 类验证拦住 = 手上这份会话已不被服务端接受 , 丢掉它 ,
        // 否则冷却期内每个请求都继续拿着死 cookie 去撞 ( 限流 / 风控与 cookie 无关 , 不动会话 )
        if (VerificationKindTraits.IsCookieRelated(challenge.Kind) && sessionStore.Get(probe.Host) != null)
        {
            sessionStore.Remove(probe.Host);
            Log.LogInformation("[VerificationPipeline] 会话已失效并清除 , host : {Host} , kind : {Kind}",
                probe.Host, challenge.Kind);
        }

        var candidates = solvers
            .Where(solver => solver.Kinds.Contains(challenge.Kind) && policy.Allows(challenge.Kind))
            .OrderBy(solver => solver.Cost)
            .ToList();
        if (candidates.Count == 0)
        {
            // 限流 / 载荷级风控的唯一合理自动响应是退避 : 等一会儿自然恢复 , 升级成"人工介入"是误导
            var byWaiting = VerificationKindTraits.ResolvesByWaiting(challenge.Kind);
            var outcome = VerificationOutcome.Blocked(challenge,
                byWaiting ? "该类型只能退避重试 ( 无自动手段 )" : "没有可用于该验证类型的策略 ( 可能被策略禁止 )",
                !byWaiting);
            SetCooldown(probe.Host, challenge, outcome.Message, policy);
            return outcome;
        }

        var startedAt = DateTime.Now;
        var gate = HostGate(probe.Host);
        await gate.WaitAsync(cancellationToken);
        try
        {
            // 等锁期间可能已有别的调用把这次验证过掉了 : 只要会话是本次调用开始之后建立的 , 直接复用 , 不重复起浏览器
            var existing = sessionStore.Get(probe.Host);
            if (existing != null && existing.ClearedAt >= startedAt)
                return VerificationOutcome.Passed(challenge, existing, "并发调用已取得会话 , 直接复用");

            var needsManualAttention = false;
            foreach (var solver in candidates)
            {
                var result = await RunSolverAsync(solver, challenge, probe, policy, cancellationToken);
                if (result == null) continue;

                if (result.Success && result.Session != null)
                {
                    sessionStore.Set(result.Session);
                    ClearCooldown(probe.Host);
                    Log.LogInformation(
                        "[VerificationPipeline] 通过验证 , host : {Host} , kind : {Kind} , solver : {Solver} , {Message}",
                        probe.Host, challenge.Kind, solver.Name, result.Message);
                    return VerificationOutcome.Passed(challenge, result.Session, result.Message);
                }

                needsManualAttention |= result.NeedsManualAttention;
                Log.LogWarning(
                    "[VerificationPipeline] 策略未通过 , host : {Host} , kind : {Kind} , solver : {Solver} , {Message}",
                    probe.Host, challenge.Kind, solver.Name, result.Message);
            }

            var blocked = VerificationOutcome.Blocked(challenge,
                $"全部策略未通过 ( {string.Join(" / ", candidates.Select(item => item.Name))} )", needsManualAttention);
            SetCooldown(probe.Host, challenge, blocked.Message, policy);
            return blocked;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>当前处于验证冷却期的源 , 供 NewsCheckJob 汇总告警</summary>
    public IReadOnlyList<VerificationBlock> BlockedHosts()
    {
        lock (_stateLock)
        {
            var now = DateTime.Now;
            return _lastBlock.Values
                .Where(block => _cooldownUntil.TryGetValue(block.Host, out var until) && until > now)
                .ToList();
        }
    }

    /// <summary>
    ///     执行单个策略 : 超时与异常都按"该策略失败"处理并继续下一个 , 不向外抛 ——
    ///     一个手段失败不应该让整条请求直接失败 , 后面还有别的手段
    /// </summary>
    private static async Task<VerificationSolveResult?> RunSolverAsync(IVerificationSolver solver,
        VerificationChallenge challenge, VerificationProbe probe, VerificationPolicy policy,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(policy.SolveTimeoutSeconds));
        try
        {
            return await solver.SolveAsync(
                new VerificationSolveRequest { Challenge = challenge, Probe = probe, Policy = policy }, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.LogWarning("[VerificationPipeline] 策略超时 , host : {Host} , solver : {Solver} , 上限 : {Timeout}s",
                probe.Host, solver.Name, policy.SolveTimeoutSeconds);
            return null;
        }
        catch (Exception e)
        {
            Log.LogWarning("[VerificationPipeline] 策略异常 , host : {Host} , solver : {Solver} , err : {}",
                probe.Host, solver.Name, e);
            return null;
        }
    }

    private VerificationBlock? CooldownOf(string host)
    {
        lock (_stateLock)
        {
            if (!_cooldownUntil.TryGetValue(host, out var until) || until <= DateTime.Now) return null;
            return _lastBlock.GetValueOrDefault(host);
        }
    }

    private void SetCooldown(string host, VerificationChallenge challenge, string reason, VerificationPolicy policy)
    {
        // 限流按源站给的 Retry-After 退避 : 它比策略里的默认冷却更了解自己的限流窗口 ( 给 60 秒就退 60 秒 ,
        // 不该被默认值 600 秒盖过去 ) ; 源站没给才用策略默认值
        var seconds = challenge.Kind == VerificationKind.RateLimited && challenge.RetryAfterSeconds > 0
            ? challenge.RetryAfterSeconds
            : policy.CooldownSeconds;
        var until = DateTime.Now.AddSeconds(seconds);
        lock (_stateLock)
        {
            _cooldownUntil[host] = until;
            _lastBlock[host] = new VerificationBlock
            {
                Host = host, Kind = challenge.Kind, Until = until, Reason = reason
            };
        }

        Log.LogWarning(
            "[VerificationPipeline] 进入验证冷却 , host : {Host} , kind : {Kind} , 至 : {Until:yyyy-MM-dd HH:mm:ss} , 原因 : {Reason}",
            host, challenge.Kind, until, reason);
    }

    private void ClearCooldown(string host)
    {
        lock (_stateLock)
        {
            _cooldownUntil.Remove(host);
            _lastBlock.Remove(host);
        }
    }

    private SemaphoreSlim HostGate(string host)
    {
        lock (_stateLock)
        {
            if (_hostGates.TryGetValue(host, out var gate)) return gate;
            gate = new SemaphoreSlim(1, 1);
            _hostGates[host] = gate;
            return gate;
        }
    }
}
