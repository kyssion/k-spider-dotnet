using KSpider.Model;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Flash;

/// <summary>
///     实时快讯源爬虫 : "列表即全文"的源 ( 财联社电报 / 新浪 7x24 / 见闻 live / 金十快讯 )。
///     与网页抓取型 ( INewsSpider 的列表 → 原始 → 解析三段 ) 不同 ,
///     拉取一次就得到完整可入库的记录 , 没有下载与解析阶段 , 也没有状态机。
///     加新快讯源时实现本接口并在 FlashNewsSpiderRegistry 注册一行。
/// </summary>
public interface IFlashNewsSpider
{
    /// <summary>
    ///     源标识 , 与 spider_flash_news.from_media 对应
    /// </summary>
    FromTypeOfNews FromMedia { get; }

    /// <summary>
    ///     本源的全部栏目 , 供快讯任务遍历与健康检查
    /// </summary>
    IReadOnlyList<NewsColumn> Columns { get; }

    /// <summary>
    ///     拉取一页快讯 , 返回可直接入库的完整记录 ( 不落库 )。
    ///     cursor 为上一页返回的 NextCursor , 首页传 null ;
    ///     页码翻页与时间游标翻页的源都走这一个入口。
    /// </summary>
    Task<FlashNewsPage> GetFlashPage(NewsColumn column, int pageSize, string? cursor);
}
