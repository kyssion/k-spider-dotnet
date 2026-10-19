using KSpider.Spider.Verify.Detector;
using KSpider.Spider.Verify.Pipeline;
using KSpider.Spider.Verify.Solver;
using KSpider.Spider.Verify.Solver.Browser;

namespace KSpider.Spider.Verify;

/// <summary>
///     验证识别器与通过策略的注册表 , 也是本模块的唯一装配点 :
///     新增一种验证方式 = 加一个识别器实现 + 在这里注册一行 ;
///     新增一种通过手段 = 加一个策略实现 + 在这里注册一行。
///     管线、抓取门面 ( VerifiedHttp ) 与健康检查都从这里取 , 无需改动调用方。
///     按主机覆盖策略也用这里 ( 见文件末尾的写法示例 ) 。
/// </summary>
public static class VerificationRegistry
{
    /// <summary>
    ///     识别器顺序即判定优先级 : 特征越具体的越靠前。
    ///     内容型 ( Cloudflare / JS 门禁 / 验证码 ) 必须先于传输层判定 ,
    ///     否则一个 403 的挑战页会被笼统的"拒绝访问"先截胡 , 丢掉真正的验证类型。
    /// </summary>
    private static readonly List<IVerificationDetector> DetectorList =
    [
        new CloudflareDetector(),
        new JsCookieGateDetector(),
        new CaptchaDetector(),
        new JsonRiskControlDetector(),
        new HttpGateDetector()
    ];

    /// <summary>通过策略按代价升序尝试 ( 见 IVerificationSolver.Cost ) , 注册顺序不影响选择</summary>
    private static readonly List<IVerificationSolver> SolverList =
    [
        new BrowserChallengeSolver(DetectorList),
        new BrowserSliderSolver(DetectorList),
        new ManualEscalationSolver()
    ];

    private static readonly Dictionary<string, VerificationPolicy> HostPolicyMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>已通过验证的会话缓存 ( 按主机 ) , 进程内复用 , 过验证不必每个请求都来一遍</summary>
    public static VerificationSessionStore SessionStore { get; } = new();

    /// <summary>生产用管线实例 ( 冷却状态与主机锁都挂在它身上 )</summary>
    public static VerificationPipeline Pipeline { get; } = new(DetectorList, SolverList, SessionStore, PolicyFor);

    public static IReadOnlyList<IVerificationDetector> Detectors => DetectorList;

    public static IReadOnlyList<IVerificationSolver> Solvers => SolverList;

    /// <summary>追加识别器到末尾 ( 注册表里越靠后优先级越低 , 通用兜底型识别器适合追加 )</summary>
    public static void RegisterDetector(IVerificationDetector detector)
    {
        DetectorList.Add(detector);
    }

    /// <summary>追加通过策略 ( 选择顺序按 Cost , 与追加顺序无关 )</summary>
    public static void RegisterSolver(IVerificationSolver solver)
    {
        SolverList.Add(solver);
    }

    /// <summary>按主机覆盖策略 , 未覆盖的主机用 <see cref="VerificationPolicy.Default" /></summary>
    public static void SetPolicy(string host, VerificationPolicy policy)
    {
        HostPolicyMap[host] = policy;
    }

    public static VerificationPolicy PolicyFor(string host)
    {
        return HostPolicyMap.TryGetValue(host, out var policy) ? policy : VerificationPolicy.Default;
    }

    // 按源覆盖策略的写法 ( 默认策略已覆盖 JS / Cloudflare 类挑战 , 这里只列两类常见需求 ) :
    //   关闭某源的自动过验证 ( 只识别并告警 ) :
    //     SetPolicy(DfNewsResource.ListResourceHost, VerificationPolicy.Disabled);
    //   放行滑块自动处理 ( 确认自己有权抓取该站后再放开 ) :
    //     SetPolicy("www.example.com", VerificationPolicy.WithKinds(VerificationKind.SliderCaptcha));
}
