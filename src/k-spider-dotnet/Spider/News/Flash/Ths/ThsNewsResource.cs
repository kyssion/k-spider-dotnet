namespace KSpider.Spider.News.Flash.Ths;

/// <summary>
///     同花顺 7x24 快讯源常量 ( 2026-09-30 实测 ) :
///     匿名公开 JSON 无签名无专用头 ; digest 即完整全文 ( 实测最长 471 字完整收尾 ) , 列表即全文成立 ;
///     页码翻页 ( page 递增 , 实测 page2 与 page1 无重叠 ) ; pagesize=50 正常 ;
///     import/color 两档重要度恒对应 ( import=3 ↔ color=2 红标 ) , source 恒为空。
/// </summary>
public static class ThsNewsResource
{
    /// <summary>
    ///     7x24 快讯列表接口 ; tag 参数传栏目标签 id ( 空 = 全量流 )
    /// </summary>
    public const string FlashListUrl = "https://news.10jqka.com.cn/tapp/news/push/stock/";

    public const string ResourceHost = "news.10jqka.com.cn";

    /// <summary>
    ///     单页条数上限 : 实测 50 正常 , 统一钳制
    /// </summary>
    public const int MaxPageSize = 50;

    /// <summary>
    ///     同花顺源分类号 ( 501-599 段 )
    /// </summary>
    public const int FlashCategoryNumber = 501;

    /// <summary>
    ///     来源名回退 ( 接口 source 恒为空 )
    /// </summary>
    public const string NewsFromName = "同花顺";
}
