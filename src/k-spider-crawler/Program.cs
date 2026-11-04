using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// 抓取基座 : 原始数据抓取入库。
// 待实现（分层与职责唯一权威见 docs/10-architecture/crawler.md）：
//   Hosting/  启动与控制
//   Channel/  渠道任务绑定与配置
//   Engine/   列表轮 / 详情回填轮
//   Adapter/  源适配器
//   Parser/   最小解析
//   Kernel/   HttpGateway 与反爬验证
//   Common/   通用工具
//   Repository/ 数据访问

await builder.Build().RunAsync();
