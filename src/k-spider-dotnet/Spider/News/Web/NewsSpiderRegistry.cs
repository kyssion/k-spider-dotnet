using KSpider.Spider.News.Web.Cls;
using KSpider.Spider.News.Web.Eastmoney;
using KSpider.Spider.News.Web.Jin10;
using KSpider.Spider.News.Web.Sina;
using KSpider.Spider.News.Web.Wscn;

namespace KSpider.Spider.News.Web;

/// <summary>
///     网页抓取型新闻源注册表 ( 列表 → 原始 → 解析三段管线 ) :
///     新增源时在 Map 中加一行映射即可接入公共流水线。
///     "列表即全文"的实时快讯源走 FlashNewsSpiderRegistry , 不要注册到这里。
///     同一网站可以有两种内容形态 ( 枚举标识"网站来源" , 管线归属由注册表决定 ) :
///     财联社 ClsMedia —— 电报在快讯注册表 , 文章频道在这里 ;
///     新浪 SinaMedia —— 7x24 快讯在快讯注册表 , 文章源在这里 ;
///     见闻 WscnMedia —— live 快讯在快讯注册表 , 文章源在这里 ;
///     金十 Jin10Media —— 快讯在快讯注册表 , 「市场参考」文章源在这里。
/// </summary>
public static class NewsSpiderRegistry
{
    private static readonly Dictionary<FromTypeOfNews, INewsSpider> Map = new()
    {
        { FromTypeOfNews.DfMedia, new DfNewsSpider() },
        { FromTypeOfNews.ClsMedia, new ClsArticleSpider() },
        { FromTypeOfNews.SinaMedia, new SinaArticleSpider() },
        { FromTypeOfNews.WscnMedia, new WscnArticleSpider() },
        { FromTypeOfNews.Jin10Media, new Jin10ArticleSpider() }
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
