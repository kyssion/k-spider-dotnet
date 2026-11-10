using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace KSpider.Admin.Hosting;

/// <summary>
///     配置 REST 与 gRPC 的分离监听端点。
/// </summary>
public static class KestrelEndpointConfiguration
{
    /// <summary>
    ///     按 Admin:Endpoints 配置绑定 HTTP/1.1 与 HTTP/2 端口。
    /// </summary>
    /// <param name="context">宿主构建上下文。</param>
    /// <param name="options">Kestrel 选项。</param>
    public static void Configure(WebHostBuilderContext context, KestrelServerOptions options)
    {
        var endpoints = context.Configuration
            .GetSection(AdminEndpointsOptions.SectionName)
            .Get<AdminEndpointsOptions>() ?? new AdminEndpointsOptions();

        if (endpoints.RestPort == endpoints.GrpcPort)
        {
            throw new InvalidOperationException("REST 与 gRPC 不能使用同一端口");
        }

        var address = IPAddress.Parse(endpoints.BindAddress);
        options.Listen(address, endpoints.RestPort, listen => listen.Protocols = HttpProtocols.Http1);
        options.Listen(address, endpoints.GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
    }
}
