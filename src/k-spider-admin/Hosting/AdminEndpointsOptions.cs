using System.ComponentModel.DataAnnotations;

namespace KSpider.Admin.Hosting;

/// <summary>
///     admin REST / gRPC 监听配置。
/// </summary>
/// <remarks>
///     REST 与 gRPC 必须使用不同端口（明文单端口无法可靠同时协商 HTTP/1.1 与 h2c gRPC）。
/// </remarks>
public sealed class AdminEndpointsOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "Admin:Endpoints";

    /// <summary>绑定地址；开发默认仅本机，生产按部署改为 0.0.0.0 或内网地址。</summary>
    [Required(AllowEmptyStrings = false)]
    public string BindAddress { get; set; } = "127.0.0.1";

    /// <summary>REST HTTP/1.1 端口。</summary>
    [Range(1, 65535)]
    public int RestPort { get; set; } = 5094;

    /// <summary>gRPC HTTP/2 端口。</summary>
    [Range(1, 65535)]
    public int GrpcPort { get; set; } = 5095;
}
