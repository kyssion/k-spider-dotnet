using KSpider.Admin.Hosting;
using KSpider.Admin.WebHosting;

var builder = WebApplication.CreateBuilder(args);

// 配置优先级：appsettings → appsettings.{环境} → K_SPIDER__ 前缀环境变量 → 代码默认值。
builder.Configuration.AddEnvironmentVariables("K_SPIDER__");

// REST 与 gRPC 使用分离监听端点：REST HTTP/1.1，gRPC HTTP/2（ADR 0002）。
builder.WebHost.ConfigureKestrel(KestrelEndpointConfiguration.Configure);

builder.Services.AddAdminServices(builder.Configuration);

var app = builder.Build();

app.UseAdminPipeline();
app.MapAdminEndpoints();
app.MapSpaFallback();

app.Run();
