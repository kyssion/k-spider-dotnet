using KSpider.Spider.News.Web;

namespace KSpider.Spider.News.Web.Sina;

/// <summary>
///     新浪财经文章源 ( 网页抓取型三段 ) 常量与栏目定义 , 与 7x24 快讯 ( Spider/News/Flash/Sina ) 是同一网站的两条管线 ,
///     共用 FromTypeOfNews.SinaMedia , 分别注册在 NewsSpiderRegistry 与 FlashNewsSpiderRegistry。
///     列表有两套体系 ( 2026-09 实测 ) :
///     ① 财经滚动 JSON 接口 ( feed.mix , 无需鉴权 ) —— 综合 Lid=2516 与经济要闻 Lid=2515 ;
///     ② 栏目滚动页 ( finance.sina.com.cn/roll/c/{cid}.shtml , 服务端渲染整页即完整列表 , 无翻页 ) —— 20 个细分栏目。
///     栏目页清单以实测存活为准 : 频道页上还挂着一批已下线的死链 cid ( 头条研报 230808 / 鹰眼预警 264124 /
///     美股卡片 40811 / 56409 / 57045 / 57046 等 , roll/c 下一律 404 ) , 不要照抄频道页链接。
/// </summary>
public static class SinaArticleResource
{
    /// <summary>
    ///     财经滚动列表接口 , {0}=lid 栏目号 , {1}=单页条数 , {2}=页码 ; 页码翻页 , 按 ctime 严格降序、页间不重叠
    /// </summary>
    public const string RollListUrl = "https://feed.mix.sina.com.cn/api/roll/get?pageid=153&lid={0}&k=&num={1}&page={2}";

    public const string RollListHost = "feed.mix.sina.com.cn";

    /// <summary>
    ///     栏目滚动页地址 , {0} 为栏目 cid ; 整页即该栏目最新完整列表 ( 活跃栏目约 200 条 , 低频栏目更少 ,
    ///     实测大盘评述仅 51 条 ) , ?page= 参数只跳回首页 , 无翻页
    /// </summary>
    public const string ColumnPageUrlTemplate = "https://finance.sina.com.cn/roll/c/{0}.shtml";

    public const string ColumnPageHost = "finance.sina.com.cn";

    /// <summary>
    ///     单页上限 : 实测 num 大于 50 被静默钳制到 50 ( 与财联社电报 rn 同款行为 , 请求侧主动钳制 )
    /// </summary>
    public const int MaxPageSize = 50;

    /// <summary>
    ///     文章源 category 编号段 ( 202-223 ) : 接在 7x24 快讯 201 之后 , 仍在新浪 201-299 段内。
    ///     综合滚动 ( 2516 ) 与各栏目页内容互有重叠 , 同一文章 URL 相同 , 重复由入库 ON CONFLICT 吸收 ( 先写入者的分类号生效 )
    /// </summary>
    public static readonly SinaArticleColumn[] ArticleColumnList =
    {
        // —— 体系 ① : 财经滚动 JSON 接口 ——
        new() { ColumnId = "roll-2516", ColumnName = "财经滚动", CategoryNumber = 202, Lid = 2516 },
        new() { ColumnId = "roll-2515", ColumnName = "经济要闻", CategoryNumber = 203, Lid = 2515 },

        // —— 体系 ② : 栏目滚动页 ( SSR 整页列表 ) ——
        new() { ColumnId = "page-56589", ColumnName = "大盘评述", CategoryNumber = 204, PageCid = 56589 },
        new() { ColumnId = "page-56598", ColumnName = "宏观研究", CategoryNumber = 205, PageCid = 56598 },
        new() { ColumnId = "page-56605", ColumnName = "市场研究", CategoryNumber = 206, PageCid = 56605 },
        new() { ColumnId = "page-56978", ColumnName = "机构观点", CategoryNumber = 207, PageCid = 56978 },
        new() { ColumnId = "page-56592", ColumnName = "上市公司", CategoryNumber = 208, PageCid = 56592 },
        new() { ColumnId = "page-56615", ColumnName = "主力动向", CategoryNumber = 209, PageCid = 56615 },
        new() { ColumnId = "page-57027", ColumnName = "港股市场快讯", CategoryNumber = 210, PageCid = 57027 },
        new() { ColumnId = "page-57028", ColumnName = "港股大行研报", CategoryNumber = 211, PageCid = 57028 },
        new() { ColumnId = "page-57038", ColumnName = "港股公司新闻", CategoryNumber = 212, PageCid = 57038 },
        new() { ColumnId = "page-56907", ColumnName = "基金市场", CategoryNumber = 213, PageCid = 56907 },
        new() { ColumnId = "page-56982", ColumnName = "外汇", CategoryNumber = 214, PageCid = 56982 },
        new() { ColumnId = "page-56988", ColumnName = "期货", CategoryNumber = 215, PageCid = 56988 },
        new() { ColumnId = "page-56995", ColumnName = "期市要闻", CategoryNumber = 216, PageCid = 56995 },
        new() { ColumnId = "page-57085", ColumnName = "黄金分析", CategoryNumber = 217, PageCid = 57085 },
        new() { ColumnId = "page-56683", ColumnName = "银行要闻", CategoryNumber = 218, PageCid = 56683 },
        new() { ColumnId = "page-56689", ColumnName = "央行动态", CategoryNumber = 219, PageCid = 56689 },
        new() { ColumnId = "page-80798", ColumnName = "银行公司动态", CategoryNumber = 220, PageCid = 80798 },
        new() { ColumnId = "page-56757", ColumnName = "保险要闻", CategoryNumber = 221, PageCid = 56757 },
        new() { ColumnId = "page-56850", ColumnName = "保险行业动态", CategoryNumber = 222, PageCid = 56850 },
        new() { ColumnId = "page-56854", ColumnName = "保险公司动态", CategoryNumber = 223, PageCid = 56854 }
    };

    public struct SinaArticleColumn
    {
        /// <summary>栏目标识 ( 日志与健康检查定位 ) : 接口栏目 roll-{lid} , 栏目页栏目 page-{cid}</summary>
        public string ColumnId { get; set; }

        public string ColumnName { get; set; }

        public int CategoryNumber { get; set; }

        /// <summary>滚动接口栏目号 ( 体系 ① ) ; 栏目页栏目此值为 0</summary>
        public int Lid { get; set; }

        /// <summary>栏目滚动页 cid ( 体系 ② ) ; 接口栏目此值为 0</summary>
        public int PageCid { get; set; }
    }
}
