namespace KSpider.Admin.Api.Contracts;

/// <summary>分页响应。</summary>
/// <param name="Items">当前页数据。</param>
/// <param name="Page">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量。</param>
/// <param name="Total">满足筛选条件的总行数。</param>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

/// <summary>渠道视图。</summary>
/// <param name="Id">渠道主键。</param>
/// <param name="CategoryId">归属分类。</param>
/// <param name="ChannelName">渠道名。</param>
/// <param name="SourceAdapter">列表适配器编码快照。</param>
/// <param name="DetailAdapter">详情适配器编码快照。</param>
/// <param name="WriteMode">入库模式。</param>
/// <param name="RetentionDays">保留天数；null 表示永久。</param>
/// <param name="Status">active / paused / retired。</param>
/// <param name="ScheduleJson">原始 schedule JSON。</param>
/// <param name="ExtraJson">渠道实例参数 JSON。</param>
/// <param name="CreateTime">创建时间。</param>
/// <param name="UpdateTime">更新时间。</param>
public sealed record ChannelView(
    long Id,
    long CategoryId,
    string ChannelName,
    string SourceAdapter,
    string DetailAdapter,
    string WriteMode,
    int? RetentionDays,
    string Status,
    string ScheduleJson,
    string ExtraJson,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>渠道分类视图。</summary>
/// <param name="Id">分类主键。</param>
/// <param name="ParentId">父分类；null 表示根。</param>
/// <param name="Name">分类名。</param>
/// <param name="Path">物化路径。</param>
/// <param name="SortNo">同层排序。</param>
/// <param name="Status">active / disabled。</param>
public sealed record ChannelCategoryView(
    long Id,
    long? ParentId,
    string Name,
    string Path,
    int SortNo,
    string Status);

/// <summary>任务视图。</summary>
/// <param name="Id">任务主键。</param>
/// <param name="NodeId">执行节点。</param>
/// <param name="TaskType">任务类型。</param>
/// <param name="TaskDetailJson">任务包 JSON。</param>
/// <param name="Status">任务状态。</param>
/// <param name="ExtraJson">执行扩展 JSON。</param>
/// <param name="CreateTime">创建时间。</param>
/// <param name="UpdateTime">更新时间（对账判据）。</param>
public sealed record TaskView(
    long Id,
    Guid NodeId,
    string TaskType,
    string TaskDetailJson,
    string Status,
    string ExtraJson,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>节点镜像视图。</summary>
/// <param name="NodeId">节点标识。</param>
/// <param name="NodeType">节点类型。</param>
/// <param name="Status">online / offline。</param>
/// <param name="DeviceJson">节点自报硬件 JSON。</param>
/// <param name="ExtraJson">节点扩展 JSON。</param>
/// <param name="CreateTime">首次注册时间。</param>
/// <param name="UpdateTime">镜像更新时间；心跳不刷新。</param>
public sealed record NodeView(
    Guid NodeId,
    string NodeType,
    string Status,
    string? DeviceJson,
    string ExtraJson,
    DateTime CreateTime,
    DateTime UpdateTime);
