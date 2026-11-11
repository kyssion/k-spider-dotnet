using KSpider.Admin.Api.Endpoints;
using KSpider.Admin.Auth;
using KSpider.Admin.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KSpider.Admin.Hosting;

/// <summary>
///     admin 端点映射。
/// </summary>
public static class AdminEndpointExtensions
{
    /// <summary>
    ///     映射 REST、健康检查与 gRPC 端点。
    /// </summary>
    /// <param name="app">Web 应用。</param>
    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        // 登录/登出/me/改密；仅登录允许匿名，其余要求登录，写接口仍受 CSRF 保护。
        app.MapAuthEndpoints();
        // 账号列表、创建、启停与重置密码。
        app.MapUserEndpoints();
        // 渠道分类/渠道启停、任务查询/取消、节点列表与总览。
        app.MapManagementEndpoints();
        // 原始数据 source/detail 只读浏览。
        app.MapRawDataEndpoints();

        // 健康检查不需要登录，供部署探针使用；内部仍执行数据库检查。
        app.MapHealthChecks("/api/health").AllowAnonymous();
        // 节点 gRPC Connect 流；节点认证/TLS 仍待部署层补齐，因此先匿名暴露。
        app.MapGrpcService<SchedulerGrpcService>().AllowAnonymous();

        return app;
    }
}
