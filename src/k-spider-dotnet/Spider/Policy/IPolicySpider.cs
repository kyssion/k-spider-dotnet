using KSpider.Model;
using KSpider.Spider.News;
using KSpider.Spider.News.Web;

namespace KSpider.Spider.Policy;

/// <summary>
///     官方文件源爬虫 ( Policy 管线 , 第六管线 ) : 政府/官方机构发布的文件与政策——
///     文件元数据 ( 发文机关/文号 ) + **正文全文** ( 政策文本即核心语料 , 与公告"链接即交付物"的本质区别 )。
///     两步取数 : 列表页存根 ( 停止判定 ) → 逐篇下载详情组装完整文档行。
///     政府/官方站点每站结构各异 ( TRS 建站为主但容器/编码不一 ) , 一站一实现 ,
///     由 PolicyJob 遍历源清单 ( 见 PolicySpiderRegistry )。
/// </summary>
public interface IPolicySpider
{
    /// <summary>源标识 , 与 spider_policy_document.from_media 对应</summary>
    FromTypeOfNews FromMedia { get; }

    /// <summary>
    ///     发文机关 ( 官方文件的核心维度 , 如 国家统计局 / 中国证监会 )
    /// </summary>
    string Institution { get; }

    /// <summary>本源的全部栏目 ( 官方站按栏目披露 , 加栏目 = 加行 )</summary>
    IReadOnlyList<NewsColumn> Columns { get; }

    /// <summary>
    ///     拉一页文档存根列表 ( 页码翻页 , 列表从新到旧 ) ;
    ///     存根含 docId / 标题 / 正文页地址 / 日期级时间 , 完整内容由
    ///     <see cref="GetDocumentAsync" /> 二段补齐
    /// </summary>
    Task<PolicyDocumentPage> GetDocumentPage(NewsColumn column, int pageNo);

    /// <summary>
    ///     下载并解析单篇详情 , 组装完整文档行 ( 正文纯文本 / 附件 / 文号 / 精确时间 )
    /// </summary>
    Task<SpiderPolicyDocumentModel> GetDocumentAsync(PolicyDocumentStub stub);
}

/// <summary>
///     文档列表存根 : 列表页可得的字段 ( 详情解析前用于存在性判定 )
/// </summary>
public sealed record PolicyDocumentStub(string DocId, string Title, string Url, DateTime PublishTime);

/// <summary>
///     文档列表页 : 存根 + 是否还有下一页
/// </summary>
public sealed record PolicyDocumentPage(List<PolicyDocumentStub> Items, bool HasMore);
