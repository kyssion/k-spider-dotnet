using KSpider.Model;

namespace KSpider.Spider.FlashNews;

/// <summary>
///     一页快讯结果 : 完整记录 + 下一页游标 ( 供停机回补时向更早翻页 )
/// </summary>
public class FlashNewsPage
{
    public List<SpiderFlashNewsModel> Items { get; init; } = [];

    /// <summary>
    ///     下一页游标 , null = 没有更多 ; 游标对任务不透明 , 由各源自行解释
    /// </summary>
    public string? NextCursor { get; init; }
}
