# 系统架构

## 一、系统全貌

```
   数据源                       采集进程                          存储                     使用侧
┌──────────────────┐      ┌──────────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│ 东方财富 列表/正文 │      │  k-spider-dotnet     │      │  PostgreSQL      │      │  PostgreSQL      │
│ 财联社   电报     │─────▶│  Generic Host + DI   │─────▶│  远端库           │─────▶│  本地库           │
│ 东财股票 Level1   │ HTTP │  Quartz 托管调度      │  ORM │  8 张表 + 触发器  │ 2 分钟│  (k-spider-sync) │
└──────────────────┘      └──────────────────────┘      └──────────────────┘      └──────────────────┘
```

采集与使用物理分离：`k-spider-dotnet` 只写远端库，`k-spider-sync` 把 4 张新闻表搬到本地库，
分析侧读本地库不干扰采集进程，两边可以独立重启与部署。

## 二、项目与依赖方向

| 项目 | 类型 | 职责 |
|---|---|---|
| `src/k-spider-dotnet` | Exe | 主爬虫：抓取、解析、落库、调度；含 Playwright（特殊页面渲染与栏目自检）与飞书 SDK（`Lark/`，当前无调用方） |
| `src/k-spider-sync` | Exe | 数据搬运：远端 → 本地增量同步（新行按 Id + 已有行按 `update_time` 双键水位） |
| `src/k-spider-test` | 类库 | MSTest 测试：离线夹具回归 + 真实接口连通性（`TestCategory=Live`） |

```
k-spider-sync ─┐
k-spider-test ─┴──▶ k-spider-dotnet   ( 复用 Model/ 实体、Data/Pg 连接工厂、Job/SpiderJob 基类 )
```

**实体只有一套**（主项目 `Model/`），另外两个项目不复制实体，避免三处漂移。

## 三、运行时模型

### 启动顺序（`Program.Main`）

1. `DOTNET_ENVIRONMENT` 未设置时默认 `Development`（Host 标准环境选择）。
2. `Host.CreateApplicationBuilder` 装配配置：`appsettings.json` → `appsettings.{环境}.json` → `K_SPIDER__` 前缀环境变量 → 代码默认值。
3. DI 注册：`DatabaseOptions`（IOptions）、`Pg`、`SpiderNewsDao`、`SpiderNewsBatchDao`、`StockDao`（均 Singleton）。
4. `AddQuartz(AddSpiderJobs)` 集中注册任务与触发器；`AddQuartzHostedService(WaitForJobsToComplete = true)` 保证收到退出信号后等在跑任务收尾。
5. `host.Build()` 后先执行 `Pg.EnsureSpiderNewsListDbObjects()` 幂等补齐库对象（库不可用时仅记日志，不阻断进程），再 `RunAsync()`。

### 调度模型

- **Quartz 内存态**：没有配置持久化 store，任务定义全部来自代码（`Program.AddSpiderJobs`），重启即按代码状态重建。
- 每个任务都加 `DisallowConcurrentExecution`：同一任务的上一轮没跑完时跳过本轮，避免重复抓取与并发写同一批数据。
- **短间隔轮询 + 状态机**，而不是消息队列 / 长连接：数据量级不大，任务状态直接存在数据库里可以随时自查，进程崩溃重启后按状态自然续跑，不需要额外中间件。

### 定时任务现状

| 任务 | 间隔 / Cron | 单轮批量 | 说明 |
|---|---|---|---|
| `FlashNewsJob` | 15 秒 | 每栏目最多 4 页 × 50 条 | **快讯源并发**拉取，直写 `spider_flash_news`（拉到即终态） |
| `NewsListJob` | 2 分钟 | 每栏目最多 4 页 × 200 条 | **网页型源并发**抓列表（源内仍串行翻页），见下 |
| `NewsContentOriginJob` | 3 秒 | 200 条 | 全源 FIFO 下载原始内容 |
| `NewsContentJob` | 1 分钟 | 1000 条 | 全源 FIFO 解析详情 |
| `NewsCheckJob` | 5 分钟 | — | 各源栏目探测 + 分源积压统计 |
| `StockCnJob` / `StockHkJob` | Cron 工作日 20:00 | 全股票池 | **默认停用**（`Program.cs` 中注释），按需启用 |
| `TransferSpiderDataJob`（sync 进程） | 2 分钟 | 2000 行 / 批 | 远端 → 本地增量同步 |

