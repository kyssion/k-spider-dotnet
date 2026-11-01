namespace KSpider.Web.Dto;

/// <summary>
///     分页信封 : { total, page, pageSize, items } , page 从 1 开始
/// </summary>
public sealed record PageResult<T>(int Total, int Page, int PageSize, List<T> Items);
