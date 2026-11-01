using KSpider.Data;
using KSpider.Model;
using KSpider.Web.Dto;
using SqlSugar;

namespace KSpider.Web.Query;

/// <summary>
///     网页型新闻查询 ( 只读 ) : spider_news_list 分页筛选 + 详情三表联查
/// </summary>
public class NewsQueryService(Pg pg)
{
    public PageResult<SpiderNewsListModel> List(int page, int pageSize, int? source, int? category, int? status,
        DateTime? start, DateTime? end, string? keyword)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        using var connection = pg.Connection();
        var query = BuildQuery(connection, source, category, status, start, end, keyword);
        var total = query.Count();
        var items = query.OrderByDescending(it => it.NewsTime)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PageResult<SpiderNewsListModel>(total, page, pageSize, items);
    }

    /// <summary>详情 : 列表行 + 原始内容 + 解析详情 + 图片列表</summary>
    public NewsDetailDto? Detail(string newsUrl)
    {
        using var connection = pg.Connection();
        var list = connection.Queryable<SpiderNewsListModel>().First(it => it.NewsUrl == newsUrl);
        if (list == null) return null;
        var origin = connection.Queryable<SpiderNewsContentOriginModel>().First(it => it.NewsUrl == newsUrl);
        var content = connection.Queryable<SpiderNewsContentModel>().First(it => it.NewsUrl == newsUrl);
        var images = connection.Queryable<SpiderNewsImageListModel>().Where(it => it.NewsUrl == newsUrl).ToList();
        return new NewsDetailDto(list, origin, content, images);
    }

    private static ISugarQueryable<SpiderNewsListModel> BuildQuery(SqlSugarClient connection,
        int? source, int? category, int? status, DateTime? start, DateTime? end, string? keyword)
    {
        var query = connection.Queryable<SpiderNewsListModel>();
        if (source != null) query = query.Where(it => it.FromMedia == source);
        if (category != null) query = query.Where(it => it.Category == category);
        if (status != null) query = query.Where(it => it.DownloadStatusCode == status);
        if (start != null) query = query.Where(it => it.NewsTime >= start);
        if (end != null) query = query.Where(it => it.NewsTime < end);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(it => it.NewsTitle != null && it.NewsTitle.Contains(keyword));
        return query;
    }
}

/// <summary>新闻详情 : 三张表 + 图片表按 news_url 联查</summary>
public sealed record NewsDetailDto(
    SpiderNewsListModel List,
    SpiderNewsContentOriginModel? Origin,
    SpiderNewsContentModel? Content,
    List<SpiderNewsImageListModel> Images);
