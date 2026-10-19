using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.Verify;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.Policy.Stats;

/// <summary>
///     国家统计局官方文件源 ( Policy 管线 ) : 「最新发布」栏目——统计数据发布与政策解读,
///     SSR 列表整页即全量 ( 无分页 , 约 30 条 , 含轮播重复由解析去重 ) ,
///     详情页正文在 div.trs_editor_view ( TRS 建站 ) , 附件为相对路径的数据表 xls ;
///     条目时间"yyyy/MM/dd HH:mm" ; doc_id 取列表路径 ( {月份段}/t{日期}_{序号} )。
/// </summary>
public class StatsPolicySpider : IPolicySpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<StatsPolicySpider>();

    public const string ListUrl = "https://www.stats.gov.cn/sj/zxfb/";

    public const string ResourceHost = "www.stats.gov.cn";

    public FromTypeOfNews FromMedia => FromTypeOfNews.StatsMedia;

    public string Institution => "国家统计局";

    public IReadOnlyList<SourceColumn> Columns { get; } = [new SourceColumn("zxfb", "最新发布")];

    public async Task<PolicyDocumentPage> GetDocumentPage(SourceColumn column, int pageNo)
    {
        // 单页即全量 ( 实测无分页链接 ) , page>1 返回空页
        if (pageNo > 1) return new PolicyDocumentPage([], false);
        try
        {
            var html = await VerifiedHttp.GetStringAsync(ResourceHost, ListUrl);
            return ParseListPage(html);
        }
        catch (Exception e)
        {
            var message = $"[StatsPolicySpider GetDocumentPage] 拉取列表失败 , url : {ListUrl} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(ListUrl, message, e);
        }
    }

    /// <summary>
    ///     解析列表 ( 独立成公开静态方法供离线测试 ) :
    ///     取 /sj/zxfb/ 相对链接条目 , 同条目在轮播与列表区重复出现 , 按 doc_id 去重 ;
    ///     整页即全量 , HasMore 恒 false
    /// </summary>
    public static PolicyDocumentPage ParseListPage(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var items = new List<PolicyDocumentStub>();
        foreach (var anchor in doc.DocumentNode.SelectNodes("//a[contains(@href,'/t') and contains(@href,'.html')]") ??
                                 new HtmlNodeCollection(doc.DocumentNode))
        {
            var href = anchor.Attributes["href"]?.Value ?? "";
            var match = DocIdRegex.Match(href);
            var title = PolicyContentExtractor.Clean(anchor.InnerText);
            if (!match.Success || title.Length < 6) continue;

            var docId = match.Groups["docid"].Value;
            // doc_id 自带日期 ( 202609/t20260930_... : 月份段/t+日期_序号 ) ,
            // 列表级时间取日期当日 00:00 , 详情补时刻
            var datePart = docId.Split('/')[1].TrimStart('t');
            if (!DateTime.TryParseExact(datePart[..4] + "-" + datePart[4..6] + "-" + datePart[6..8],
                    "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var publishTime))
                continue;

            items.Add(new PolicyDocumentStub(docId, title,
                $"https://www.stats.gov.cn/sj/zxfb/{docId}.html", publishTime));
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
    ///     正文容器段落+表格文本化 , 附件相对链接拼全地址 , 文号从标题提取
    /// </summary>
    public static SpiderPolicyDocumentModel ParseDocument(string raw, PolicyDocumentStub stub,
        FromTypeOfNews fromMedia, string institution)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(raw);

        var contentNode = doc.DocumentNode.SelectSingleNode(
            "//div[contains(@class,'trs_editor_view') or contains(@class,'TRS_UEDITOR')]");
        if (contentNode == null)
            throw new HtmlFormException(stub.Url,
                "[StatsPolicySpider GetDocumentAsync] 详情页缺少 trs_editor_view 正文容器 ( 页面结构可能已改版 )");

        // 附件 : 详情页目录下的相对链接 ( ./P{...}.xls 数据表 )
        var attachments = (doc.DocumentNode.SelectNodes(
                "//a[contains(@href,'./P') or contains(@href,'/P')]") ?? new HtmlNodeCollection(doc.DocumentNode))
            .Select(anchor => anchor.Attributes["href"]?.Value ?? "")
            .Where(href => AttachmentRegex.IsMatch(href))
            .Select(href => $"https://www.stats.gov.cn/sj/zxfb/{stub.DocId.Split('/')[0]}/" +
                            href.Replace("./", ""))
            .Distinct()
            .ToList();

        var timeText = TimeRegex.Match(raw).Value;
        var publishTime = DateTime.TryParseExact(timeText, "yyyy/MM/dd HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : stub.PublishTime;

        return new SpiderPolicyDocumentModel
        {
            FromMedia = (int)fromMedia,
            DocId = stub.DocId,
            Title = stub.Title,
            Institution = institution,
            DocumentNumber = PolicyContentExtractor.ExtractDocumentNumber(stub.Title, ""),
            PublishTime = DateTime.SpecifyKind(publishTime, DateTimeKind.Unspecified),
            Url = stub.Url,
            ContentText = PolicyContentExtractor.ExtractText(contentNode),
            Attachments = attachments.Count > 0 ? JsonSerializer.Serialize(attachments) : null,
            RawContent = raw
        };
    }

    /// <summary>列表相对链接 : ./202609/t20260930_1965449.html ( 相对 /sj/zxfb/ 目录 )</summary>
    private static readonly Regex DocIdRegex =
        new(@"\.?/?(?<docid>\d{6}/t\d{8}_\d+)\.html", RegexOptions.Compiled);

    private static readonly Regex AttachmentRegex = new(@"\./?P\d+\.(?:xls|xlsx|pdf|docx?)", RegexOptions.Compiled);

    private static readonly Regex TimeRegex = new(@"\d{4}/\d{2}/\d{2} \d{2}:\d{2}", RegexOptions.Compiled);
}
