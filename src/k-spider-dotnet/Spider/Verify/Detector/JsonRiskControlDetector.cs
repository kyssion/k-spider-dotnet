using System.Text.Json;
using System.Text.Json.Nodes;
using KSpider.Spider.Verify.Model;

namespace KSpider.Spider.Verify.Detector;

/// <summary>
///     载荷级风控识别器 : 状态码 200 、载荷却是风控 / 鉴权失败 ——
///     这是最危险的一类拦截 , 因为调用方看着"请求成功了" , 实际一条数据都拿不到。
///     识别器与内容型识别器互补 : 它只处理非 HTML 载荷 ( 各源正常业务响应都是 JSON ) ,
///     并且只沿对象层级读 envelope 上的提示语字段 , 不进业务数据数组 ( 见 <see cref="CollectMessages" /> )。
/// </summary>
public sealed class JsonRiskControlDetector : IVerificationDetector
{
    /// <summary>
    ///     风控 / 鉴权类措辞。刻意**不含**三类 :
    ///     "签名" —— 财联社签名错误是前端版本号过期 ( 属源改版 , 有独立处理路径 , 见 AGENTS 已知坑 ) ;
    ///     "校验" / "请求异常" —— 源站自己的参数或内部报错 , 不是风控 ;
    ///     裸 "risk" / "verify" —— 英文里太常见 ( 金融文本尤其 ) , 拿它当特征必然误判。
    /// </summary>
    private static readonly string[] RiskKeywords =
    [
        "验证", "风控", "拦截", "拒绝访问", "频繁", "访问受限", "非法请求",
        "forbidden", "unauthorized", "captcha", "blocked", "too many requests", "rate limit",
        "access denied"
    ];

    /// <summary>载荷里可能承载提示语的字段名 ( 各源命名不同 , 逐一覆盖 )</summary>
    private static readonly string[] MessageFields =
    [
        "msg", "message", "errmsg", "error", "info", "desc", "retmsg", "errormsg"
    ];

    /// <summary>对象层级的扫描深度上限 ( 提示语在 envelope 上 , 不会嵌得太深 )</summary>
    private const int MaxScanDepth = 4;

    /// <summary>收集条数上限 , 避免对整篇业务数据全量遍历</summary>
    private const int MaxMessageScan = 40;

    /// <summary>识别器名 , 会写进 <see cref="VerificationChallenge.DetectorName" /></summary>
    public string Name => "JsonRiskControlDetector";

    /// <summary>非 HTML 载荷解析成 JSON 后收集 envelope 提示语 , 命中风控措辞即判 RiskControl</summary>
    public VerificationChallenge? Detect(VerificationProbe probe)
    {
        if (probe.IsHtml || probe.BodySample.Length == 0) return null;

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(probe.BodySample);
        }
        catch (JsonException)
        {
            // 不是 JSON 就不是本识别器的事 ( 取样被截断的大载荷也会走到这里 —— 风控响应本身都很小 )
            return null;
        }

        var messages = new List<string>();
        CollectMessages(root, 0, messages);
        foreach (var message in messages)
        {
            var keyword = RiskKeywords.FirstOrDefault(
                item => message.Contains(item, StringComparison.OrdinalIgnoreCase));
            if (keyword == null) continue;
            return new VerificationChallenge
            {
                Kind = VerificationKind.RiskControl,
                DetectorName = Name,
                StatusCode = probe.StatusCode,
                Evidence = $"载荷提示 {message} ( 命中 \"{keyword}\" )"
            };
        }

        return null;
    }

    /// <summary>
    ///     只沿对象层级收集提示语字段 , **不进入数组** : 风控提示挂在响应 envelope 上 ,
    ///     而业务数据都在数组里 ( 新闻条目列表 ) —— 进去扫等于把每条新闻的 desc / info 当风控提示读 ,
    ///     一条含 "risk" 或 "验证" 字样的新闻就能让整个源误判进冷却。
    /// </summary>
    private static void CollectMessages(JsonNode? node, int depth, List<string> found)
    {
        if (node is not JsonObject jsonObject || depth > MaxScanDepth || found.Count >= MaxMessageScan) return;
        foreach (var pair in jsonObject)
        {
            if (pair.Value is JsonValue value &&
                MessageFields.Contains(pair.Key, StringComparer.OrdinalIgnoreCase) &&
                value.TryGetValue<string>(out var text) &&
                !string.IsNullOrWhiteSpace(text))
                found.Add($"{pair.Key}=\"{text}\"");
            CollectMessages(pair.Value, depth + 1, found);
        }
    }
}
