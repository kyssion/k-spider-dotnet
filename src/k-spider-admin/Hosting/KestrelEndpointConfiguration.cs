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
        // 每次启动重新读取配置，部署方可用环境变量覆盖绑定地址与端口。
        var endpoints = context.Configuration
            .GetSection(AdminEndpointsOptions.SectionName)
            .Get<AdminEndpointsOptions>() ?? new AdminEndpointsOptions();

        // 明文 Kestrel 无法在同一端口可靠协商 HTTP/1.1 与 h2c gRPC，必须拆成两个监听。
        if (endpoints.RestPort == endpoints.GrpcPort)
        {
            throw new InvalidOperationException("REST 与 gRPC 不能使用同一端口");
        }

        var address = IPAddress.Parse(endpoints.BindAddress);
        // REST：浏览器 API 与 SPA 静态资源走 HTTP/1.1。
        options.Listen(address, endpoints.RestPort, listen => listen.Protocols = HttpProtocols.Http1);
        // gRPC：节点 Connect 双向流走 HTTP/2（当前 h2c，TLS 由部署层决定）。
        options.Listen(address, endpoints.GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
    }
}