任务停用/启用只改 `Program.AddSpiderJobs` 里的注释，不要在别处加开关。

### 列表任务的并发模型

`NewsListJob` 以 **media（源）** 为并发单位：`Task.WhenAll` 遍历 `NewsSpiderRegistry.All`，每个 media 在自己的连接、事务与节奏里跑完自己的全部栏目。

- **media 之间并行**：各源互不依赖，整轮耗时从"各源之和"变成"最慢那个源"。
- **media 内部保持原逻辑**：逐栏目、按游标串行翻页（下一页依赖上一页的游标），每页一个事务。
- **每个 media 独占一个连接**（`SqlSugarClient` 不是线程安全的），因此并发度等于源的数量。当前 5 个源 + 连接池 `MaxPoolSize=10`，余量充足；源数量显著增长时需同步调大连接池。
- **单个 media 失败不外溢**：`RunSourceAsync` 捕获全部异常并记日志，返回该源本轮写入数，不影响其它源，也不吞掉整轮汇总日志。
- 汇总日志按源输出写入数（`source detail : DfMedia=12 | ClsMedia=3 | ...`），便于判断是哪个源没进来。

实测（空库冷启动，5 源）：串行调度整轮约 29.1 秒（东财 26.0 秒 + 其余四源合计 3.1 秒），并行调度整轮约 25.7 秒 ≈ 东财单源耗时。

> **关于各源的当前规模**：上面的耗时构成**只是当前开发进度的快照，不是架构特性**。东财是已开发最完整的源（35 个栏目），
> 其余四个源目前都只接了 1 个栏目（财联社「电报」、新浪「7x24」、见闻「全球宏观」、金十「快讯」），
> 而它们**本身有大量栏目可供扩展**——见闻实测有 `global-channel` / `a-stock-channel` / `forex-channel` /
> `commodity-channel` / `bond-channel` / `hk-stock-channel` / `us-stock-channel` 等 8 个以上频道，
> 金十快讯条目自带 5 个频道分类，财联社与新浪也都有分频道能力。
> 因此随着这些源的栏目补齐，"东财占大头"的格局会改变，media 级并行的收益也会随之放大
> （整轮耗时始终等于**最慢那个媒体**，而不是各源之和）。
>
> 但**补栏目不是加配置**：四个快讯源目前把频道参数与 `category` 写死在各自 Resource 常量里，
> 多栏目需要先改造取数与分类号传递（详见 [news-pipeline.md](news-pipeline.md) 的源明细表说明）。
>
> 评估调度改动时另注意：东财列表接口的网络耗时波动很大（实测单轮 7~26 秒），不要用单次测量下结论。

## 四、模块职责（主项目）

```
src/k-spider-dotnet/
├── Program.cs        # 唯一装配入口 : 配置 + DI + 任务注册
├── Config/           # DatabaseOptions ( IOptions 绑定 )
├── Data/             # Pg 连接工厂 + DAO ( 事务由 Job 层管理 ) + SpiderNewsBatchDao 手拼批量 SQL
│   └── Devtools/     # DbFirst 实体生成器 ( 开发期工具 , 不参与生产 )
├── Model/            # SqlSugar 实体 ( DbFirst 生成 , 带 Model 后缀 ) — 全解决方案唯一实体源
├── Job/              # SpiderJob 基类 + 定时任务 , 与 Spider 同维度分组
│   ├── News/Web/     #   网页型三段任务
│   ├── News/Flash/   #   FlashNewsJob ( 15 秒 )
│   ├── Check/        #   NewsCheckJob
│   └── Stock/        #   StockCnJob / StockHkJob ( 默认停用 )
├── Spider/           # 抓取与解析 , 按 "数据域 → 管线类型 → 源" 三级分组
│   ├── DataResource.cs   # 中心枚举 ( FromTypeOfNews / 状态机 / 分类号 )
│   ├── News/             # ── 新闻域 ──
│   │   ├── NewsSpiderModel.cs  # 跨管线共享 : NewsColumn / NewsContentSegment
│   │   ├── Web/           # 网页抓取型 : INewsSpider + NewsSpiderRegistry + Eastmoney/ ( 含 Playwright 兜底 )
│   │   └── Flash/         # 实时快讯型 : IFlashNewsSpider + FlashNewsSpiderRegistry + Cls/ Sina/ Wscn/ Jin10/
│   ├── Stock/            # ── 股票域 ── : IStockSpider + Eastmoney/ ( China/ Hk/ Usa/ )
│   └── Report/           # ── 研报域 ── : Eastmoney/ ( 当前无调用方 , 预留扩展 )
├── Tool/             # Http/ ( 伪装头客户端与 URL 工具 ) + Html/ ( 标签枚举与解析工具 )
├── Common/           # 公共工具 : Logger/ + Json/ + Collection/ + Strings/ + Time/
├── Lark/             # 飞书 SDK ( 当前无调用方 , 保留备用 )
└── Exceptions/       # DownloadHttpException 族 + KDbException
```

