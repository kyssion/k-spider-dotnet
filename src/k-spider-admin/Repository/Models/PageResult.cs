namespace KSpider.Admin.Repository.Models;

/// <summary>
///     仓储分页结果。
/// </summary>
/// <param name="Items">当前页数据。</param>
/// <param name="Page">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
/// <param name="Total">满足条件的总行数。</param>
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
