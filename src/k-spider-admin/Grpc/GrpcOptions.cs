using System.ComponentModel.DataAnnotations;

namespace KSpider.Admin.Grpc;

/// <summary>
///     gRPC 下行与探测超时配置。
/// </summary>
public sealed class GrpcOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "Admin:Grpc";

    /// <summary>单次下行写入超时（秒）；避免半死连接卡住调度/对账。</summary>
    [Range(1, 300)]
    public int SendTimeoutSeconds { get; set; } = 5;

    /// <summary>单次探测等待应答超时（秒）。</summary>
    [Range(1, 60)]
    public int ProbeTimeoutSeconds { get; set; } = 5;
}
