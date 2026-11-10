namespace KSpider.Admin.Repository.Models;

/// <summary>
///     sys_node 行快照。
/// </summary>
/// <param name="Id">代理主键。</param>
/// <param name="NodeId">节点启动时生成的 uuid。</param>
/// <param name="NodeType">admin / crawler / process。</param>
/// <param name="Status">online / offline。</param>
/// <param name="Ip">展示用地址。</param>
/// <param name="Port">展示用端口。</param>
/// <param name="DeviceJson">硬件信息 JSON。</param>
/// <param name="ExtraJson">节点扩展 JSON。</param>
/// <param name="CreateTime">首次注册时间。</param>
/// <param name="UpdateTime">镜像更新时间。</param>
public sealed record SysNodeRecord(
    long Id,
    Guid NodeId,
    string NodeType,
    string Status,
    string? Ip,
    int? Port,
    string? DeviceJson,
    string ExtraJson,
    DateTime CreateTime,
    DateTime UpdateTime);

/// <summary>
///     节点注册/复活写入参数。
/// </summary>
/// <param name="NodeId">节点 uuid。</param>
/// <param name="NodeType">节点类型。</param>
/// <param name="Ip">展示用地址。</param>
/// <param name="Port">展示用端口。</param>
/// <param name="DeviceJson">硬件 JSON。</param>
/// <param name="ExtraJson">扩展 JSON。</param>
public sealed record SysNodeUpsert(
    Guid NodeId,
    string NodeType,
    string? Ip,
    int? Port,
    string? DeviceJson,
    string ExtraJson);
