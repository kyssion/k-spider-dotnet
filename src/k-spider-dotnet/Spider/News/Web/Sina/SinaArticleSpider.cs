using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common.Html;
using KSpider.Common.Http;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Sina;

/// <summary>
///     新浪财经文章爬虫 ( 网页抓取型三段 ) : 列表 ( 滚动接口或栏目滚动页 ) → 详情页原始 HTML → 解析正文。
///     与 7x24 快讯 ( Spider/News/Flash/Sina , 列表即全文 ) 是同一网站的两条管线 ,
///     共用 FromTypeOfNews.SinaMedia , 详情页统一为 doc-*.shtml 服务端渲染页 ( 正文在 div#artibody )。
/// </summary>
public partial class SinaArticleSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<SinaArticleSpider>();

    /// <summary>
    ///     列表与详情的内联空白清洗 ( 与财联社解析器同款 )
    /// </summary>
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    // 栏目标识 → 栏目资源 , 供 GetListPage 反查
    private static readonly Dictionary<string, SinaArticleResource.SinaArticleColumn> ColumnMap =
        SinaArticleResource.ArticleColumnList.ToDictionary(item => item.ColumnId);

    public FromTypeOfNews FromMedia => FromTypeOfNews.SinaMedia;

    /// <summary>解析器标识 , 随 origin 行落库 , 重放作业按行路由 ( 见 INewsSpider.ParserCode )</summary>
    public string ParserCode => SinaArticleResource.ParserCode;

    public IReadOnlyList<NewsColumn> Columns { get; } = SinaArticleResource.ArticleColumnList
        .Select(item => new NewsColumn(item.ColumnId, item.ColumnName))
        .ToList();

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        var resource = ColumnMap[column.ColumnId];
        try
        {
            // 体系 ① : 滚动接口栏目 , 页码翻页 ( 游标即页码字符串 )
            if (resource.Lid > 0)
            {
                var requestSize = Math.Min(pageSize, SinaArticleResource.MaxPageSize);
                var pageNumber = cursor == null ? 1 : int.Parse(cursor);
                var url = string.Format(SinaArticleResource.RollListUrl, resource.Lid, requestSize, pageNumber);
                var responseString = await VerifiedHttp.GetStringAsync(SinaArticleResource.RollListHost, url);
                return ParseRollListPage(responseString, resource.CategoryNumber, requestSize, pageNumber);
            }

            // 体系 ② : 栏目滚动页 , 整页即完整列表 , 无翻页 ( 游标恒为 null )
            var pageUrl = string.Format(SinaArticleResource.ColumnPageUrlTemplate, resource.PageCid);
            var pageString = await VerifiedHttp.GetStringAsync(SinaArticleResource.ColumnPageHost, pageUrl);
            return ParseColumnPage(pageString, resource.CategoryNumber);
        }
        catch (Exception e)
        {
            var message =
                $"[SinaArticleSpider GetListPage] 拉取文章列表失败 , column : {column.ColumnId} , cursor : {cursor} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(column.ColumnId, message, e);
        }
    }

    /// <summary>
    ///     解析滚动接口响应 ( 独立成公开静态方法供离线测试 ) ;
    ///     返回满页说明后面还有存量 , 短页即末页 ( 与 7x24 快讯源同款页码游标语义 )
    /// </summary>
    public static NewsListPage ParseRollListPage(string responseString, int categoryNumber, int requestSize,
        int pageNumber)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(SinaArticleResource.RollListUrl,
            "[SinaArticleSpider ParseRollListPage] 响应不是合法 JSON");
        if (jsonNode["result"]?["status"]?["code"]?.ToString() != "0")
            throw new HtmlFormException(SinaArticleResource.RollListUrl,
                $"[SinaArticleSpider ParseRollListPage] 接口返回错误 : {jsonNode["result"]?["status"]?["msg"]}");
        if (jsonNode["result"]?["data"] is not JsonArray listData)
            throw new HtmlFormException(SinaArticleResource.RollListUrl,
                "[SinaArticleSpider ParseRollListPage] 响应缺少 result.data");

        var items = new List<SpiderNewsListModel>();
        foreach (var node in listData)
        {
            if (node == null) continue;
            var listItem = SinaRollListItem.FromJson(node, categoryNumber);
            if (listItem.ShouldSkip) continue;
            items.Add(listItem.ToSpiderNewListModel());
        }

        return new NewsListPage
        {
            Items = items,
            NextCursor = items.Count < requestSize ? null : (pageNumber + 1).ToString()
        };
    }

    /// <summary>
    ///     解析栏目滚动页 ( 独立成公开静态方法供离线测试 ) :
    ///     整页即该栏目最新完整列表 ( 活跃栏目约 200 条 , 低频栏目更少 ) , 没有翻页 , NextCursor 恒为 null。
    ///     页面本身按新到旧排序 , 顺序不另做处理
    /// </summary>
    public static NewsListPage ParseColumnPage(string pageString, int categoryNumber)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(pageString);
        var liNodes = htmlDoc.DocumentNode.SelectNodes("//ul[@id='listcontent']/li");
        var items = new List<SpiderNewsListModel>();
        if (liNodes == null)
            throw new HtmlFormException(SinaArticleResource.ColumnPageUrlTemplate,
                "[SinaArticleSpider ParseColumnPage] 页面缺少列表容器 ul#listcontent ( 页面模板可能已改版 )");

        foreach (var liNode in liNodes)
        {
            var listItem = SinaColumnPageItem.FromHtmlNode(liNode, categoryNumber);
            if (listItem.ShouldSkip) continue;
            items.Add(listItem.ToSpiderNewListModel());
        }

        return new NewsListPage { Items = items, NextCursor = null };
    }

    public async Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var url = newsItem.NewsUrl ?? "";
        var ans = new NewsContentOrigin
        {
            NewsUrl = url,
            // 详情页整页即原始内容 ( 站点没有结构化详情接口 , 页面本身就是数据载体 )
            OriginType = NewsContentOriginType.Html,
            ParserCode = SinaArticleResource.ParserCode,
            FromMedia = (int)FromTypeOfNews.SinaMedia,
            NewsOriginContent = ""
        };
        try
        {
            // 条目可能落在 finance 之外的子站 ( news.sina.com.cn 等 ) , host 从 URL 自取
            var host = new Uri(url).Host;
            ans.NewsOriginContent = await VerifiedHttp.GetStringAsync(host, url);
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[SinaArticleSpider GetContentOrigin] 下载详情失败 url : {} , err : {}", url, e);
        }

        return ans;
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(originContent);
            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='artibody']");
            if (bodyNode == null)
                throw new DownloadHttpRequestException(newsUrl,
                    "[SinaArticleSpider ParseContent] 原始内容缺少正文容器 div#artibody");
            var detail = SinaArticleDetailInfo.FromHtmlDocument(htmlDoc);

            var segments = new List<NewsContentSegment>();
            ParseContentSegments(bodyNode, segments, newsUrl);

            // 纯文本聚合 : TEXT / TABLE 片段按行拼接 ( 与东财、财联社同款语义 )
            var current = new StringBuilder();
            foreach (var segment in segments)
                switch (segment.ValueType)
                {
                    case NewsContentSegment.TextType:
                    case NewsContentSegment.TableType:
                        current.Append(segment.Value).Append('\n');
                        break;
                }

            // 图片列表 : 正文片段里的图片按出现顺序
            var images = segments
                .Where(segment => segment is { ValueType: NewsContentSegment.ImgType, ResourceUri: not null })
                .Select(segment => new SpiderNewsImageListModel
                {
                    NewsUrl = newsUrl,
                    ImageResourceUrl = segment.ResourceUri!,
                    ImageName = HttpUrlTools.GetUrlLastPath(segment.ResourceUri!)
                })
                .ToList();

            return new NewsContentParseResult
            {
                Content = detail.ToSpiderNewsContentModel(newsUrl,
                    JsonTools.GetJson(segments), current.ToString()),
                Images = images
            };
        }
        catch (DownloadHttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new HtmlFormException(newsUrl, $"[SinaArticleSpider ParseContent] 解析详情失败 , err : {e}", e);
        }
    }

    /// <summary>
    ///     正文 HTML → 结构化片段。新浪正文实测标签为 p / strong / span / img / a / blockquote / div ,
    ///     其余已知名签按同语义归类 , 未知标签 ( 含广告 ins / style / script ) 跳过片段并记日志 ,
    ///     不让单条未知标签炸掉整篇 ( 与东财、财联社解析器同一策略 )
    /// </summary>
    private static void ParseContentSegments(HtmlNode bodyNode, List<NewsContentSegment> segments, string newsUrl)
    {
        var ti = CultureInfo.CurrentCulture.TextInfo;
        foreach (var node in bodyNode.ChildNodes)
        {
            if (node.NodeType == HtmlNodeType.Comment) continue;

            if (node.NodeType == HtmlNodeType.Text)
            {
                AddTextSegment(segments, node.InnerText, "#text");
                continue;
            }

            // 新浪正文还有引用块与链接两种顶层标签 : 取内联文本 , 不当作未知标签丢弃
            var nodeName = node.Name.ToLowerInvariant();
            if (nodeName is "blockquote" or "a")
            {
                AddTextSegment(segments, node.InnerText, nodeName);
                continue;
            }

            if (!Enum.TryParse<HtmlTagName>(ti.ToTitleCase(node.Name), true, out var tagName) ||
                !Enum.IsDefined(typeof(HtmlTagName), tagName))
            {
                Log.LogWarning("[SinaArticleSpider ParseContentSegments] unknown html tag skip , tag : {} , url : {}",
                    node.Name, newsUrl);
                continue;
            }

            switch (tagName)
            {
                case HtmlTagName.P:
                case HtmlTagName.Span:
                case HtmlTagName.Strong:
                case HtmlTagName.Pre:
                    var image = node.SelectSingleNode(".//img");
                    if (image?.Attributes["src"] != null)
                    {
                        AddImageSegment(segments, image.Attributes["src"].Value);
                        break;
                    }

                    AddTextSegment(segments, node.InnerText, node.Name);
                    break;
                case HtmlTagName.Center:
                    var centerImage = node.SelectSingleNode(".//img");
                    if (centerImage?.Attributes["src"] != null)
                        AddImageSegment(segments, centerImage.Attributes["src"].Value);
                    break;
                case HtmlTagName.H:
                case HtmlTagName.H1:
                case HtmlTagName.H2:
                case HtmlTagName.H3:
                case HtmlTagName.H4:
                case HtmlTagName.H5:
                case HtmlTagName.H6:
                    AddTextSegment(segments, node.InnerText, HtmlTagName.H.ToString());
                    break;
                case HtmlTagName.Ul:
                    var liNodes = node.SelectNodes("li") ?? node.SelectNodes("ul/li");
                    if (liNodes == null || liNodes.Count == 0) break;
                    segments.Add(new NewsContentSegment
                    {
                        TagType = node.Name,
                        Value = JsonTools.GetJson(liNodes.Select(li => li.InnerText).ToList()),
                        ValueType = NewsContentSegment.UlType
                    });
                    break;
                case HtmlTagName.Table:
                    var rows = node.SelectNodes("tbody/tr") ?? node.SelectNodes("tr");
                    if (rows == null) break;
                    var tableData = rows
                        .Select(tr => (tr.SelectNodes("td") ?? tr.SelectNodes("th"))?
                            .Where(td => td.InnerText != null)
                            .Select(td => td.InnerText).ToList() ?? [])
                        .Where(cells => cells.Count != 0)
                        .ToList();
                    if (tableData.Count == 0) break;
                    segments.Add(new NewsContentSegment
                    {
                        TagType = node.Name,
                        Value = JsonTools.GetJson(tableData),
                        ValueType = NewsContentSegment.TableType
                    });
                    break;
                case HtmlTagName.Div:
                case HtmlTagName.Br:
                    // 已知但正文里不当结构用的标签 : 取内联文本兜底 ; div 另承载两种新浪特有形态 ——
                    // 正文图容器 div.img_wrapper ( 图为直下子节点 , 图注在 img_descr 里 ) ,
                    // 文末"新浪财经APP"推广二维码 div.appendQr_wrap ( 每篇固定尾巴 , 整块跳过 )
                    var divClass = node.Attributes["class"]?.Value ?? "";
                    if (divClass.Contains("appendQr", StringComparison.Ordinal)) break;
                    var divImages = node.SelectNodes("img");
                    if (divImages != null)
                        foreach (var divImage in divImages)
                            if (divImage.Attributes["src"] != null)
                                AddImageSegment(segments, divImage.Attributes["src"].Value);
                    AddTextSegment(segments, node.InnerText, node.Name);
                    break;
                default:
                    break;
            }
        }
    }

    private static void AddTextSegment(List<NewsContentSegment> segments, string innerText, string tagType)
    {
        var text = FillWriteLine().Replace(innerText, "");
        if (text == "") return;
        segments.Add(new NewsContentSegment { TagType = tagType, Value = text, ValueType = NewsContentSegment.TextType });
    }

    private static void AddImageSegment(List<NewsContentSegment> segments, string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;
        segments.Add(new NewsContentSegment
        {
            TagType = HtmlTagName.Center.ToString(),
            Value = "",
            ValueType = NewsContentSegment.ImgType,
            ResourceUri = imageUrl
        });
    }
}
