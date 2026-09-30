namespace KSpider.Spider.News.Web.Nbd;

/// <summary>
///     每日经济新闻（每经网）文章源常量 ( 2026-09-30 实测 ) :
///     栏目页为 SSR HTML 列表 ( UTF-8 , 每页约 80 条 ) , **整页即一批无翻页** ( 每轮重拉由入库去重吸收 ) ;
///     文章链接自带完整日期 ( /articles/{yyyy-MM-dd}/{id}.html ) ; 详情页正文在 div.u-editor 的 p 段落 ,
///     时间 "yyyy-MM-dd HH:mm:ss" ; title 标签为空用 h1。
/// </summary>
public static class NbdArticleResource
{
    /// <summary>解析器标识 : 每经文章 SSR HTML → 结构化片段 ( origin 行路由用 )</summary>
    public const string ParserCode = "nbd-article-v1";

    /// <summary>
    ///     栏目列表页模板 , {0} 为栏目 id
    /// </summary>
    public const string ListUrlTemplate = "https://www.nbd.com.cn/columns/{0}/";

    public const string ResourceHost = "www.nbd.com.cn";

    /// <summary>
    ///     每经头条栏目分类号 ( 段内 701-703 , 段锁测试用 )
    /// </summary>
    public const int HeadlineCategoryNumber = 701;

    /// <summary>
    ///     文章栏目清单 ( 挑时间流型编辑栏目 ; 公告/专题精选不接 )
    /// </summary>
    public static readonly NbdArticleColumnResource[] ArticleColumnResourceList =
    {
        new() { ColumnId = "1161", ColumnName = "每经头条", CategoryNumber = 701 },
        new() { ColumnId = "1072", ColumnName = "每经热评", CategoryNumber = 702 },
        new() { ColumnId = "332", ColumnName = "重磅原创", CategoryNumber = 703 }
    };

    public struct NbdArticleColumnResource
    {
        /// <summary>栏目 id ( 列表页 URL 的 columns/{id} 部分 , 同时作为 NewsColumn 的 ColumnId )</summary>
        public string ColumnId { get; set; }

        public string ColumnName { get; set; }

        public int CategoryNumber { get; set; }
    }
}
