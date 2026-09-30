using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Cls;

/// <summary>
///     财联社文章频道 ( 站内称"深度/depth" , 与电报快讯 Spider/News/Flash/Cls 是两条管线 ) 常量与栏目定义。
///     频道清单来自 GET /v2/base/common_config 的 column_bar 字段 ( 2026-09 实测 14 个 ,
///     与官网顶部导航一致 ) ; 品见是同站第二列表族 ( 走专用接口 /v5/web/pinjian/assembled2 ) ,
///     招财号是机构入驻 UGC 平台 ( App 内入口为主 , 网页侧无确认可用的匿名列表接口 ) , 未接入。
///     接口参数与签名算法同电报 ( 见 ClsSignature ) , 实测 sv = 8.7.9 有效。
/// </summary>
public static class ClsArticleResource
{

    /// <summary>解析器标识 : 财联社文章 SSR __NEXT_DATA__ → 结构化片段 ( origin 行路由用 )</summary>
    public const string ParserCode = "cls-article-v1";
    /// <summary>
    ///     频道文章列表接口 , {0} 为频道 id ; 时间游标翻页 ( last_time = 上一页最老一条 ctime )
    /// </summary>
    public const string DepthListUrl = "https://www.cls.cn/v3/depth/list/{0}";
    public const string ResourceHost = "www.cls.cn";

    /// <summary>
    ///     品见专用接口 : 固定拼装首页流 ( 2026-09-30 实测 4 个真实专题 × 各 6 篇 + 11 个置顶专题卡 ,
    ///     rn / last_time 均不影响返回 , 整页即全量、无翻页 ) ;
    ///     文章 id 与财联社全局 id 同空间 , 详情页复用 /detail/{id} __NEXT_DATA__ 流程
    /// </summary>
    public const string PinjianAssembledUrl = "https://www.cls.cn/v5/web/pinjian/assembled2";

    /// <summary>
    ///     品见伪栏目号 ( NewsColumn.ColumnId ) : depth 频道用数字 id , 品见用它区分列表族
    /// </summary>
    public const string PinjianColumnId = "pinjian";

    public const string PinjianChannelName = "品见";

    /// <summary>
    ///     品见 category ( 接在文章频道 102-114 之后 , 仍在财联社 101-199 段内 )
    /// </summary>
    public const int PinjianCategoryNumber = 115;

    /// <summary>
    ///     详情页地址作为 news_url 的稳定形态。文章与电报共用同一套全局 id ,
    ///     一个 id 只属于一种内容类型 , 与电报的 /detail/{id} 不会撞键 ( 且两者落不同的表 )
    /// </summary>
    public const string DetailUrlTemplate = "https://www.cls.cn/detail/{0}";

    /// <summary>
    ///     单页请求条数 : 站点前端固定 rn=20。实测服务端不按 rn 裁剪响应 ( 恒返回约 30 行 ,
    ///     头条频道首页可达 50+ ) , 因此不能用"短页"判断末页 , 以空页为准
    /// </summary>
    public const int RequestPageSize = 20;

    /// <summary>
    ///     文章频道的 category 编号段 ( 102-114 ) : 接在电报 101 之后 , 仍在财联社 101-199 段内
    /// </summary>
    public static readonly ArticleChannelResource[] ArticleChannelResourceList =
    {
        new() { ChannelId = 1000, ChannelName = "头条", CategoryNumber = 102 },
        new() { ChannelId = 1003, ChannelName = "A股", CategoryNumber = 103 },
        new() { ChannelId = 1135, ChannelName = "港股", CategoryNumber = 104 },
        new() { ChannelId = 1007, ChannelName = "环球", CategoryNumber = 105 },
        new() { ChannelId = 1005, ChannelName = "公司", CategoryNumber = 106 },
        new() { ChannelId = 1118, ChannelName = "券商", CategoryNumber = 107 },
        new() { ChannelId = 1110, ChannelName = "基金ETF", CategoryNumber = 108 },
        new() { ChannelId = 1006, ChannelName = "地产", CategoryNumber = 109 },
        new() { ChannelId = 1032, ChannelName = "金融", CategoryNumber = 110 },
        new() { ChannelId = 1119, ChannelName = "汽车", CategoryNumber = 111 },
        new() { ChannelId = 1111, ChannelName = "科创", CategoryNumber = 112 },
        new() { ChannelId = 1124, ChannelName = "期货", CategoryNumber = 113 },
        new() { ChannelId = 1176, ChannelName = "投教", CategoryNumber = 114 }
    };

    public struct ArticleChannelResource
    {
        /// <summary>频道 id ( /v3/depth/list/{id} 的路径参数 , 同时作为 NewsColumn 的 ColumnId )</summary>
        public int ChannelId { get; set; }

        public string ChannelName { get; set; }

        public int CategoryNumber { get; set; }
    }
}
