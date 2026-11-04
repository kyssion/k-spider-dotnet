var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 管理控制服务 : 调度 k-spider-crawler 与 k-spider-process，并托管 web 前端。
// 待实现（机制唯一权威见 docs/10-architecture/distributed.md 与 docs/10-architecture/auth.md）：
//   调度与对账 / gRPC 节点服务 / REST 管理 API / 认证与账号 / 前端静态托管。
app.MapGet("/", () => "k-spider-admin");

app.Run();
