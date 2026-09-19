using KSpider.Model;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web;

/// <summary>
///     一页列表结果 : 列表项 + 下一页游标 ( 快讯型源走 FlashNewsPage , 不在这里 )
/// </summary>
public class NewsListPage
{
    public List<SpiderNewsListModel> Items { get; init; } = [];

    /// <summary>
    ///     下一页游标 , null = 没有更多 ; 游标对任务不透明 , 由各源自行解释 ( 东财 = 页码 )
    /// </summary>
    public string? NextCursor { get; init; }
}

/// <summary>
///     单条新闻的原始内容 ( 未解析 )
/// </summary>
public class NewsContentOrigin
{
    public string NewsUrl { get; set; } = "";

    public NewsContentOriginType OriginType { get; set; }

    public string NewsOriginContent { get; set; } = "";

    public NewsContentOriginStatus Status { get; set; }

    public string Message { get; set; } = "";

    public SpiderNewsContentOriginModel ToModel()
    {
        return new SpiderNewsContentOriginModel
        {
            NewsUrl = NewsUrl,
            NewsOriginContent = NewsOriginContent,
            NewsOriginType = (int)OriginType,
            Status = (int)Status,
            Message = Message
        };
    }
}

/// <summary>
///     解析结果 : 详情实体 + 图片列表实体
/// </summary>
public class NewsContentParseResult
{
    public SpiderNewsContentModel Content { get; init; } = new();

    public List<SpiderNewsImageListModel> Images { get; init; } = [];
}
