using KSpider.Spider.Policy.Csrc;
using KSpider.Spider.Policy.Stats;

namespace KSpider.Spider.Policy;

/// <summary>
///     官方文件源清单 ( Policy 管线 ) : 政府/官方站点每站结构各异 ( TRS 建站但容器/编码不一 ) ,
///     一站一实现 ; 出现结构同构的站群 ( 如多部委同为标准 TRS ) 时再评估抽取通用解析器。
///     新增官方源 = 实现 IPolicySpider + 本清单加一行。
/// </summary>
public static class PolicySpiderRegistry
{
    private static readonly IReadOnlyList<IPolicySpider> Spiders =
    [
        new StatsPolicySpider(),
        new CsrcPolicySpider()
    ];

    /// <summary>全部在管的官方文件源</summary>
    public static IReadOnlyList<IPolicySpider> All => Spiders;
}
