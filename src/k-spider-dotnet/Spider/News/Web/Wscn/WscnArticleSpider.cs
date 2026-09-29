using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Common;
using KSpider.Common.Html;
using KSpider.Common.Http;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Wscn;

/// <summary>
///     华尔街见闻文章爬虫 ( 网页抓取型三段 ) : 列表 ( 全量文章流 , 游标翻页 ) → 详情接口 JSON 原文 → 解析正文片段。
///     与 live 快讯 ( Spider/News/Flash/Wscn , 列表即全文 ) 是同一网站的两条管线 , 共用 FromTypeOfNews.WscnMedia。
///     列表与详情都是免签 JSON 接口 , 详情正文为 HTML 片段 ( 标签集实测 p/h2/img/blockquote/strong/span/div )。
/// </summary>
public partial class WscnArticleSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<WscnArticleSpider>();

    /// <summary>条目 uri 里的文章 id : https://wallstreetcn.com/articles/3782716 末段数字</summary>
    [GeneratedRegex(WscnArticleResource.ArticleIdPattern)]
    private static partial Regex ArticleIdRegex();

    /// <summary>正文内联空白清洗 ( 与财联社/新浪解析器同款 )</summary>
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    public FromTypeOfNews FromMedia => FromTypeOfNews.WscnMedia;

    // 单栏目全量流 : 见 WscnArticleResource.ColumnId 注释 ( 不按类别建多栏目 , 分类号逐条推断 )
    public IReadOnlyList<NewsColumn> Columns { get; } =
        [new(WscnArticleResource.ColumnId, WscnArticleResource.ColumnName)];

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        try
        {
            var requestSize = Math.Min(pageSize, WscnArticleResource.MaxPageSize);
            var url = $"{WscnArticleResource.ArticlesListUrl}?limit={requestSize}";
            if (!string.IsNullOrEmpty(cursor)) url += $"&cursor={cursor}";
            var responseString = await VerifiedHttp.GetStringAsync(WscnArticleResource.ResourceHost, url);
            return ParseListPage(responseString, requestSize);
        }
        catch (Exception e)
        {
            var message =
                $"[WscnArticleSpider GetListPage] 拉取文章列表失败 , column : {column.ColumnId} , cursor : {cursor} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(column.ColumnId, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ;
    ///     游标为接口给的 next_cursor ( "最新时间,最老时间" 对 , 透传即可 ) , 单调向旧、页间零重叠 ,
    ///     短页即末页。data 为空字符串是 limit 超上限的静默表现 ( 见 MaxPageSize ) , 与正常空页区分报错
    /// </summary>
    public static NewsListPage ParseListPage(string responseString, int requestSize)
    {
        var jsonNode = JsonNode.Parse(responseString) ??
                       throw new HtmlFormException(WscnArticleResource.ArticlesListUrl,
                           "[WscnArticleSpider ParseListPage] 响应不是合法 JSON");
        if (jsonNode["code"]?.ToString() != "20000")
            throw new HtmlFormException(WscnArticleResource.ArticlesListUrl,
                $"[WscnArticleSpider ParseListPage] 接口返回错误 : {jsonNode["message"]}");
        if (jsonNode["data"] is not JsonObject dataNode)
            throw new HtmlFormException(WscnArticleResource.ArticlesListUrl,
                "[WscnArticleSpider ParseListPage] 响应缺少 data 对象 ( limit 超过 30 时接口静默返回空字符串 )");

        var items = new List<SpiderNewsListModel>();
        if (dataNode["items"] is JsonArray listData)
            foreach (var node in listData)
            {
                if (node == null) continue;
                var listItem = WscnArticleListItem.FromJson(node);
                if (listItem.ShouldSkip) continue;
                items.Add(listItem.ToSpiderNewListModel());
            }

        var nextCursor = dataNode["next_cursor"]?.ToString();
        return new NewsListPage
        {
            Items = items,
            // 短页即末页 ; 满页时透传接口的 next_cursor ( 空串视为无更多 )
            NextCursor = items.Count < requestSize || string.IsNullOrEmpty(nextCursor) ? null : nextCursor
        };
    }

    public async Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var url = newsItem.NewsUrl ?? "";
        var ans = new NewsContentOrigin
        {
            NewsUrl = url,
            // 详情接口 JSON 原文即原始内容 ( 正文在其 data.content 字段 , 解析阶段再展开 )
            OriginType = NewsContentOriginType.Json,
            NewsOriginContent = ""
        };
        try
        {
            var articleId = ArticleIdRegex().Match(url).Groups[1].Value;
            if (articleId.Length == 0)
                throw new DownloadHttpRequestException(url, "[WscnArticleSpider GetContentOrigin] URL 缺少文章 id");
            var detailUrl = string.Format(WscnArticleResource.ArticleDetailUrlTemplate, articleId);
            ans.NewsOriginContent = await VerifiedHttp.GetStringAsync(WscnArticleResource.ResourceHost, detailUrl);
            ans.IsPaid = ReadIsPaid(ans.NewsOriginContent);
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[WscnArticleSpider GetContentOrigin] 下载详情失败 url : {} , err : {}", url, e);
        }

        return ans;
    }

    /// <summary>
    ///     从详情响应 JSON 读付费标记 ( data.is_priced ) ; 解析失败不拦截入库流程 , 按非付费处理
    /// </summary>
    public static bool ReadIsPaid(string detailJson)
    {
        try
        {
            return JsonNode.Parse(detailJson)?["data"]?["is_priced"]?.GetValue<bool>() ?? false;
        }
        catch
        {
            return false;
        }
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        try
        {
            var jsonNode = JsonNode.Parse(originContent) ??
                           throw new DownloadHttpRequestException(newsUrl,
                               "[WscnArticleSpider ParseContent] 原始内容不是合法 JSON");
            if (jsonNode["code"]?.ToString() != "20000")
                throw new DownloadHttpRequestException(newsUrl,
                    $"[WscnArticleSpider ParseContent] 详情接口返回错误 : {jsonNode["message"]}");
            if (jsonNode["data"] is not JsonObject dataNode)
                throw new DownloadHttpRequestException(newsUrl,
                    "[WscnArticleSpider ParseContent] 详情响应缺少 data 对象");

            var detail = WscnArticleDetailInfo.FromJson(dataNode);
            var segments = new List<NewsContentSegment>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(detail.Content);
            // content 是 HTML 片段 ( 无统一容器 ) , 节点直接挂在文档根上
            ParseContentSegments(htmlDoc.DocumentNode, segments, newsUrl);

            // 纯文本聚合 : TEXT / TABLE 片段按行拼接 ( 与东财、财联社、新浪同款语义 )
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
            throw new HtmlFormException(newsUrl, $"[WscnArticleSpider ParseContent] 解析详情失败 , err : {e}", e);
        }
    }

    /// <summary>
    ///     正文 HTML → 结构化片段。见闻正文实测标签为 p / h2 / img / strong / span / blockquote / div ,
    ///     其余已知名签按同语义归类 , 未知标签跳过片段并记日志 , 不让单条未知标签炸掉整篇 ( 与各源解析器同一策略 )
    /// </summary>
    private static void ParseContentSegments(HtmlNode containerNode, List<NewsContentSegment> segments, string newsUrl)
    {
        var ti = CultureInfo.CurrentCulture.TextInfo;
        foreach (var node in containerNode.ChildNodes)
        {
            if (node.NodeType == HtmlNodeType.Comment) continue;

            if (node.NodeType == HtmlNodeType.Text)
            {
                AddTextSegment(segments, node.InnerText, "#text");
                continue;
            }

            // 顶层引用块与链接 : 取内联文本 , 不当未知标签丢弃
            var nodeName = node.Name.ToLowerInvariant();
            if (nodeName is "blockquote" or "a")
            {
                AddTextSegment(segments, node.InnerText, nodeName);
                continue;
            }

            if (!Enum.TryParse<HtmlTagName>(ti.ToTitleCase(node.Name), true, out var tagName) ||
                !Enum.IsDefined(typeof(HtmlTagName), tagName))
            {
                Log.LogWarning("[WscnArticleSpider ParseContentSegments] unknown html tag skip , tag : {} , url : {}",
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
                    // 已知但正文里不当结构用的标签 : 取内联文本兜底 ; div 内的图片按直下子节点提取
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
