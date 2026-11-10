namespace KSpider.Admin.Repository.Models;

/// <summary>
///     channel 行快照。
/// </summary>
/// <param name="Id">渠道主键。</param>
/// <param name="CategoryId">归属分类。</param>
/// <param name="ChannelName">渠道名。</param>
/// <param name="SourceAdapter">列表适配器编码。</param>
/// <param name="DetailAdapter">详情适配器编码。</param>
/// <param name="WriteMode">overwrite / skip / append。</param>
/// <param name="RetentionDays">保留天数；null 为永久。</param>
/// <param name="ScheduleJson">schedule JSON 文本。</param>
/// <param name="Status">active / paused / retired。</param>
/// <param name="ExtraJson">渠道实例参数 JSON 文本。</param>
/// <param name="CreateTime">创建时间。</param>
/// <param name="UpdateTime">更新时间。</param>
public sealed record ChannelRecord(
    long Id,
    long CategoryId,
    string ChannelName,
    string SourceAdapter,
    string DetailAdapter,
    string WriteMode,
    int? RetentionDays,
    string ScheduleJson,
    string Status,
    string ExtraJson,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>
///     channel_category 行快照。
/// </summary>
/// <param name="Id">分类主键。</param>
/// <param name="ParentId">父分类；null 为根。</param>
/// <param name="Name">分类名。</param>
/// <param name="Path">物化路径。</param>
/// <param name="SortNo">同层排序。</param>
/// <param name="Status">active / disabled。</param>
public sealed record ChannelCategoryRecord(
    long Id,
    long? ParentId,
    string Name,
    string Path,
    int SortNo,
    string Status);

/// <summary>
///     渠道查询条件。
/// </summary>
/// <param name="Page">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量，仓储会钳制到 1–100。</param>
/// <param name="CategoryId">可选分类筛选。</param>
/// <param name="Status">可选状态筛选。</param>
public sealed record ChannelQuery(
    int Page,
    int PageSize,
    long? CategoryId = null,
    string? Status = null);
