using KSpider.Spider.ClsNews;
using KSpider.Spider.Jin10News;
using KSpider.Spider.SinaNews;
using KSpider.Spider.WscnNews;

namespace KSpider.Spider.FlashNews;

/// <summary>
///     实时快讯源注册表 : 新增快讯源时在 Map 中加一行映射即可接入快讯管线。
///     与 NewsSpiderRegistry ( 网页抓取型 ) 分开维护 , 一个源只属于一种管线。
/// </summary>
public static class FlashNewsSpiderRegistry
{
    private static readonly Dictionary<FromTypeOfNews, IFlashNewsSpider> Map = new()
    {
        { FromTypeOfNews.ClsMedia, new ClsNewsSpider() },
        { FromTypeOfNews.SinaMedia, new SinaNewsSpider() },
        { FromTypeOfNews.WscnMedia, new WscnNewsSpider() },
        { FromTypeOfNews.Jin10Media, new Jin10NewsSpider() }
    };

    /// <summary>
    ///     全部在管的快讯源
    /// </summary>
    public static IReadOnlyCollection<IFlashNewsSpider> All => Map.Values;

    /// <summary>
    ///     按 from_media 取源实现 , 未注册返回 null ( 由调用方记日志并跳过 )
    /// </summary>
    public static IFlashNewsSpider? Get(int fromMedia)
    {
        return Map.GetValueOrDefault((FromTypeOfNews)fromMedia);
    }
}
