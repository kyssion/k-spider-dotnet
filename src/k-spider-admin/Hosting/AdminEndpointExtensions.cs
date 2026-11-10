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
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapManagementEndpoints();
        app.MapRawDataEndpoints();

        app.MapHealthChecks("/api/health").AllowAnonymous();
        app.MapGrpcService<SchedulerGrpcService>().AllowAnonymous();

        return app;
    }
}
