namespace KSpider.Spider.Announcement.Cninfo;

/// <summary>
///     巨潮资讯公告源常量 ( 2026-09-30 实测 ) :
///     POST hisAnnouncement/query 公开 JSON ( 表单编码 , 带 Referer ) ;
///     **pageSize 服务端钳 30** ( 请求 100/200 均实得 30 ) ; 排序为时间从新到旧 ;
///     分类白名单控制订阅范围 ( 全市场全类型每日数千条噪声大 , 核心分类约 500 条/日 ) ;
///     公告发布即终态 , 补充更正公告本身就是新 announcementId 的新行 ( 契约见 docs/architecture.md )。
/// </summary>
public static class CninfoAnnouncementResource
{
    public const string ParserCode = "cninfo-announcement-v1";

    public const string QueryUrl = "http://www.cninfo.com.cn/new/hisAnnouncement/query";

    public const string ResourceHost = "www.cninfo.com.cn";

    /// <summary>
    ///     附件根地址 : adjunctUrl ( finalpage/2026-09-30/{id}.PDF ) 拼接后为完整 PDF 地址
    /// </summary>
    public const string PdfUrlPrefix = "http://static.cninfo.com.cn/";

    /// <summary>
    ///     单页条数上限 ( 实测服务端钳 30 )
    /// </summary>
    public const int MaxPageSize = 30;

    /// <summary>
    ///     公告分类白名单 ( 巨潮 category 代码 ) : 定期报告 / 业绩 / 股权激励 / 增发 /
    ///     风险提示与澄清致歉 / 权益分派 / 董事会决议 ; 加分类 = 数组加行
    /// </summary>
    public static readonly string[] AnnouncementCategoryList =
    [
        "category_yjygjxz_szsh", // 业绩预告
        "category_yjkb_szsh",    // 业绩快报
        "category_ndbg_szsh",    // 年度报告
        "category_bndbg_szsh",   // 半年度报告
        "category_gqjl_szsh",    // 股权激励
        "category_zf_szsh",      // 增发
        "category_fxts_szsh",    // 风险提示
        "category_cqdq_szsh",    // 澄清致歉
        "category_qyfpxzcs_szsh",// 权益分派
        "category_dshgg_szsh"    // 董事会决议
    ];

    /// <summary>
    ///     tabName 固定 fulltext ( 全文检索库 , 含沪深京三市 )
    /// </summary>
    public const string TabName = "fulltext";

    public const string NewsFromName = "巨潮资讯";
}
