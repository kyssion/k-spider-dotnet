using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Policy.Csrc;

/// <summary>
///     中国证监会官方文件源 ( Policy 管线 ) : 「要闻」栏目 ( c106311 )——
///     全量列表页 common_xq_list.shtml 约 180 条 SSR 直出 ,
///     详情页正文在 div.detail-news ( 服务端直出 , font/span 混排 ) ,
///     精确时间与来源在 meta 标签 ( PubDate / ContentSource ) ; doc_id 取栏目文档 id ( c{数字} )。
///     旧栏目 c100028 的 common_list.shtml 是陈旧存档 ( 2021 年停更 ) , 不要误接。
/// </summary>
public class CsrcPolicySpider : IPolicySpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<CsrcPolicySpider>();

    public const string ListUrl = "https://www.csrc.gov.cn/csrc/c106311/common_xq_list.shtml";

    public const string ResourceHost = "www.csrc.gov.cn";

    /// <summary>栏目 id ( 详情页路径 /csrc/c106311/{doc_id}/content.shtml 的栏目段 )</summary>
    public const string ColumnCode = "c106311";

    public FromTypeOfNews FromMedia => FromTypeOfNews.CsrcMedia;

    public string Institution => "中国证监会";

    public IReadOnlyList<SourceColumn> Columns { get; } = [new SourceColumn(ColumnCode, "要闻")];

    public async Task<PolicyDocumentPage> GetDocumentPage(SourceColumn column, int pageNo)
    {
        // 全量列表单页即全量 ( 实测约 180 条 ) , page>1 返回空页
        if (pageNo > 1) return new PolicyDocumentPage([], false);
        try
        {
            var html = await VerifiedHttp.GetStringAsync(ResourceHost, ListUrl);
            return ParseListPage(html);
        }
        catch (Exception e)
        {
            var message = $"[CsrcPolicySpider GetDocumentPage] 拉取列表失败 , url : {ListUrl} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(ListUrl, message, e);
        }
    }

    /// <summary>
    ///     解析列表 ( 独立成公开静态方法供离线测试 ) :
    ///     取本栏目 content.shtml 锚点 , 条目旁带日期 ( yyyy-MM-dd ) ; 整页即全量 , HasMore 恒 false
    /// </summary>
    public static PolicyDocumentPage ParseListPage(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var items = new List<PolicyDocumentStub>();
        foreach (var anchor in doc.DocumentNode.SelectNodes("//a[contains(@href,'content.shtml')]") ??
                                 new HtmlNodeCollection(doc.DocumentNode))
        {
            var href = anchor.Attributes["href"]?.Value ?? "";
            // 收会管口径的 /csrc/ 路径 ( 排除列表页混入的地方证监局 /jilin/ 等辖区站点链接 ) ;
            // 文档 id 全局递增 , 取路径末段 ( 兼容 c{数字} 与 GUID 两形态 , 政策文件类是 GUID )
            var match = DocIdRegex.Match(href);
            if (!match.Success) continue;
            var docId = match.Groups["docid"].Value;
            var title = PolicyContentExtractor.Clean(anchor.InnerText);
            if (title.Length < 6) continue;

            // 条目旁日期 : 同一父块内第一个 yyyy-MM-dd
            var context = anchor.ParentNode?.ParentNode?.InnerText ?? anchor.InnerText ?? "";
            var dateText = DateRegex.Match(context).Value;
            if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishTime))
                continue;

            items.Add(new PolicyDocumentStub(docId, title,
                $"https://www.csrc.gov.cn/csrc/{ColumnCode}/{docId}/content.shtml", publishTime));
        }

        var deduped = items.GroupBy(item => item.DocId).Select(group => group.First()).ToList();
        return new PolicyDocumentPage(deduped, false);
    }

    public async Task<SpiderPolicyDocumentModel> GetDocumentAsync(PolicyDocumentStub stub)
    {
        var raw = await VerifiedHttp.GetStringAsync(ResourceHost, stub.Url);
        return ParseDocument(raw, stub, FromMedia, Institution);
    }

    /// <summary>
    ///     解析详情 ( 独立成公开静态方法供离线测试 ) :
    ///     detail-news 容器文本化 , 精确时间与发文机关取 meta 标签 ( TRS 动态渲染站点的元数据形态 )
    /// </summary>
    public static SpiderPolicyDocumentModel ParseDocument(string raw, PolicyDocumentStub stub,
        FromTypeOfNews fromMedia, string fallbackInstitution)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(raw);

        var contentNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'detail-news')]");
        if (contentNode == null)
            throw new HtmlFormException(stub.Url,
                "[CsrcPolicySpider GetDocumentAsync] 详情页缺少 detail-news 正文容器 ( 页面结构可能已改版 )");

        // 精确时间与来源在 meta 标签 ( TRS 动态渲染站点的元数据承载形态 )
        var timeText = MetaContent(doc, "PubDate");
        var publishTime = DateTime.TryParseExact(timeText, "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : stub.PublishTime;

        return new SpiderPolicyDocumentModel
        {
            FromMedia = (int)fromMedia,
            DocId = stub.DocId,
            Title = stub.Title,
            Institution = MetaContent(doc, "ContentSource") is { Length: > 0 } source ? source : fallbackInstitution,
            DocumentNumber = PolicyContentExtractor.ExtractDocumentNumber(stub.Title, ""),
            PublishTime = DateTime.SpecifyKind(publishTime, DateTimeKind.Unspecified),
            Url = stub.Url,
            ContentText = PolicyContentExtractor.ExtractText(contentNode),
            Attachments = null,
            RawContent = raw
        };
    }

    /// <summary>meta 元数据取值 ( ArticleTitle / PubDate / ContentSource )</summary>
    private static string MetaContent(HtmlDocument doc, string name)
    {
        return doc.DocumentNode.SelectSingleNode($"//meta[@name='{name}']")?.Attributes["content"]?.Value?.Trim() ?? "";
    }

    /// <summary>
    ///     会管文档链接 : /csrc/{归档栏目}/{doc_id}/content.shtml——列表页聚合多栏目最新内容 ,
    ///     归档栏目路径不进去重键 ( doc_id 全局递增唯一 ) ;
    ///     doc_id 两形态 : 要闻类 c{数字} , 政策文件类 GUID
    /// </summary>
    private static readonly Regex DocIdRegex =
        new(@"/csrc/[^/ ""]+/(?<docid>(?:c\d+|[0-9a-f]{16,32}))/content\.shtml", RegexOptions.Compiled);

    private static readonly Regex DateRegex = new(@"20\d{2}-\d{2}-\d{2}", RegexOptions.Compiled);
}
