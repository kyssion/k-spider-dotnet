using KSpider.Admin.Hosting;
using KSpider.Admin.WebHosting;

var builder = WebApplication.CreateBuilder(args);

// 配置优先级：appsettings → appsettings.{环境} → K_SPIDER__ 前缀环境变量 → 代码默认值。
builder.Configuration.AddEnvironmentVariables("K_SPIDER__");

// REST 与 gRPC 使用分离监听端点：REST HTTP/1.1，gRPC HTTP/2（ADR 0002）。
builder.WebHost.ConfigureKestrel(KestrelEndpointConfiguration.Configure);

// 注册 admin 组合根：Options、PostgreSQL 仓储、任务/调度/对账服务、gRPC、认证、
// 查询服务与后台循环；后续 UseAdminPipeline/MapAdminEndpoints 依赖这些注册。
builder.Services.AddAdminServices(builder.Configuration);

// 构建 DI 容器与请求管道；AddAdminServices 注册的后台服务从这里开始具备启动条件。
var app = builder.Build();

// 安装请求管道：异常边界 → 安全响应头 → 静态资源 → Cookie 认证 → 授权 → CSRF。
app.UseAdminPipeline();
// 映射 REST（认证/账号/渠道/任务/节点/原始数据）、健康检查与 gRPC 调度端点。
app.MapAdminEndpoints();
// 非 /api、/grpc 的未命中路径回退到 SPA；保证前端客户端路由刷新可恢复。
app.MapSpaFallback();

// 启动 Kestrel 与所有 IHostedService（调度、对账、节点健康、任务清理）。
app.Run();
