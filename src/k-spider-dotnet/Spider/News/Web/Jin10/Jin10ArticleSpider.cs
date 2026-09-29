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

namespace KSpider.Spider.News.Web.Jin10;

/// <summary>
///     金十数据文章爬虫 ( 网页抓取型三段 ) : 列表 ( reference 接口 , 页码翻页 ) → 详情接口 JSON 原文 → 解析正文片段。
///     与快讯 ( Spider/News/Flash/Jin10 , 列表即全文 ) 是同一网站的两条管线 , 共用 FromTypeOfNews.Jin10Media。
///     列表与详情接口都要带各自的 x-app-id/x-version 头 ( 两套 app-id 不通用 ) ;
///     详情正文为 HTML 片段 ( 实测标签集 p/strong/span/h2/a/img/figure/figcaption/video/blockquote/div )。
/// </summary>
public partial class Jin10ArticleSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<Jin10ArticleSpider>();

    /// <summary>条目规范 URL 里的文章 id : https://xnews.jin10.com/details/231303 末段数字</summary>
    [GeneratedRegex(Jin10ArticleResource.ArticleIdPattern)]
    private static partial Regex ArticleIdRegex();

    /// <summary>正文内联空白清洗 ( 与财联社/新浪/见闻解析器同款 )</summary>
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    // 栏目标识 → 分类号 , 供 GetListPage 反查
    private static readonly Dictionary<string, int> ColumnCategoryMap = Jin10ArticleResource.ArticleColumnList
        .ToDictionary(item => item.ColumnId, item => item.CategoryNumber);

    public FromTypeOfNews FromMedia => FromTypeOfNews.Jin10Media;

    /// <summary>解析器标识 , 随 origin 行落库 , 重放作业按行路由 ( 见 INewsSpider.ParserCode )</summary>
    public string ParserCode => Jin10ArticleResource.ParserCode;

    public IReadOnlyList<NewsColumn> Columns { get; } = Jin10ArticleResource.ArticleColumnList
        .Select(item => new NewsColumn(item.ColumnId, item.ColumnName))
        .ToList();

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        var categoryNumber = ColumnCategoryMap[column.ColumnId];
        try
        {
            var requestSize = Math.Min(pageSize, Jin10ArticleResource.MaxPageSize);
            // 游标即页码字符串 ( 与新浪滚动接口同款语义 ) , 首页为 1
            var pageNumber = cursor == null ? 1 : int.Parse(cursor);
            var url = string.Format(Jin10ArticleResource.ListUrlTemplate, column.ColumnId, pageNumber, requestSize);
            // 传请求工厂而不是请求实例 : 过完验证要重放请求 , 而 HttpRequestMessage 不能重复发送
            var responseString = await VerifiedHttp.SendStringAsync(Jin10ArticleResource.ResourceHost, () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                // 接口必须带客户端标识头 , 否则 502 ( 列表与详情是两套 app-id )
                request.Headers.Add("x-app-id", Jin10ArticleResource.AppId);
                request.Headers.Add("x-version", Jin10ArticleResource.Version);
                return request;
            });
            return ParseListPage(responseString, categoryNumber, requestSize, pageNumber);
        }
        catch (Exception e)
        {
            var message =
                $"[Jin10ArticleSpider GetListPage] 拉取文章列表失败 , column : {column.ColumnId} , cursor : {cursor} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(column.ColumnId, message, e);
        }
    }

    /// <summary>
    ///     解析列表响应 ( 独立成公开静态方法供离线测试 ) ;
    ///     页码翻页、页间零重叠 , 短页即末页。末页判断用原始条数 ( 过滤跳过的 vip/视频条目不参与 ) ,
    ///     避免满页被跳过条目吃掉后误判成末页提前停翻
    /// </summary>
    public static NewsListPage ParseListPage(string responseString, int categoryNumber, int requestSize,
        int pageNumber)
    {
        var jsonNode = JsonNode.Parse(responseString) ??
                       throw new HtmlFormException(Jin10ArticleResource.ListUrlTemplate,
                           "[Jin10ArticleSpider ParseListPage] 响应不是合法 JSON");
        if (jsonNode["status"]?.ToString() != "200")
            throw new HtmlFormException(Jin10ArticleResource.ListUrlTemplate,
                $"[Jin10ArticleSpider ParseListPage] 接口返回错误 : {jsonNode["status"]} {jsonNode["message"]}");
        if (jsonNode["data"]?["list"] is not JsonArray listData)
            throw new HtmlFormException(Jin10ArticleResource.ListUrlTemplate,
                "[Jin10ArticleSpider ParseListPage] 响应缺少 data.list");

        var items = new List<SpiderNewsListModel>();
        foreach (var node in listData)
        {
            if (node == null) continue;
            var listItem = Jin10ArticleListItem.FromJson(node, categoryNumber);
            if (listItem.ShouldSkip) continue;
            items.Add(listItem.ToSpiderNewListModel());
        }

        return new NewsListPage
        {
            Items = items,
            // 短页即末页 ( 越界页码接口返回空列表 ) ; 满页继续翻
            NextCursor = listData.Count < requestSize ? null : (pageNumber + 1).ToString()
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
            ParserCode = Jin10ArticleResource.ParserCode,
            FromMedia = (int)FromTypeOfNews.Jin10Media,
            NewsOriginContent = ""
        };
        try
        {
            var articleId = ArticleIdRegex().Match(url).Groups[1].Value;
            if (articleId.Length == 0)
                throw new DownloadHttpRequestException(url, "[Jin10ArticleSpider GetContentOrigin] URL 缺少文章 id");
            var detailUrl = string.Format(Jin10ArticleResource.DetailUrlTemplate, articleId);
            ans.NewsOriginContent = await VerifiedHttp.SendStringAsync(Jin10ArticleResource.ResourceHost, () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, detailUrl);
                // 详情接口是另一套 app-id ( 与列表不通用 ) , 缺头 502
                request.Headers.Add("x-app-id", Jin10ArticleResource.DetailAppId);
                request.Headers.Add("x-version", Jin10ArticleResource.Version);
                return request;
            });
            ans.IsPaid = ReadIsPaid(ans.NewsOriginContent);
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[Jin10ArticleSpider GetContentOrigin] 下载详情失败 url : {} , err : {}", url, e);
        }

        return ans;
    }

    /// <summary>
    ///     从详情响应 JSON 读付费标记 ( data 的 vip/super_vip/elite_vip 任一非零 ) ;
    ///     解析失败不拦截入库流程 , 按非付费处理
    /// </summary>
    public static bool ReadIsPaid(string detailJson)
    {
        try
        {
            return JsonNode.Parse(detailJson)?["data"] is JsonObject dataNode &&
                   Jin10ArticleListItem.ReadVipFlag(dataNode);
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
                               "[Jin10ArticleSpider ParseContent] 原始内容不是合法 JSON");
            if (jsonNode["status"]?.ToString() != "200")
                throw new DownloadHttpRequestException(newsUrl,
                    $"[Jin10ArticleSpider ParseContent] 详情接口返回错误 : {jsonNode["status"]} {jsonNode["message"]}");
            if (jsonNode["data"] is not JsonObject dataNode)
                throw new DownloadHttpRequestException(newsUrl,
                    "[Jin10ArticleSpider ParseContent] 详情响应缺少 data 对象");

            var detail = Jin10ArticleDetailInfo.FromJson(dataNode);
            var segments = new List<NewsContentSegment>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(detail.Content);
            // content 是 HTML 片段 ( 无统一容器 ) , 节点直接挂在文档根上
            ParseContentSegments(htmlDoc.DocumentNode, segments, newsUrl);

            // 纯文本聚合 : TEXT / TABLE 片段按行拼接 ( 与东财、财联社、新浪、见闻同款语义 )
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
            throw new HtmlFormException(newsUrl, $"[Jin10ArticleSpider ParseContent] 解析详情失败 , err : {e}", e);
        }
    }

    /// <summary>
    ///     正文 HTML → 结构化片段。金十正文实测标签为 p / strong / span / h2 / a / img / figure / figcaption /
    ///     video / blockquote / div , 其余已知名签按同语义归类 , 未知标签跳过片段并记日志 ,
    ///     不让单条未知标签炸掉整篇 ( 与各源解析器同一策略 )
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

            var nodeName = node.Name.ToLowerInvariant();
            // 金十图容器 : figure 直下 img ( 图注在 figcaption ) , 图片与图注各自成段
            if (nodeName is "figure")
            {
                var figureImage = node.SelectSingleNode(".//img");
                if (figureImage?.Attributes["src"] != null)
                    AddImageSegment(segments, figureImage.Attributes["src"].Value);
                var caption = node.SelectSingleNode("figcaption");
                if (caption != null) AddTextSegment(segments, caption.InnerText, "figcaption");
                continue;
            }

            // 裸图注 ( 不在 figure 里 ) : 取文本
            if (nodeName is "figcaption")
            {
                AddTextSegment(segments, node.InnerText, nodeName);
                continue;
            }

            // 内嵌视频 : 正文常见形态 ( 实测约 2 处/篇 ) , 无文本可取 , 静默跳过 ( 不按未知标签告警 )
            if (nodeName is "video") continue;

            // 内容结构段 ( 实测 231212 等文用 section 包正文块 ) : 同 div 处理 , 图片与内联文本各自成段
            if (nodeName is "section")
            {
                var sectionImages = node.SelectNodes(".//img");
                if (sectionImages != null)
                    foreach (var sectionImage in sectionImages)
                        if (sectionImage.Attributes["src"] != null)
                            AddImageSegment(segments, sectionImage.Attributes["src"].Value);
                AddTextSegment(segments, node.InnerText, nodeName);
                continue;
            }

            // 顶层引用块与链接 : 取内联文本 , 不当未知标签丢弃
            if (nodeName is "blockquote" or "a")
            {
                AddTextSegment(segments, node.InnerText, nodeName);
                continue;
            }

            if (!Enum.TryParse<HtmlTagName>(ti.ToTitleCase(node.Name), true, out var tagName) ||
                !Enum.IsDefined(typeof(HtmlTagName), tagName))
            {
                Log.LogWarning("[Jin10ArticleSpider ParseContentSegments] unknown html tag skip , tag : {} , url : {}",
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
