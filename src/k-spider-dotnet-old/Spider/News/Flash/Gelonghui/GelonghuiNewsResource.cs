namespace KSpider.Spider.News.Flash.Gelonghui;

/// <summary>
///     格隆汇 live 快讯源常量 ( 2026-09-30 实测 , 接口路径从前端 chunk c7be353.js 反查 ) :
///     匿名公开 JSON 无签名 ; v4 接口固定返回 15 条 ( limit 参数不生效 ) ,
///     翻页用 liveId = 上一页最老 id ( 实测页间零重叠 ) ;
///     **timestamp 毫秒参数必带** ( 不带会命中服务端缓存 , 恒返回同一批旧数据 ) ;
///     route 自带规范详情页地址 ; level 0/1 两档 ( 1 = 站内红标重要 ) ; source 多为空。
/// </summary>
public static class GelonghuiNewsResource
{
    /// <summary>
    ///     live 快讯 v4 接口 : {0} = liveId 翻页游标 ( 首页传空 ) , {1} = timestamp 毫秒
    /// </summary>
    public const string LiveUrlTemplate =
        "https://www.gelonghui.com/api/live-channels/all/lives/v4?category=all&liveId={0}&limit=15&timestamp={1}";

    public const string ResourceHost = "www.gelonghui.com";

    /// <summary>
    ///     单页条数 ( 接口固定返回 15 , 不可调 )
    /// </summary>
    public const int FixedPageSize = 15;

    /// <summary>
    ///     格隆汇源分类号 ( 601-699 段 )
    /// </summary>
    public const int FlashCategoryNumber = 601;

    public const string NewsFromName = "格隆汇";
}
