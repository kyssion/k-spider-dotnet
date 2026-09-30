using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using KSpider.Exceptions;
using KSpider.Model;
using KSpider.Spider.News.Web;
using KSpider.Spider.Verify;
using KSpider.Common;
using KSpider.Common.Html;
using KSpider.Common.Http;
using Microsoft.Extensions.Logging;

namespace KSpider.Spider.News.Web.Nbd;

/// <summary>
///     每日经济新闻（每经网）文章源 ( 网页抓取型三段 ) : 栏目 SSR 列表 → 详情页原始内容 → 解析正文。
///     列表页整页即一批 ( 无翻页 , 每轮重拉由入库去重吸收 , 新浪栏目页同款形态 ) ;
///     详情页正文在 div.u-editor 的 p 段落 , 时间 "yyyy-MM-dd HH:mm:ss"。
/// </summary>
public class NbdArticleSpider : INewsSpider
{
    private static readonly ILogger Log = LogFactory.GetLogger<NbdArticleSpider>();

    /// <summary>栏目 id → 栏目资源 , 供 GetListPage 反查</summary>
    private static readonly Dictionary<string, NbdArticleResource.NbdArticleColumnResource> ColumnResourceMap =
        NbdArticleResource.ArticleColumnResourceList.ToDictionary(item => item.ColumnId);

    public FromTypeOfNews FromMedia => FromTypeOfNews.NbdMedia;

    /// <summary>解析器标识 , 随 origin 行落库 , 重放作业按行路由 ( 见 INewsSpider.ParserCode )</summary>
    public string ParserCode => NbdArticleResource.ParserCode;

    public IReadOnlyList<NewsColumn> Columns { get; } = NbdArticleResource.ArticleColumnResourceList
        .Select(item => new NewsColumn(item.ColumnId, item.ColumnName))
        .ToList();

    public async Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor)
    {
        // 整页即全量 : cursor 恒为 null , 传了也忽略
        var url = string.Format(NbdArticleResource.ListUrlTemplate, column.ColumnId);
        try
        {
            var html = await VerifiedHttp.GetStringAsync(NbdArticleResource.ResourceHost, url);
            return ParseListPage(html, ColumnResourceMap[column.ColumnId].CategoryNumber);
        }
        catch (Exception e)
        {
            var message =
                $"[NbdArticleSpider GetListPage] 拉取文章列表失败 , column : {column.ColumnId} , url : {url} , err : {e}";
            Log.LogError(message);
            throw new HtmlFormException(url, message, e);
        }
    }

    /// <summary>
    ///     解析栏目列表 SSR ( 独立成公开静态方法供离线测试 ) :
    ///     取全部指向 /articles/{date}/{id}.html 的锚点 ( 页面本身即文章列表 ,
    ///     侧栏跳转不是该形态 ) ; 整页即全量 , NextCursor 恒为 null
    /// </summary>
    public static NewsListPage ParseListPage(string html, int categoryNumber)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        if (doc.DocumentNode.SelectNodes("//a") == null)
            throw new HtmlFormException(NbdArticleResource.ListUrlTemplate,
                "[NbdArticleSpider ParseListPage] 页面没有锚点 ( 结构可能已改版 )");

        var items = new List<SpiderNewsListModel>();
        foreach (var anchor in doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'https://www.nbd.com.cn/articles/')]"))
        {
            var href = anchor.Attributes["href"]?.Value ?? "";
            var title = anchor.InnerText?.Trim() ?? "";
            if (title.Length < 6) continue; // 图卡/短链锚点跳过 ( 标题锚点才是列表条目 )
            var item = NbdArticleListItem.FromAnchor(href, title, categoryNumber);
            if (item.ShouldSkip) continue;
            items.Add(item.ToSpiderNewListModel());
        }

        // 锚点去重 ( 同一文章可能在页内出现多次 )
        var deduped = items.GroupBy(item => item.NewsUrl).Select(group => group.First()).ToList();
        return new NewsListPage { Items = deduped, NextCursor = null };
    }

    public async Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var url = newsItem.NewsUrl ?? "";
        var ans = new NewsContentOrigin
        {
            NewsUrl = url,
            OriginType = NewsContentOriginType.Html,
            ParserCode = NbdArticleResource.ParserCode,
            FromMedia = (int)FromTypeOfNews.NbdMedia,
            NewsOriginContent = ""
        };
        try
        {
            // 详情页整页 HTML 作 origin ( 与新浪/同花顺同款口径 : 无结构化详情接口 )
            ans.NewsOriginContent = await VerifiedHttp.GetStringAsync(NbdArticleResource.ResourceHost, url);
            ans.Status = NewsContentOriginStatus.Success;
            return ans;
        }
        catch (Exception e)
        {
            ans.Message = e.ToString();
            ans.Status = NewsContentOriginStatus.Failed;
            Log.LogError("[NbdArticleSpider GetContentOrigin] 下载详情失败 url : {} , err : {}", url, e);
        }

        return ans;
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(originContent);
            // 正文在 g-articl-text ( 站方拼写如此 ) ; u-editor 是页尾空壳 , 不能用
            var contentNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'g-articl-text')]");
            if (contentNode == null)
                throw new DownloadHttpRequestException(newsUrl,
                    "[NbdArticleSpider ParseContent] 详情页缺少正文容器 g-articl-text ( 页面结构可能已改版 )");

            var segments = new List<NewsContentSegment>();
            var current = new StringBuilder();
            foreach (var node in contentNode.SelectNodes(".//p") ?? new HtmlNodeCollection(contentNode))
            {
                // 授权声明块 ( articleCopyright ) 也在该容器外的相邻位置 , 不受影响 ; 跳过空段
                var text = WhitespaceRegex.Replace(node.InnerText ?? "", "");
                if (text == "") continue;
                // 段内图片一并提取 ( p > img 形态 )
                foreach (var image in node.SelectNodes(".//img") ?? new HtmlNodeCollection(node))
                    AddImageSegment(segments, image.Attributes["src"]?.Value);
                segments.Add(new NewsContentSegment
                {
                    TagType = HtmlTagName.P.ToString(),
                    Value = text,
                    ValueType = NewsContentSegment.TextType
                });
                current.Append(text).Append('\n');
            }

            // 标题 : h1 优先 ( title 标签为空 , 实测 )
            var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "";

            var timeText = Regex.Match(originContent, @"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}").Value;
            var newsTime = DateTime.TryParseExact(timeText, "yyyy-MM-dd HH:mm:ss",
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
                    NewsFrom = "每日经济新闻",
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
            throw new HtmlFormException(newsUrl, $"[NbdArticleSpider ParseContent] 解析详情失败 , err : {e}", e);
        }
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
