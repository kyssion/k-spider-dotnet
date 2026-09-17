using KSpider.Model;
using KSpider.Spider.News;

namespace KSpider.Spider.DfNews;

/// <summary>
///     东方财富新闻源 : 把既有的 DfListSpider / DfContentSpider 适配为统一新闻源接口
/// </summary>
public class DfNewsSpider : INewsSpider
{
    // 栏目编号 ( 列表接口 column 号 ) → 栏目资源 , 供 GetListPage 反查
    private static readonly Dictionary<string, DfNewsResource.DfListUrlResource> ColumnResourceMap =
        DfNewsResource.DfListUrlResourceList.ToDictionary(item => item.ListResourceNumber.ToString());

    private readonly DfContentSpider _contentSpider = new();
    private readonly DfListSpider _listSpider = new();

    public FromTypeOfNews FromMedia => FromTypeOfNews.DfMedia;

    public IReadOnlyList<NewsColumn> Columns { get; } = DfNewsResource.DfListUrlResourceList
        .Select(item => new NewsColumn(item.ListResourceNumber.ToString(), item.CategoryInfo.CategoryName))
        .ToList();

    public async Task<List<SpiderNewsListModel>> GetListPage(NewsColumn column, int pageNumber, int pageSize)
    {
        var resourceItem = ColumnResourceMap[column.ColumnId];
        var listInfos =
            await _listSpider.GetDfListInfoByPageUrl(resourceItem, pageNumber, pageSize, DfListOrderType.ByTime);
        return listInfos.Select(item => item.ToSpiderNewListModel()).ToList();
    }

    public async Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem)
    {
        var origin = await _contentSpider.GetDfContentOriginInfoByInterface(newsItem.NewsUrl ?? "");
        return new NewsContentOrigin
        {
            NewsUrl = origin.NewsUrl,
            OriginType = origin.OriginType,
            NewsOriginContent = origin.NewsOriginContent,
            Status = origin.Status,
            Message = origin.Message
        };
    }

    public NewsContentParseResult ParseContent(string originContent, string newsUrl)
    {
        var contentInfo = _contentSpider.GetContentInfoByJson(originContent, newsUrl);
        return new NewsContentParseResult
        {
            Content = contentInfo.ToSpiderNewsContentModel(),
            Images = contentInfo.ImgInfos.Select(item => new SpiderNewsImageListModel
            {
                NewsUrl = item.NewsUrl,
                ImageResourceUrl = item.ResourceUrl,
                ImageName = item.ImgName
            }).ToList()
        };
    }
}
