using KSpider.Data;
using KSpider.Model;
using KSpider.Web.Dto;
using SqlSugar;

namespace KSpider.Web.Query;

/// <summary>
///     实时快讯查询 ( 只读 ) : spider_flash_news 分页筛选
/// </summary>
public class FlashQueryService(Pg pg)
{
    public PageResult<SpiderFlashNewsModel> List(int page, int pageSize, int? source, int? category, int? level,
        DateTime? start, DateTime? end, string? keyword)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        using var connection = pg.Connection();
        var query = connection.Queryable<SpiderFlashNewsModel>();
        if (source != null) query = query.Where(it => it.FromMedia == source);
        if (category != null) query = query.Where(it => it.Category == category);
        if (level != null) query = query.Where(it => it.Level == level);
        if (start != null) query = query.Where(it => it.NewsTime >= start);
        if (end != null) query = query.Where(it => it.NewsTime < end);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(it => it.Title != null && it.Title.Contains(keyword));
        var total = query.Count();
        var items = query.OrderByDescending(it => it.NewsTime)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PageResult<SpiderFlashNewsModel>(total, page, pageSize, items);
    }
}
