namespace KSpider.Admin.Repository.Models;

/// <summary>
///     data_source 只读快照；列表查询 Raw 为 null，详情查询才填充原始载荷。
/// </summary>
/// <param name="Id">source 主键。</param>
/// <param name="ChannelId">渠道主键。</param>
/// <param name="ChannelName">渠道名（联表）。</param>
/// <param name="Adapter">列表适配器编码快照。</param>
/// <param name="RecordKey">资源唯一键。</param>
/// <param name="DataStatus">数据事实。</param>
/// <param name="ExecStatus">执行进度。</param>
/// <param name="RawSize">原始载荷字节数。</param>
/// <param name="Raw">原始载荷；列表查询为 null。</param>
/// <param name="CreateTime">创建时间（UTC）。</param>
/// <param name="UpdateTime">更新时间（UTC）。</param>
public sealed record RawDataSourceRecord(
    long Id,
    long ChannelId,
    string ChannelName,
    string Adapter,
    string RecordKey,
    string DataStatus,
    string ExecStatus,
    int RawSize,
    byte[]? Raw,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>
///     data_detail 只读快照；列表查询 Raw 为 null，详情查询才填充原始载荷。
/// </summary>
/// <param name="Id">detail 主键。</param>
/// <param name="SourceId">来源 source。</param>
/// <param name="FetchUrl">实际请求地址快照。</param>
/// <param name="Adapter">详情适配器编码快照。</param>
/// <param name="RecordKey">内容唯一键。</param>
/// <param name="DataStatus">数据事实。</param>
/// <param name="ExecStatus">执行进度。</param>
/// <param name="RawSize">原始载荷字节数。</param>
/// <param name="Raw">原始载荷；列表查询为 null。</param>
/// <param name="CreateTime">创建时间（UTC）。</param>
/// <param name="UpdateTime">更新时间（UTC）。</param>
public sealed record RawDataDetailRecord(
    long Id,
    long SourceId,
    string FetchUrl,
    string Adapter,
    string RecordKey,
    string DataStatus,
    string ExecStatus,
    int RawSize,
    byte[]? Raw,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>
///     source 列表筛选与分页。
/// </summary>
/// <param name="Page">页码，从 1 开始。</param>
/// <param name="PageSize">每页数量，仓储钳制到 1–100。</param>
/// <param name="ChannelId">可选渠道筛选。</param>
/// <param name="FromUtc">可选创建时间下界（UTC）。</param>
/// <param name="ToUtc">可选创建时间上界（UTC）。</param>
/// <param name="ExecStatus">可选执行状态。</param>
/// <param name="DataStatus">可选数据状态。</param>
public sealed record RawDataSourceQuery(
    int Page,
    int PageSize,
    long? ChannelId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? ExecStatus = null,
    string? DataStatus = null);
