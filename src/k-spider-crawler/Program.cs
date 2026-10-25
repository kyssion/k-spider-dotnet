using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// 抓取基座 : 原始数据抓取入库。
// 待实现（按 docs/data-model.md 与模块结构逐步落地）：
//   Models/  四业务表实体 + 代码枚举
//   Data/    Pg 连接 / 托管 migration / 覆盖式 upsert / 双水位
//   Http/    HttpGateway 唯一出口
//   Verification/  反爬验证
//   Engine/  调度 + 管线
//   Sources/ 源适配器

await builder.Build().RunAsync();
