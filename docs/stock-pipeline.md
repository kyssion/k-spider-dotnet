# 股票管线设计

股票管线抓的是**当日 Level1 归档快照**（A 股与港股），与新闻管线完全独立：不共享状态机、不走三段接力，也不需要同步到本地（`k-spider-sync` 只搬 4 张新闻表）。

## 一、数据来源

行情接口是 SSE（Server-Sent Events）流：`17.push2.eastmoney.com/api/qt/stock/details/sse`，按 `secid = <市场号>.<股票代码>` 请求，第一帧就是当日的归档数据。

`AbsStockSpider.GetLevel1DailyArchived(url, channelId, stockId)` 只读第一行就断开——要的是归档首帧，不是持续推送：

- 读到的首帧是 `{"data":null}` 之类时返回空串，由 Job 判定为"当日无归档"并跳过（**不落空行**）。
- 共享 `HttpClient` 不能 Dispose（见 `Tool/Http/HttpClientTools.cs`）。

| 市场 | 实现 | 接口市场号（`secid` 前缀） | 库内 `exchange_channel` |
|---|---|---|---|
| 上海 | `Spider/Stock/Eastmoney/China/ShStockSpider.cs` | `1` | `1` (`ShangHStockExchangeChannel`) |
| 深圳 / 北交所 | `Spider/Stock/Eastmoney/China/SzBjStockSpider.cs` | `0` | `0` (`SzBjStockExchangeChannel`) |
| 港股 | `Spider/Stock/Eastmoney/Hk/HkStockSpider.cs` | `116`（实现内硬编码） | `2` (`HkStockExchangeChannel`) |
| 美股 | `Spider/Stock/Eastmoney/Usa/UsaStockSpider.cs` | — | `3` (`UsaStockExchangeChannel`) |

> 注意：库内渠道枚举与接口市场号**不是同一套值**——沪 / 深北恰好一致，港股库内是 `2`、接口要 `116`。
> 加新市场时别直接拿枚举值去拼 `secid`。

`IStockSpider` 还声明了 `Level1Listening`（实时监听），当前各实现均未启用（抛 `NotImplementedException` / 无调用方）——它是预留能力，不是生产链路。

## 二、执行流程

```
stock_cn_introduction ( 股票池 , 按 ExchangeChannel 区分市场 )
        │  Job 逐个股票请求 SSE 归档接口
        ▼
  空数据 → 跳过 ( 不落库 )
        │ 有数据
        ▼
stock_{cn,hk}_level1_archived_daily_origin  ( 唯一键 (date, stock_id) , upsert )
```

- `StockCnJob`：读 `exchange_channel ∈ {深北(0), 上海(1)}` 的股票，按渠道分发到对应爬虫 → 写 `stock_cn_level1_archived_daily_origin`。
- `StockHkJob`：读 `exchange_channel = 港股(2)` 的股票 → `HkStockSpider` → 写 `stock_hk_level1_archived_daily_origin`。
- 股票池来自 `stock_cn_introduction`，**A 股与港股共用同一张池表**，靠 `exchange_channel` 区分（枚举见 `Spider/DataResource.cs` 的 `StockExchangeChannel`）。
- 单个股票失败只记日志并 `continue`，不中断整轮；写入按 `(date, stock_id)` upsert，重跑同一天是幂等的。

## 三、启用方式与注意事项

任务默认**停用**（`Program.AddSpiderJobs` 里被注释），这是有意的按需启用，不要顺手打开：

```csharp
// quartz.AddJob<StockCnJob>(j => j.WithIdentity("StockCnJob").DisallowConcurrentExecution())
//     .AddTrigger(t => t.WithIdentity("StockCnJob.Trigger").ForJob("StockCnJob").StartNow()
//         .WithCronSchedule("0 0 20 ? * MON-FRI"));
```

- Cron 是工作日 20:00，**依赖服务器时区**：UTC 服务器上会在错误的日期抓取。systemd 模板已设 `TZ=Asia/Shanghai`，自管进程需要自行确认。
- 日期取 `DateTime.Today`，同样依赖时区。
- `StockUsaJob`、`UsaStockSpider`（空类，只留了一段富途接口的抓包样例注释）与 `stock_usa_level1_archived_daliy_origin`（表名与实体里的 `daliy` 是历史笔误，已成事实契约）当前**未接入**：Job 类不是 `SpiderJob`、没有 DI 注册也没有调用方。
- `Spider/Stock/Eastmoney/Devtools/` 是一次性下载工具，不参与生产链路。
