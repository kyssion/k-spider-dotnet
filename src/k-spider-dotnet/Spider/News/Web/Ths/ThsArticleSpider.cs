using System.Text;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Flash.Ths;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common;
using KSpider.Common.Html;
using KSpider.Common.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Ths;

/// <summary>
///     同花顺财经文章源 ( 网页抓取型三段 ) : 栏目 SSR 列表 → 详情页原始内容 → 解析正文。
///     与同花顺 7x24 快讯 ( Spider/News/Flash/Ths , 列表即全文 ) 是同一网站的两条管线 ,
///     共用 FromTypeOfNews.ThsMedia , 分别注册在 NewsSpiderRegistry 与 FlashNewsSpiderRegistry。
///     列表页为 GBK 编码 SSR ( 编码由 HttpClientTools 注册的代码页提供方处理 ) ,
///     详情页为 UTF-8 SSR , 正文在 div.news-content-parsed 的纯 p 段落。
/// </summary>
public class ThsArticleSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<ThsArticleSpider>();

    /// <summary>栏目代码 → 栏目资源 , 供 GetListPage 反查</summary>
    private static readonly Dictionary<string, ThsArticleResource.ThsArticleColumnResource> ColumnResourceMap =
        ThsArticleResource.ArticleColumnResourceList.ToDictionary(item => item.ColumnCode);

    public FromTypeOfNews FromMedia => FromTypeOfNews.ThsMedia;

    /// <summary>解析器标识 , 随 origin 行落库 , 重放作业按行路由 ( 见 INewsSpider.ParserCode )</summary>
    public string ParserCode => ThsArticleResource.ParserCode;

    public IReadOnlyList<NewsColumn> Columns { get; } = ThsArticleResource.ArticleColumnResourceList
        .Select(item => new NewsColumn(item.ColumnCode, item.ColumnName))
        .ToList();

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        var channel = ColumnResourceMap[column.ColumnId];
        // 页码游标 : 首页为栏目根路径 , 第 n 页为 index_{n}.shtml
        var page = int.TryParse(cursor, out var parsed) && parsed > 1 ? parsed : 1;
        var pagePart = page == 1 ? "" : $"index_{page}.shtml";
        var url = string.Format(ThsArticleResource.ListUrlTemplate, channel.ColumnCode, pagePart);
        try
        {
            var html = await VerifiedHttp.GetStringAsync(ThsArticleResource.ResourceHost, url);
            return ParseListPage(html, channel.CategoryNumber, page);
        }
        catch (Exception e)
        {
            var message =
                $"[ThsArticleSpider GetListPage] 拉取文章列表失败 , column : {column.ColumnId} , page : {page} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析栏目列表 SSR ( 独立成公开静态方法供离线测试 ) :
    ///     只取 div.list-con 主列表区的 arc-title 条目 ( 页面侧栏还有推荐链接 , 不能整页扫 a 标签 ) ;
    ///     每页固定 25 条 , 满页给下一页页码游标
    /// </summary>
    public static NewsListPage ParseListPage(string html, int categoryNumber, int page)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var container = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'list-con')]");
        if (container == null)
            throw new HtmlFormException(ThsArticleResource.ListUrlTemplate,
                "[ThsArticleSpider ParseListPage] 页面缺少 list-con 主列表区 ( 结构可能已改版 )");

        var items = new List<SpiderNewsListModel>();
        foreach (var anchor in container.SelectNodes(".//span[contains(@class,'arc-title')]/a") ?? new HtmlNodeCollection(container))
        {
            var href = anchor.Attributes["href"]?.Value ?? "";
            var title = anchor.InnerText ?? "";
            // 时刻在 arc-title 的直接子 span 里 ( "09月30日 13:10" , 无年份 )
            var timeText = anchor.SelectSingleNode("following-sibling::span[1]")?.InnerText?.Trim() ?? "";
            var item = ThsArticleListItem.FromAnchor(href, title, timeText, categoryNumber);
            if (item.ShouldSkip) continue;
            items.Add(item.ToSpiderNewListModel());
        }

        return new NewsListPage
        {
            Items = items,
            NextCursor = items.Count >= ThsArticleResource.PageArticleCount ? (page + 1).ToString() : null
        };
    }

    public async Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var url = newsItem.NewsUrl ?? "";
        var ans = new NewsContentOrigin
        {
            NewsUrl = url,
            OriginType = NewsContentOriginType.Html,
            ParserCode = ThsArticleResource.ParserCode,
            FromMedia = (int)FromTypeOfNews.ThsMedia,
            NewsOriginContent = ""
        };
        try
        {
            // 详情页整页 HTML 作 origin ( 与新浪同款口径 : 站点无结构化详情接口 )
            ans.NewsOriginContent = await VerifiedHttp.GetStringAsync(ThsArticleResource.ResourceHost, url);
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[ThsArticleSpider GetContentOrigin] 下载详情失败 url : {} , err : {}", url, e);
        }

        return ans;
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(originContent);
            // 精确匹配正文容器 : 外层 news-content article-content 是空壳 ,
            // 段落全在 news-content-parsed 里 ( 实测 )
            var contentNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'news-content-parsed')]");
            if (contentNode == null)
                throw new DownloadHttpRequestException(newsUrl,
                    "[ThsArticleSpider ParseContent] 详情页缺少正文容器 news-content ( 页面结构可能已改版 )");

            // 同花顺正文实测只出现纯 p 段落 ( 段内可有 a/span 内联标签 ) : 取段落文本 ,
            // 未知顶层标签跳过并记日志 ( 与其它源解析器同一策略 )
            var segments = new List<NewsContentSegment>();
            var current = new StringBuilder();
            foreach (var node in contentNode.ChildNodes)
            {
                if (node.NodeType == HtmlNodeType.Comment) continue;
                if (node.NodeType == HtmlNodeType.Text)
                {
                    AddTextSegment(segments, current, node.InnerText);
                    continue;
                }

                var nodeName = node.Name.ToLowerInvariant();
                switch (nodeName)
                {
                    case "p":
                    case "span":
                    case "strong":
                        AddTextSegment(segments, current, node.InnerText);
                        break;
                    case "img":
                        AddImageSegment(segments, node.Attributes["src"]?.Value);
                        break;
                    default:
                        Log.LogWarning(
                            "[ThsArticleSpider ParseContent] unknown html tag skip , tag : {} , url : {}",
                            node.Name, newsUrl);
                        break;
                }
            }

            // 标题 : 详情页 h1 优先 , 缺失回退列表标题无法拿 ( origin 只有详情页 ) , 用 title 标签截前缀
            var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                var pageTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "";
                title = pageTitle.Split('_')[0].Trim();
            }

            // 时间 : 详情页 "yyyy-MM-dd HH:mm" 形态
            var timeText = Regex.Match(originContent, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}").Value;
            var newsTime = DateTime.TryParseExact(timeText, "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : DateTime.MinValue;

            var images = segments
                .Where(segment => segment is { ValueType: NewsContentSegment.ImgType, ResourceUri: not null })
                .Select(segment => segment.ResourceUri!)
                .Select(imageUrl => new SpiderNewsImageListModel
                {
                    NewsUrl = newsUrl,
                    ImageResourceUrl = imageUrl,
                    ImageName = HttpUrlTools.GetUrlLastPath(imageUrl)
                })
                .ToList();

            return new NewsContentParseResult
            {
                Content = new SpiderNewsContentModel
                {
                    NewsUrl = newsUrl,
                    NewsTitle = title,
                    NewsFrom = ThsNewsResource.NewsFromName,
                    NewsTime = newsTime,
                    NewsContentJson = JsonTools.GetJson(segments),
                    NewsContentText = current.ToString()
                },
                Images = images
            };
        }
        catch (DownloadHttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new HtmlFormException(newsUrl, $"[ThsArticleSpider ParseContent] 解析详情失败 , err : {e}", e);
        }
    }

    private static void AddTextSegment(List<NewsContentSegment> segments, StringBuilder current, string innerText)
    {
        var text = WhitespaceRegex.Replace(innerText ?? "", "");
        if (text == "") return;
        segments.Add(new NewsContentSegment
        {
            TagType = HtmlTagName.P.ToString(),
            Value = text,
            ValueType = NewsContentSegment.TextType
        });
        current.Append(text).Append('\n');
    }

    private static void AddImageSegment(List<NewsContentSegment> segments, string? imageUrl)
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

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
}
