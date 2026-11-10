namespace KSpider.Admin.Api.Contracts;

/// <summary>source 列表项；不包含 raw。</summary>
/// <param name="Id">source 主键。</param>
/// <param name="ChannelId">渠道主键。</param>
/// <param name="ChannelName">渠道名。</param>
/// <param name="Adapter">列表适配器编码快照。</param>
/// <param name="RecordKey">资源唯一键。</param>
/// <param name="DataStatus">数据事实。</param>
/// <param name="ExecStatus">执行进度。</param>
/// <param name="RawSize">原始载荷字节数。</param>
/// <param name="CreateTime">创建时间（UTC）。</param>
/// <param name="UpdateTime">更新时间（UTC）。</param>
public sealed record RawDataSourceListItem(
    long Id,
    long ChannelId,
    string ChannelName,
    string Adapter,
    string RecordKey,
    string DataStatus,
    string ExecStatus,
    int RawSize,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>source 详情；包含 raw。</summary>
/// <param name="Id">source 主键。</param>
/// <param name="ChannelId">渠道主键。</param>
/// <param name="ChannelName">渠道名。</param>
/// <param name="Adapter">列表适配器编码快照。</param>
/// <param name="RecordKey">资源唯一键。</param>
/// <param name="DataStatus">数据事实。</param>
/// <param name="ExecStatus">执行进度。</param>
/// <param name="RawSize">原始载荷字节数。</param>
/// <param name="RawBase64">原始载荷 Base64。</param>
/// <param name="RawEncoding">固定 base64。</param>
/// <param name="CreateTime">创建时间（UTC）。</param>
/// <param name="UpdateTime">更新时间（UTC）。</param>
public sealed record RawDataSourceDetailView(
    long Id,
    long ChannelId,
    string ChannelName,
    string Adapter,
    string RecordKey,
    string DataStatus,
    string ExecStatus,
    int RawSize,
    string RawBase64,
    string RawEncoding,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>detail 列表项；不包含 raw。</summary>
/// <param name="Id">detail 主键。</param>
/// <param name="SourceId">来源 source。</param>
/// <param name="FetchUrl">实际请求地址快照。</param>
/// <param name="Adapter">详情适配器编码快照。</param>
/// <param name="RecordKey">内容唯一键。</param>
/// <param name="DataStatus">数据事实。</param>
/// <param name="ExecStatus">执行进度。</param>
/// <param name="RawSize">原始载荷字节数。</param>
/// <param name="CreateTime">创建时间（UTC）。</param>
/// <param name="UpdateTime">更新时间（UTC）。</param>
public sealed record RawDataDetailListItem(
    long Id,
    long SourceId,
    string FetchUrl,
    string Adapter,
    string RecordKey,
    string DataStatus,
    string ExecStatus,
    int RawSize,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>detail 详情；包含 raw。</summary>
/// <param name="Id">detail 主键。</param>
/// <param name="SourceId">来源 source。</param>
/// <param name="FetchUrl">实际请求地址快照。</param>
/// <param name="Adapter">详情适配器编码快照。</param>
/// <param name="RecordKey">内容唯一键。</param>
/// <param name="DataStatus">数据事实。</param>
/// <param name="ExecStatus">执行进度。</param>
/// <param name="RawSize">原始载荷字节数。</param>
/// <param name="RawBase64">原始载荷 Base64。</param>
/// <param name="RawEncoding">固定 base64。</param>
/// <param name="CreateTime">创建时间（UTC）。</param>
/// <param name="UpdateTime">更新时间（UTC）。</param>
public sealed record RawDataDetailDetailView(
    long Id,
    long SourceId,
    string FetchUrl,
    string Adapter,
    string RecordKey,
    string DataStatus,
    string ExecStatus,
    int RawSize,
    string RawBase64,
    string RawEncoding,
    DateTime CreateTime,
    DateTime UpdateTime);
