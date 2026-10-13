namespace KSpider.Spider.Dataset.LootDrop;

/// <summary>
///     Loot Drop 数据集源 ( 第七管线 Dataset 数据集型首源 ) 常量 :
///     站点本体是 Vite SPA ( www.loot-drop.io ) , 全部数据存 Supabase Postgres ,
///     前端浏览器直查 PostgREST REST 接口 —— 因此抓取不走 HTML , 直连同一 REST 通道。
///     接口与 key 均为站点公开面 ( publishable key 随前端 bundle 分发给每个访客 , 无需鉴权 ,
///     实测无反爬无频率拦截 ; 2026-10-12 侦察 , 侦察结论见 docs/architecture.md 数据集型契约 )。
/// </summary>
public static class LootDropDatasetResource
{
    /// <summary>Supabase 项目 REST 根 ( PostgREST )</summary>
    public const string RestBaseUrl = "https://lentxykytbylpxytluic.supabase.co/rest/v1";

    /// <summary>发布密钥 ( 站点公开面 , 随前端 JS 分发 , 仅作匿名身份标识 )</summary>
    public const string ApiKey = "sb_publishable_W5UgIXv8SGHeo43duatMCw_0h8GbgCY";

    public const string ResourceHost = "lentxykytbylpxytluic.supabase.co";

    /// <summary>startups 表路径 ( 案例主表 )</summary>
    public const string StartupsPath = "/startups";

    /// <summary>
    ///     单页条数 : PostgREST 无钳制实测 , 1000/页 × 2 页拉完全集 ( 约 1800 行 )。
    ///     1000 而非更大 , 控制单条 SQL/响应体积
    /// </summary>
    public const int PageSize = 1000;

    /// <summary>全量翻页行数上限 ( 防游标异常导致的无限翻页 )</summary>
    public const int MaxRows = 100_000;

    /// <summary>
    ///     拉取 URL 模板 : {0}=页内偏移 , {1}=页大小 。
    ///     id 升序 + offset 翻页 ( 数据集有界 , offset 无跳变风险 ; 行会修订故不用时间水位 )
    /// </summary>
    public const string StartupsPageUrlTemplate =
        RestBaseUrl + StartupsPath + "?select=*&order=id.asc&limit={1}&offset={0}";

    /// <summary>对账探测 URL : 只取 id 且带 count , 响应头 Content-Range 给出总数</summary>
    public const string CountProbeUrl = RestBaseUrl + StartupsPath + "?select=id&limit=1";

    public const string NewsFromName = "Loot Drop";
}
