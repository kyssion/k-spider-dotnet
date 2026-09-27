using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Flash.Cls;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common.Html;
using KSpider.Common.Http;
using KSpider.Common;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Cls;

/// <summary>
///     财联社文章频道爬虫 ( 网页抓取型三段 ) : 频道列表 → 详情页原始内容 → 解析正文。
///     与电报快讯 ( Spider/News/Flash/Cls , 列表即全文 ) 是同一网站的两条管线 ,
///     共用 FromTypeOfNews.ClsMedia , 分别注册在 NewsSpiderRegistry 与 FlashNewsSpiderRegistry。
///     详情页为服务端渲染 , 正文 HTML 内嵌在 __NEXT_DATA__ 里 , 无需浏览器渲染。
/// </summary>
public partial class ClsArticleSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<ClsArticleSpider>();

    /// <summary>
    ///     列表按 SortScore 编辑混排 , 两页之间会有重叠 , 边界去重交给入库去重吸收
    /// </summary>
    [GeneratedRegex(@"\s")]
    private static partial Regex FillWriteLine();

    // 频道 id → 频道资源 , 供 GetListPage 反查
    private static readonly Dictionary<string, ClsArticleResource.ArticleChannelResource> ChannelResourceMap =
        ClsArticleResource.ArticleChannelResourceList.ToDictionary(item => item.ChannelId.ToString());

    public FromTypeOfNews FromMedia => FromTypeOfNews.ClsMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = ClsArticleResource.ArticleChannelResourceList
        .Select(item => new NewsColumn(item.ChannelId.ToString(), item.ChannelName))
        .ToList();

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        var channel = ChannelResourceMap[column.ColumnId];
        // 游标即上一页最老一条的 ctime ( 站点前端同款用法 ) , 首页传 0
        var lastTime = string.IsNullOrEmpty(cursor) ? "0" : cursor;
        var parameters = new Dictionary<string, string>
        {
            ["id"] = channel.ChannelId.ToString(),
            ["last_time"] = lastTime,
            ["rn"] = ClsArticleResource.RequestPageSize.ToString()
        };
        var queryString = ClsSignature.BuildQueryString(parameters);
        var url = $"{string.Format(ClsArticleResource.DepthListUrl, channel.ChannelId)}?{queryString}&sign={ClsSignature.Sign(queryString)}";
        try
        {
            var responseString = await VerifiedHttp.GetStringAsync(ClsArticleResource.ResourceHost, url);
            return ParseListPage(responseString, channel.CategoryNumber);
        }
        catch (Exception e)
        {
            var message =
                $"[ClsArticleSpider GetListPage] 拉取频道文章列表失败 , channel : {column.ColumnId} , last_time : {lastTime} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析频道列表响应 ( 独立成公开静态方法供离线测试 ) :
    ///     跳过广告 ( is_ad ) 与站外跳转 ( external_link 非空 , 没有 /detail/{id} 详情页 ) 条目 ;
    ///     NextCursor 取本页最老一条 ctime , 空页即末页 ( 服务端不按 rn 裁剪 , 短页判断不可用 )
    /// </summary>
    public static NewsListPage ParseListPage(string responseString, int categoryNumber)
    {
        var jsonNode = JsonNode.Parse(responseString) ?? throw new HtmlFormException(ClsArticleResource.DepthListUrl,
            "[ClsArticleSpider ParseListPage] 响应不是合法 JSON");
        var errno = jsonNode["errno"]?.ToString();
        if (errno != "0")
            throw new HtmlFormException(ClsArticleResource.DepthListUrl,
                $"[ClsArticleSpider ParseListPage] 接口返回错误 errno : {errno} , msg : {jsonNode["msg"]}");
        if (jsonNode["data"] is not JsonArray listData)
            throw new HtmlFormException(ClsArticleResource.DepthListUrl,
                "[ClsArticleSpider ParseListPage] 响应缺少 data");

        var items = new List<SpiderNewsListModel>();
        var oldestCtime = long.MaxValue;
        foreach (var node in listData)
        {
            if (node == null) continue;
            var listItem = ClsArticleListItem.FromJson(node, categoryNumber);
            if (listItem.ShouldSkip) continue;
            items.Add(listItem.ToSpiderNewListModel());
            oldestCtime = Math.Min(oldestCtime, listItem.Ctime);
        }

        return new NewsListPage
        {
            Items = items,
            NextCursor = items.Count == 0 || oldestCtime == long.MaxValue ? null : oldestCtime.ToString()
        };
    }

    public async Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var articleId = HttpUrlTools.GetUrlLastPath(newsItem.NewsUrl ?? "");
        var url = string.Format(ClsArticleResource.DetailUrlTemplate, articleId);
        var ans = new NewsContentOrigin
        {
            NewsUrl = newsItem.NewsUrl ?? "",
            OriginType = NewsContentOriginType.Json,
            NewsOriginContent = ""
        };
        try
        {
            var html = await VerifiedHttp.GetStringAsync(ClsArticleResource.ResourceHost, url);
            // 只存 __NEXT_DATA__ 载荷 ( 页面本身是服务端渲染 , 数据全在里面 ) , 解析按纯 JSON 重跑
            ans.NewsOriginContent = ExtractNextDataJson(html);
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[ClsArticleSpider GetContentOrigin] 下载详情失败 url : {} , err : {}", url, e);
        }

        return ans;
    }

    /// <summary>
    ///     从详情页 HTML 提取 __NEXT_DATA__ 的 JSON 载荷 ( 独立成公开静态方法供离线测试 )
    /// </summary>
    public static string ExtractNextDataJson(string html)
    {
        const string marker = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            throw new HtmlFormException(ClsArticleResource.DetailUrlTemplate,
                "[ClsArticleSpider ExtractNextDataJson] 响应中找不到 __NEXT_DATA__ ( 页面模板可能已改版 )");
        start += marker.Length;
        var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        if (end < 0)
            throw new HtmlFormException(ClsArticleResource.DetailUrlTemplate,
                "[ClsArticleSpider ExtractNextDataJson] __NEXT_DATA__ 未正常闭合");
        return html[start..end].Trim();
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        try
        {
            var root = JsonNode.Parse(originContent);
            var articleNode = root?["props"]?["pageProps"]?["articleDetail"];
            if (articleNode == null)
                throw new DownloadHttpRequestException(newsUrl, "[ClsArticleSpider ParseContent] 原始内容缺少 articleDetail");
            var detail = ClsArticleDetailInfo.FromJson(articleNode);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(detail.Content);
            var segments = new List<NewsContentSegment>();
            ParseContentSegments(htmlDoc.DocumentNode, segments, newsUrl);

            // 纯文本聚合 : TEXT / TABLE 片段按行拼接 ( 与东财同款语义 )
            var current = new StringBuilder();
            foreach (var segment in segments)
                switch (segment.ValueType)
                {
                    case NewsContentSegment.TextType:
                    case NewsContentSegment.TableType:
                        current.Append(segment.Value).Append('\n');
                        break;
                }

            // 图片列表 : 正文图 + 封面图 ( 封面不在正文里时补上 ) , 保持出现顺序
            var imageUrls = segments
                .Where(segment => segment is { ValueType: NewsContentSegment.ImgType, ResourceUri: not null })
                .Select(segment => segment.ResourceUri!)
                .ToList();
            foreach (var cover in detail.Images)
                if (!imageUrls.Contains(cover))
                    imageUrls.Add(cover);
            var images = imageUrls.Select(imageUrl => new SpiderNewsImageListModel
            {
                NewsUrl = newsUrl,
                ImageResourceUrl = imageUrl,
                ImageName = HttpUrlTools.GetUrlLastPath(imageUrl)
            }).ToList();

            return new NewsContentParseResult
            {
                Content = detail.ToSpiderNewsContentModel(newsUrl, segments, current.ToString()),
                Images = images
            };
        }
        catch (DownloadHttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new HtmlFormException(newsUrl, $"[ClsArticleSpider ParseContent] 解析详情失败 , err : {e}", e);
        }
    }

    /// <summary>
    ///     正文 HTML → 结构化片段。财联社详情正文标签很简单 ( 实测 p / strong / img / h ) ,
    ///     其余已知名签按同语义归类 , 未知标签跳过片段并记日志 ( 防源站改版导致整篇失败 , 与东财解析器同一策略 )
    /// </summary>
    private static void ParseContentSegments(HtmlNode htmlDoc, List<NewsContentSegment> segments, string newsUrl)
    {
        var ti = CultureInfo.CurrentCulture.TextInfo;
        foreach (var node in htmlDoc.ChildNodes)
        {
            if (node.NodeType == HtmlNodeType.Comment) continue;

            if (node.NodeType == HtmlNodeType.Text)
            {
                AddTextSegment(segments, node.InnerText, "#text");
                continue;
            }

            // 财联社正文实测还有引用块与链接两种顶层标签 ( 东财枚举之外 ) : 取内联文本 , 不当作未知标签丢弃
            var nodeName = node.Name.ToLowerInvariant();
            if (nodeName is "blockquote" or "a")
            {
                AddTextSegment(segments, node.InnerText, nodeName);
                continue;
            }

            if (!Enum.TryParse<HtmlTagName>(ti.ToTitleCase(node.Name), true, out var tagName) ||
                !Enum.IsDefined(typeof(HtmlTagName), tagName))
            {
                Log.LogWarning("[ClsArticleSpider ParseContentSegments] unknown html tag skip , tag : {} , url : {}",
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
                    if (centerImage?.Attributes["src"] != null) AddImageSegment(segments, centerImage.Attributes["src"].Value);
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
                    // 已知但正文里不当结构用的标签 : 取内联文本兜底
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
