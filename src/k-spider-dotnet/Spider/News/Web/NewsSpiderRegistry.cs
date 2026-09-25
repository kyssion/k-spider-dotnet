using KSpider.Spider.News.Web.Cls;
using KSpider.Spider.News.Web.Eastmoney;

namespace KSpider.Spider.News.Web;

/// <summary>
///     网页抓取型新闻源注册表 ( 列表 → 原始 → 解析三段管线 ) :
///     新增源时在 Map 中加一行映射即可接入公共流水线。
///     "列表即全文"的实时快讯源走 FlashNewsSpiderRegistry , 不要注册到这里。
///     财联社一个网站两种内容形态 : 电报在快讯注册表 ( ClsMedia ) , 文章频道在这里 ( ClsArticleMedia ) ,
///     两者用不同的 FromMedia 值维持"一个源只属于一种管线"的约定。
/// </summary>
public static class NewsSpiderRegistry
{
    private static readonly Dictionary<FromTypeOfNews, INewsSpider> Map = new()
    {
        // { FromTypeOfNews.DfMedia, new DfNewsSpider() },
        { FromTypeOfNews.ClsArticleMedia, new ClsArticleSpider() }
    };

    /// <summary>
    ///     全部在管的源
    /// </summary>
    public static IReadOnlyCollection<INewsSpider> All => Map.Values;

    /// <summary>
    ///     按 from_media 取源实现 , 未注册返回 null ( 由调用方记日志并跳过 )
    /// </summary>
    public static INewsSpider? Get(int fromMedia)
    {
        return Map.GetValueOrDefault((FromTypeOfNews)fromMedia);
    }
}
