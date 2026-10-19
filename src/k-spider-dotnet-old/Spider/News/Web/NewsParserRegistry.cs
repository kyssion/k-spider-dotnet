namespace KSpider.Spider.News.Web;

/// <summary>
///     解析器路由 : parser_code → 源实现 , 供数据重放作业按 origin 行路由解析方法。
///     码未命中 ( 存量行未标记 / 旧版本码 ) 时回退按 from_media 走 <see cref="NewsSpiderRegistry" /> ,
///     与 NewsContentJob 的现行路由同源 , 两条路径不会漂移。
/// </summary>
public static class NewsParserRegistry
{
    private static readonly Dictionary<string, INewsSpider> CodeMap = NewsSpiderRegistry.All
        .GroupBy(spider => spider.ParserCode, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    /// <summary>全部解析器码 ( 供重放配置页下拉 )</summary>
    public static IReadOnlyCollection<string> ParserCodes => CodeMap.Keys;

    /// <summary>
    ///     重放路由 : 先按 parser_code 精确命中 ; 空/未知码回退按 from_media 走注册表 , 再不命中返回 null ( 调用方计失败 )
    /// </summary>
    public static INewsSpider? Resolve(string? parserCode, int fromMedia)
    {
        if (!string.IsNullOrEmpty(parserCode) && CodeMap.TryGetValue(parserCode, out var byCode))
            return byCode;
        return NewsSpiderRegistry.Get(fromMedia);
    }
}