## 五、关键设计决策

| 决策 | 原因 | 代价 / 注意 |
|---|---|---|
| 三段接力 + 状态机，而不是一次抓完 | 抓取、下载、解析的失败原因与重试策略不同；解析规则改版后可直接用已存的原始内容重跑，不必重新抓取 | 需要维护状态机与重试计数；积压要额外监控 |
| 原始内容（`spider_news_content_origin`）单独存整篇响应 | 解析可重跑、源改版后可回溯比对、出问题能拿到原始证据 | 表增长快，需要定期归档（见 data-model） |
| 事务边界在 Job 层，DAO 只接收连接 | 一次业务动作的原子范围一眼可见；DAO 无状态、可复用 | Job 代码略长 |
| 表结构不用 CodeFirst，DDL 手工维护 + DbFirst 反向生成实体 | 索引 / 部分索引 / 触发器 / 约束这些是对生产库有实际影响的对象，交给 DDL 更可控 | 改表要 Model 与 DDL 两处同步 |
| 多源抽象在出现第二个源时才提取 | 避免为假想扩展点提前设计（见 principles） | 东财单源时期的历史代码需要一次性改造 |
| 快讯型源在列表阶段直接落原始内容并置 `status=3` | 这类源没有可回查的单条接口，原始内容只能在列表响应里拿到 | 列表行与原始内容必须同事务写入 |
| 轮询热路径依赖部分索引 | 轮询每 3 秒一次，全表扫描会拖垮库 | 索引变更要用 `CREATE INDEX CONCURRENTLY` 上线（普通建索引的写锁会卡住 3 秒轮询） |
| 测试分"离线夹具 + 真实连通性"两层 | 门禁要确定性，接口是否还活着要能真实检验 | 联网用例需能从门禁排除 |

## 六、扩展点

| 要加什么 | 怎么做 |
|---|---|
| 新闻源 | 实现 `Spider/News/Web/INewsSpider.cs` → 在 `NewsSpiderRegistry` 注册一行 → `FromTypeOfNews` 加枚举值；表结构无需改动。参考 [news-pipeline.md](news-pipeline.md) |
| 定时任务 | 继承 `Job/SpiderJob.cs`（只需实现 `Execute`）→ 构造函数注入 DAO/Pg/`ILogger<T>` → `Program.AddSpiderJobs` 加 `AddJob` + `AddTrigger` 两行（`DisallowConcurrentExecution` 必加） |
| 表字段 / 索引 | 增量演进（列、索引）可加到 `Pg.EnsureSpiderNewsListDbObjects()` 启动幂等执行；结构性变更同时改 `Model/` 与 `db/k_script_spider.sql` |
| 同步到本地的表 | `k-spider-sync` 的 `TransferSpiderData.DoTransfer` 加一行 `SyncTableSafely<T>`（实体需实现 `ILongIdEntity` + `IUpdateTimeEntity`） |
| 需要浏览器渲染的页面 | `Spider/News/Web/Eastmoney/Playwright/` 已有模式可参考；该命名空间下调用库入口要写全限定 `Microsoft.Playwright.Playwright` |
