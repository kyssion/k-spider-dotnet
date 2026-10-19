using KSpider.Model;

namespace KSpider.Spider.News.Flash;

/// <summary>
///     一页快讯结果 : 完整记录 + 下一页游标 ( 供停机回补时向更早翻页 )
/// </summary>
public class FlashNewsPage
{
    /// <summary>
    ///     本页快讯记录 , 均为可直接入库的完整记录
    /// </summary>
    public List<SpiderFlashNewsModel> Items { get; init; } = [];

    /// <summary>
    ///     下一页游标 , null = 没有更多 ; 游标对任务不透明 , 由各源自行解释
    /// </summary>
    public string? NextCursor { get; init; }
}
