using KSpider.Model;

namespace KSpider.Spider.News;

/// <summary>
///     网页抓取型新闻源爬虫 : 有独立详情页的源 ( 列表 → 原始内容 → 解析三段 ) ,
///     一个源一个实现 , 新增源时实现本接口并在 NewsSpiderRegistry 注册一行。
///     "列表即全文"的实时快讯源走 IFlashNewsSpider , 不要实现本接口。
/// </summary>
public interface INewsSpider
{
    /// <summary>
    ///     源标识 , 与 spider_news_list.from_media 对应
    /// </summary>
    FromTypeOfNews FromMedia { get; }

    /// <summary>
    ///     本源的全部栏目 , 供列表任务遍历与健康检查
    /// </summary>
    IReadOnlyList<NewsColumn> Columns { get; }

    /// <summary>
    ///     抓取指定栏目的一页列表 , 返回归一化后的列表实体 ( 不落库 )
    ///     cursor 为上一页返回的 NextCursor , 首页传 null ; 页码翻页与时间游标翻页的源都走这一个入口
    /// </summary>
    Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor);

    /// <summary>
    ///     下载单条新闻的原始内容 ( 详情页 / 详情接口 )
    /// </summary>
    Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem);

    /// <summary>
    ///     解析原始内容为结构化详情与图片列表 ( 不落库 )
    /// </summary>
    NewsContentParseResult ParseContent(string originContent, string newsUrl);
}
