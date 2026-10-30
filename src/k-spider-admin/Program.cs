var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 管理控制服务 : 控制 k-spider-crawler 与 k-spider-process。
// 待实现（逐步落地）：渠道配置 / 指令下发 / 状态监控 / 数据查看接口。
app.MapGet("/", () => "k-spider-admin");

app.Run();
