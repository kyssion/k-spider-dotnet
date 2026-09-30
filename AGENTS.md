# AGENTS.md — AI 代理开发指南

本文件面向 AI 编码代理（ZCode / Claude Code / Cursor / Copilot Agent 等）。开始任何修改前请先读完本文件。

## 项目是什么

7x24 小时金融数据爬虫：抓取财经新闻快讯与实时快讯（多源框架，网页抓取型接入东方财富 35 个栏目、财联社文章频道 13 个栏目与品见拼装流、新浪财经文章 22 个栏目、华尔街见闻文章全量流、金十「市场参考」5 个栏目、同花顺文章 10 个栏目与每经文章 3 个栏目；实时快讯型 15 秒轮询接入财联社电报 / 新浪 7x24 / 华尔街见闻 live（7 频道）/ 金十快讯 / 同花顺 7x24 / 格隆汇 live；研报独立管线 5 分钟轮询接入东财研报中心个股/行业/宏观三类；盘面榜单 Ranking 管线 30 分钟轮询接入东财数据中心龙虎榜/大宗/两融；公告 Announcement 管线 10 分钟轮询接入巨潮资讯沪深京法定披露（分类白名单）），存入 PostgreSQL；独立部署的 Web 控制台（`k-spider-web` + 仓库根 `web/` 前端）提供运行状态总览、数据查询、分析与任务控制。解决方案共 4 个项目，目标框架 net10.0，ORM 统一使用 SqlSugar，基于 Generic Host + 依赖注入 + Options 模式。

## 常用命令

```bash
# 构建整个解决方案（改完代码必须跑 , 当前零警告零错误 , 不要引入新告警）
dotnet build k-spider-dotnet.sln

# 运行全部测试（含真实接口连通性用例 , 需要联网 ; 断网时该用例报告跳过而非失败）
dotnet test src/k-spider-test/k-spider-test.csproj

# 只跑离线用例（夹具解析回归 , 不依赖网络与数据库 , 随时可执行）
dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory!=Live"

# 只跑真实接口连通性（验证两源 URL 能调通并拿到数据集 , 排障时先跑这个）
dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory=Live"

# 一键验证（文档链接 + 前端构建(有 pnpm 时) + 构建 + 离线用例 , CI 同款）
./scripts/verify.sh

# 构建前端并拷贝产物到 k-spider-web/wwwroot（改了 web/ 必须跑 , 需要 Node 20+ 与 pnpm）
bash scripts/build-web.sh

# 前端本地开发（5173 端口热更新 , /api 自动代理到 5800）
cd web && pnpm install && pnpm dev

# 前端 E2E（Playwright + mock API 夹具 , 离线确定 ; 首次先 pnpm --dir web exec playwright install chromium）
cd web && pnpm test:e2e

# 前端真实后端连通性（先起 k-spider-web 再跑 , 与后端 Live 用例同款分层）
cd web && pnpm test:e2e:live

# 本地运行主程序（需要可用的 PostgreSQL , 默认 Development 环境）
dotnet run --project src/k-spider-dotnet

# 本地运行 Web 控制台（默认 http://localhost:5800 , 先跑过 build-web 才有页面）
dotnet run --project src/k-spider-web

# 本地切换环境调试（构建与运行参数对齐 , 确保对应环境配置文件被拷贝）
DOTNET_ENVIRONMENT=Test dotnet run --project src/k-spider-dotnet -p:SpiderEnvironment=Test

# Linux 自包含发布（部署用 , 产物只带目标环境配置）
dotnet publish src/k-spider-dotnet/k-spider-dotnet.csproj -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

环境要求：.NET SDK 10（`global.json` 锁 9.0.0 但 `rollForward: latestMajor` 会自动使用 10.x，已禁用预览版）。

## 解决方案结构

仓库根 = 解决方案根，四个项目位于 `src/` 下：

| 项目 | 类型 | 职责 |
|---|---|---|
| `src/k-spider-dotnet` | Exe | 主爬虫：新闻抓取、解析、落库、Quartz 托管调度；含 Playwright（特殊页面抓取与栏目自检）与飞书 SDK（`Lark/`，当前无调用方） |
| `src/k-spider-sync` | Exe | 数据搬运：SqlSugar 把远端 PG 的 6 张数据表（4 张网页新闻 + `spider_flash_news` + `spider_research_report`）同步到本地（新行按 Id 增量 + 已有行按 `update_time` 双键水位更新，水位存本地 `sync_transfer_watermark` 表、sync 启动幂等自建），引用主项目实体 |
| `src/k-spider-web` | Exe | Web 控制台：只读查询/分析 Minimal API + 前端静态页伺服；任务控制（触发/暂停/恢复）写 `spider_job_command` 指令表异步受理（约 3 秒内由爬虫节点的 `NodeStateJob` 消费），引用主项目实体与 `SystemStatusDao` |
| `src/k-spider-test` | 类库 | MSTest 单元测试（全部离线） |

依赖方向：`k-spider-sync / k-spider-web / k-spider-test → k-spider-dotnet`（复用 `Model/` 实体与 `Data/Pg` 连接工厂）。**实体只有一套**（主项目 `Model/`）。前端源码在仓库根 `web/`（React + TS + Vite + pnpm），构建产物拷入 `k-spider-web/wwwroot`。

主项目目录（命名空间 PascalCase 风格 `KSpider.*`；代码在项目根下）：

```
src/k-spider-dotnet/
├── Program.cs                 # Host 入口 : DI 注册 + AddSpiderJobs 集中调度注册 + 优雅停机
├── appsettings*.json          # 多环境配置 ( Development / Test / Production )
├── Config/                    # DatabaseOptions ( IOptions 绑定 )
├── Data/                      # Pg 连接工厂 + DAO ( DI Singleton ) + SystemStatusDao ( 系统表通道 ) + Devtools/ ( 开发期工具 )
├── Model/                     # SqlSugar 实体（DbFirst 生成，带 Model 后缀）
├── Job/                       # SpiderJob 基类 + 定时任务 , 与 Spider 同维度分组
│   ├── News/Web/              #   网页型三段 : NewsListJob / NewsContentOriginJob / NewsContentJob
│   ├── News/Flash/            #   快讯型 : FlashNewsJob ( 15 秒 )
│   ├── News/Report/           #   研报 : ResearchReportJob ( 5 分钟 , 第三管线 )
│   ├── Ranking/               #   盘面榜单 : RankingJob ( 30 分钟 , 第四管线 )
│   ├── Announcement/          #   公告 : AnnouncementJob ( 10 分钟 , 第五管线 )
│   ├── Check/                 #   NewsCheckJob ( 含节点快照上报 )
│   ├── Node/                  #   NodeStateJob + NodeIdentity ( 调度态上报 + 指令消费 , 3 秒 )
│   └── JobRuntimeListener.cs  #   IJobListener : 每次执行完把结果/耗时/RunSummary 写 spider_job_state
├── Spider/                    # 爬虫实现 , 按 "数据域 → 管线类型 → 源" 三级分组
│   ├── DataResource.cs        #   中心枚举 ( FromTypeOfNews / 状态机 / 分类号 )
│   ├── News/                  #   ── 新闻域 ──
│   │   ├── NewsSharedModel.cs #     跨管线共享 : NewsColumn / NewsContentSegment
│   │   ├── Web/               #     网页抓取型 : INewsSpider + NewsSpiderRegistry + Eastmoney/ Cls/ Sina/
│   │   ├── Flash/             #     实时快讯型 : IFlashNewsSpider + FlashNewsSpiderRegistry + Cls/ Sina/ Wscn/ Jin10/
│   │   └── Report/            #     研报 ( 第三管线 ) : DfResearchReportSpider 直连 , 无注册表 ( 单源 )
│   └── Verify/                #   反爬验证 ( 数据域无关 ) : 根 = 对外面 ( 契约接口 / Registry / Policy / VerifiedHttp )
│       ├── Model/             #     共享词汇 : 枚举与纯数据 ( Kind / Probe / Challenge / Solving / Outcome / Session )
│       ├── Pipeline/          #     编排与运行时状态 : VerificationPipeline + VerificationSessionStore
│       ├── Detector/          #     识别器 : HTTP 门禁 / Cloudflare / JS cookie 门禁 / 验证码 / 载荷风控
│       └── Solver/            #     通过策略 : Browser/ ( 浏览器基建 + 过挑战 / 滑块 ) + 人工升级
├── Common/                    # 通用工具 : 纯函数工具平铺 ( JsonTools / StringTools / ListTools / TimeTools / LogFactory ) + Http/（HttpClient 伪装头、URL 工具）+ Html/（标签枚举、图片提取）
├── Lark/                      # 飞书 SDK（当前无调用方，保留备用）
└── Exceptions/                # 异常体系（DownloadHttpException 族、KDbException）
```

仓库根：`Directory.Build.props`（公共属性）、`Directory.Packages.props`（**CPM 包版本中心**，加包两处改：这里加版本 + csproj 加无版本引用）、`db/`（建库 DDL `k_script_spider.sql`）、`deploy/`（systemd 模板）、`docs/`（设计文档，见 [docs/README.md](docs/README.md) 索引）、`.github/workflows/ci.yml`（CI）。

## 核心数据流（改动前必须理解）

新闻管线按数据形态分两条 , 互相独立 ; 研报是新闻域之外的第三条管线 , 也独立 :

**网页抓取型** ( 有独立详情页的源 , 当前只有东财 ) —— 三段接力 + 状态机 :

```
NewsListJob (每2分钟, Job/News/)
  各 media 并行 ( 每源独立连接与事务 ; 源内仍逐栏目按游标串行翻页 , 最多4页 )
  → 批量 ON CONFLICT DO NOTHING 写 spider_news_list (status=0)
  单源失败只记日志 , 不影响其它源

NewsContentOriginJob (每3秒, 每轮200条, 全源按Id先进先出)
  取 status=0 或 (status=4 且 fail_count<3)
  → 按 from_media 分发抓文章原始内容 → spider_news_content_origin → status=3 (失败→4, fail_count+1)

NewsContentJob (每1分钟, 按Id先进先出)
  取 status=3 或 (status=2 且 fail_count<3) , 批量预加载 origin
  → 按 from_media 分发解析 → spider_news_content / spider_news_image_list
  → status=1 (失败→2, fail_count+1 ; origin 缺失→退回4重新下载)
```

**实时快讯型** ( "列表即全文"的源 : 财联社电报 / 新浪 7x24 / 见闻 live / 金十快讯 ) —— 单段直写 , 无状态机 :

```
FlashNewsJob (每15秒, Job/News/)
  各源并行 , 每源独立连接 ; 拉一页即完整数据
  → 批量 ON CONFLICT (from_media, news_url) 写 spider_flash_news ( 拉到即终态 )
  已存在行仅当 raw_content 变化时更新内容字段 ( 快讯发布后数分钟内的修正随下一轮 poll 回填 , 未变不空转更新 )
  失败记日志 , 下一轮 ( 15 秒后 ) 自然重试 ; 停机回补按游标最多翻 4 页
```

**研报** ( 东财研报中心 , 个股/行业/宏观三类 ) —— 列表即结构化元数据直写 + 摘要二段回填 , 无状态机 :

```
ResearchReportJob (每5分钟, Job/News/Report/)
  三类 qType 各拉近 3 天窗口 , 页码翻页最多 4 页
  → 批量 ON CONFLICT (info_code) DO NOTHING 写 spider_research_report ( 元数据不可变 )
  → 摘要回填 : summary 为空且 summary_fail_count<3 的行每轮限量 20 条 ,
    请求详情页 ( 三类模板 zw_stock/zw_industry/zw_macresearch ) 解析 ctx-content 回填
  失败记日志下一轮重试 ; KDbException 中断本轮且不消耗计数 ( 与主管线同语义 )
  注意 : 研报走独立表与独立 Job , 不进两个新闻注册表 ; reportapi 与新闻管线 np-listapi 不是同一套接口
```

NewsCheckJob (每5分钟, Job/Check/): 各源栏目接口可用性探测 ( 含研报列表 ) + 分源流水线状态统计 + 快讯源实时性滞后监控 ( 最新一条距现在多久 ) + 研报实时性监控 ( 最新发布日期距今超 3 天告警 , 只记日志不入节点快照 ) + 反爬验证阻塞告警 ( 处于验证冷却期的源 )。

**抓取层的 HTTP 出口统一是 `Spider/Verify/VerifiedHttp`**（不再直接调 `HttpClientTools`）：它自动带上已通过验证的会话 cookie，识别到反爬拦截时按注册表里的策略自动过验证并重放请求，过不了则抛带验证类型的 `VerificationRequiredException`。新增源照抄现有源的写法即可。

状态机：`0 未下载 → 3 已下载原始 → 1 已解析详情`，失败态 `2 解析失败 / 4 下载失败`。失败态在 `fail_count < NewsPipelineConst.MaxFailCount(3)` 时自动重试；数据库异常（KDbException）不消耗重试次数。

股票管线已移除（2026-09）：原 `StockCnJob/StockHkJob` 与 `stock_*` 表逻辑已从代码与 DDL 删除，历史实现与表结构可从 git 历史找回；快讯表的 `stock_list`（关联标的）只是数据字段，与已删除的股票管线无关。

## 代码约定

### 开发原则（最高优先级，覆盖以下所有条目）

- **不过度设计、不过度封装**：优先最简单可用的实现；不为假想的扩展点提前加抽象层（接口/泛型/继承层级），只有出现第二个真实使用者时才提取抽象。
- **可读性优先**：代码首先是给人读的；注释/日志用中文，新代码与所在文件的既有风格保持一致；宁要直白的长代码，不要绕弯的短代码。
- **兼顾性能最优化**：热路径（轮询查询、批量写入、HTTP 调用）必须注意连接复用、批量操作与索引支撑；但不做无测量依据的微优化，不为性能牺牲可读性。

### 具体约定

- 注释、日志、commit message 用中文。
- **新增定时任务的固定套路**：继承 `SpiderJob`（Job/SpiderJob.cs，只需实现 `Execute`）→ 构造函数注入所需 DAO/Pg/`ILogger<T>` → 在 `Program.AddSpiderJobs` 加 `AddJob`+`AddTrigger` 两行（照抄现有条目，`DisallowConcurrentExecution` 必加）。
- Job 必须 async：网络调用直接 `await`，不要 `.Result`/`.Wait()`。
- DAO 风格：`Data/` 下实例类（DI Singleton 注入），方法接收 Job 层创建并传入的 `SqlSugarClient`（事务由 Job 层管理），upsert 用 `Storageable(...).WhereColumns(唯一列)` + `IgnoreColumns("id","create_time","update_time")`，批量写入优先 `SpiderNewsBatchDao` 的手拼 `ON CONFLICT`（一文件一类），异常包装为 `KDbException`。
- 事务写法：`BeginTran → 业务 → CommitTran`，catch 中 `RollbackTran`（SqlSugar 对无活动事务的 Rollback 是安全空操作），不要在 finally 里回滚。
- 日志：DI 托管类（Job/DAO/Pg）注入 `ILogger<T>`；Spider 爬虫类与 Devtools 过渡期仍可用 `LogFactory.GetLogger<T>()`（注意：静态类不能作类型参数）。
- 表结构变更：改主项目 `Model/` 实体 + `db/k_script_spider.sql` 两处；属于"增量演进"的列/索引可加到 `Pg.EnsureSpiderNewsListDbObjects()`（启动时幂等执行）。
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + 项目 csproj 加无版本 `PackageReference`。
- **测试夹具**：接口真实响应放 `src/k-spider-test/TestData/`（csproj 已配置 `CopyToOutputDirectory`），解析回归优先用真实响应而不是手搓 JSON；新增夹具时在同目录 `README.md` 登记来源接口、抓取时间与参数，接口改版或解析变更时同步重抓并更新断言。唯一例外是反爬挑战页样本（`verify_*.html`，按公开特征构造并在 README 里标注），与"真实响应不误判"用例互补。
- **真实接口连通性用例**：`LiveConnectivityTest`（`[TestCategory("Live")]`）直接请求线上 URL，验证"能调通 + 能拿到数据集 + 能解析"，排障（源改版、签名失效）时先跑它。网络不可达/超时报告为跳过，接口能连上却拿不到数据则判失败；`verify.sh` 与 CI 用 `--filter "TestCategory!=Live"` 排除，门禁保持离线确定。
- 爬虫实现一律放 `Spider/` 目录 , 按下方网页抓取型 / 实时快讯型两条套路接入。
- **新增网页抓取型新闻源**（有独立详情页）：实现 `Spider/News/Web/INewsSpider.cs`（列表 / 原始内容 / 解析 三段）+ 在 `NewsSpiderRegistry` 注册一行（新网站才加 `FromTypeOfNews` 枚举值；已有网站的第二个管线复用原值，如新浪文章复用 `SinaMedia`）+ `Model/` 与 DDL 无需改动（`from_media` 已在表上）。侦察流程与验收标准见 [docs/web-source-playbook.md](docs/web-source-playbook.md)。参考实现：`Spider/News/Web/Eastmoney/DfNewsSpider.cs`（页码翻页 + 详情接口）、`Spider/News/Web/Cls/ClsArticleSpider.cs`（时间游标 + 详情页 SSR `__NEXT_DATA__`）、`Spider/News/Web/Sina/SinaArticleSpider.cs`（一个源两套列表体系 + 详情整页 HTML 作 origin）。
  - 翻页走 `GetListPage(column, pageSize, cursor)` 的不透明游标，`NextCursor = null` 表示没有更多。
- **新增实时快讯源**（"列表即全文"）：实现 `Spider/News/Flash/IFlashNewsSpider.cs`（一个方法：`GetFlashPage` 拉一页完整记录）+ 在 `FlashNewsSpiderRegistry` 注册一行。参考实现：`Spider/News/Flash/Cls/ClsNewsSpider.cs`（时间游标）、`Spider/News/Flash/Jin10/Jin10NewsSpider.cs`（含 PLUS 锁定条目兜底与跳过）。写 `spider_flash_news` , 无状态机、无下载/解析阶段。
  - 各源 `category` 用独立编号段（东财 1-22、财联社电报 101、财联社文章 102-114 + 品见 115、新浪 7x24 201 + 新浪文章 202-223、见闻 301 + 见闻文章 302-311 + live 频道 312-317、金十 401 + 金十文章 402-406、同花顺 501 + 文章 502-511、格隆汇 601、每经 701-703），不要去复用别源的语义；`level` 重要度统一 1/2/3（各源映射见 docs/news-pipeline.md）。
  - 两个任务都按源并发，**源实现必须是线程安全的**：不要用可变实例字段保存请求状态（如"当前游标"），游标与页状态一律走方法参数与返回值。
- **新增研报源 / 研报栏目**（第三管线，`spider_research_report` 专属表）：研报是"列表即结构化元数据"的形态（评级/个股/机构/盈利预测在列表接口一次给全），**不进两个新闻注册表**，由 `ResearchReportJob` 直连源类。参考实现：`Spider/News/Report/Eastmoney/`（三件套 Resource/SpiderModel/Spider）+ `ResearchReportJob` + `SpiderResearchReportDao`。要点：① `report_kind` 枚举（1 个股/2 行业/3 宏观）对应接口 qType，枚举值 = qType + 1；② 详情页三类模板路径不同（zw_stock/zw_industry/zw_macresearch），加新研报类型时先实测模板；③ `pageSize` 钳 100（实测 200 不报错但没必要）；④ 表结构变更同步 `Model/`、`db/k_script_spider.sql` 与 `Pg.EnsureResearchReportDbObjects` 三处。
- **新立管线类型**（数据形态与 Web/Flash/Report 都不同，如日历/公告/榜单）：管线类型由数据形态决定、不由网站决定；判别标准是需要**新表体系 + 新 Job + 新写入/重试语义**——满足即新类型，**先把"类型契约"（形态定义 / 存储与去重键 / 节奏 / 写入语义 / 监控 / 生态挂接 / 测试基线 七项）定进 [docs/architecture.md](docs/architecture.md) 的"管线类型契约"小节再动代码**，Report 型是完整参照实现；只是往现有类型加源则不适用本条，照上面各类型的套路走。
- **Playwright 必须保留在主项目中**：部分特殊页面需要浏览器渲染抓取（`Spider/News/Web/Eastmoney/Playwright/`），生产新闻链路是纯 HTTP（`Common/Http/HttpClientTools.CreateByHost` 伪装 Chrome 头），两者分工明确；该命名空间下调用库入口需写全限定 `Microsoft.Playwright.Playwright`（避免与命名空间撞名）。反爬验证的浏览器策略（`Spider/Verify/Solver/Browser/`）只在被拦截时按需启动浏览器，平时不参与抓取。
- **Web 控制台（`src/k-spider-web` + 仓库根 `web/`）与爬虫进程只通过库通信**，主程序不开 HTTP 端口：
  - 运行状态上行：`JobRuntimeListener`（每次执行写执行结果）+ `NodeStateJob`（3 秒刷调度态）+ `NewsCheckJob`（5 分钟节点快照）写 3 张系统表；Web 只读。
  - 控制指令下行：Web 写 `spider_job_command`（`trigger`/`pause`/`resume`）→ `NodeStateJob` 轮询消费（约 3 秒），异步受理、结果回写指令行。**`NodeStateJob` 自身不许暂停**（代码里已拒绝该指令：它停了没人消费恢复指令）。
  - 新增 API 端点：`src/k-spider-web/Api/` 一域一文件写 `MapXxxApis` 扩展 → `WebEndpoints.MapSpiderApis` 加一行；查询逻辑在 `Query/` 对应服务（DI Singleton，只读），分页信封 `PageResult<T>`，分析聚合强制时间窗 ≤31 天。
  - 新增前端页面：`web/src/pages/` 加页面 → `App.tsx` 的 `NAV_ITEMS` 与 `Routes` 各加一项；请求一律走 `api/hooks.ts` 的 TanStack Query 封装（轮询型 hook 带 `refetchInterval`）；改完必须 `bash scripts/build-web.sh` 才会进 `wwwroot`。
  - **前端 E2E**（`web/tests/`，Playwright + `@playwright/test`）：业务 API 由 `tests/helpers/mock-api.ts` 用 `page.route` 拦截成 `tests/fixtures/` 里的夹具——离线确定、进 CI；真实后端用例写进 `tests/live.spec.ts`（`@live` 前缀，默认跳过，`LIVE_E2E=1 pnpm test:e2e:live` 执行），与后端"夹具回归 + Live 连通性"同款分层。**新增 API 端点时同步补夹具**——页面请求了未登记的接口，测试会直接报"未 mock 的接口"；服务端 DTO 改字段时夹具跟着改（夹具约定见 `web/tests/fixtures/README.md`）。浏览器版本与主项目 .NET Playwright 对齐（共用 `~/Library/Caches/ms-playwright` 的 chromium-1243，零额外下载）。
  - Job 想在控制台带业务摘要：`Execute` 末尾给基类属性 `RunSummary` 赋值（如"新增列表 12"），listener 自动上报。
- **新增反爬验证识别方式 / 通过手段**（模块见 [docs/anti-bot-verification.md](docs/anti-bot-verification.md)）：
  - 识别方式：`VerificationKind` 按需加值 → 写一个 `IVerificationDetector` 实现（**纯判定、不联网**，否则没法离线回归）→ 在 `VerificationRegistry.DetectorList` 按"特征越具体越靠前"加一行 → 补夹具与用例。
  - 通过手段：写一个 `IVerificationSolver` 实现（声明 `Kinds` 与 `Cost`，越小越先试）→ 在 `VerificationRegistry.SolverList` 加一行。人机确认类（滑块 / 图形 / 短信）**不要**加进 `VerificationPolicy.DefaultAllowedKinds`，由部署方按源显式放开。
  - 财联社是当前唯一按源放开滑块的源（`Program.cs` 里 `SetPolicy`）：`/detail/*` 详情页被阿里云 WAF 人机验证拦截（2026-09-25 实测，纯 HTTP 一律拿到 200 + 滑块页，**间歇性出现**——同一 IP 前一天全拦、次日放行），`BrowserSliderSolver` 先等挑战脚本自动放行、等不到再拖滑块；挑战页样本 `verify_aliyun_waf_captcha.html`。
  - 抓取层一律走 `VerifiedHttp.GetStringAsync` / `SendStringAsync`（需要自定义请求头时传**请求工厂**，`HttpRequestMessage` 不能重发）；不要直接调 `HttpClientTools`。
  - **误判比漏判贵**（误判 = 白起一次浏览器 + 这条数据失败 + 该主机进冷却），所以：内容型识别器只对 HTML 生效（各源正常响应是 JSON）、中文泛词（如"验证码"）必须配合表单元素才算数、风控提示只在响应 envelope（对象层级）上找而**不进业务数据数组**（一条含 `risk` 字样的快讯就能让整源误判）、词表不放中性词（裸 `risk`/`verify`、`校验`/`请求异常`）、"签名错误"不归风控（它是源改版，有独立处理路径）。
  - `VerificationPolicy.WithKinds` 是**在默认放行集上追加**；只想收窄就直接构造 `new VerificationPolicy { AllowedKinds = [...] }` —— 别指望它替换（会把该源的 Cloudflare / JS 门禁自动通过静默关掉）。
  - 限流（`RateLimited`）与载荷级风控（`RiskControl`）没有任何自动手段，唯一合理响应是退避：不要加进 `DefaultAllowedKinds`，也不要让它们升级成"需要人工介入"（见 `VerificationKindTraits.ResolvesByWaiting`）。

## 配置

- 配置绑定走 `DatabaseOptions`（IOptions），环境由 `DOTNET_ENVIRONMENT` 选择（Development/Test/Production，Host 标准，未设置默认 Development）。
- 读取优先级：`appsettings.json` → `appsettings.{环境}.json` → `K_SPIDER__` 前缀环境变量 → 代码默认值（`DatabaseOptions.DefaultConnectionString`）。

| 配置键 | 环境变量 | 说明 |
|---|---|---|
| `Database:ConnectionString` | `K_SPIDER__DATABASE__CONNECTIONSTRING` | 主程序 PG 连接串（库 `k_script_spider`） |
| —（仅环境变量） | `K_SPIDER_REMOTE__CONNECTIONSTRING` | k-spider-sync 远端库 |
| —（仅环境变量） | `K_SPIDER_LOCAL__CONNECTIONSTRING` | k-spider-sync 本地库 |

配置值为空 = 使用代码内默认值（本地开发库）。**凭据红线**：test/prod 连接串不入库，由部署方环境变量注入。测试不读取任何配置。

## 数据库

- 库名 `k_script_spider`，10 张表的完整 DDL 在 `db/k_script_spider.sql`（DBX 导出 + 增量演进段），新环境用它初始化。
- 唯一键约定：新闻三表以 `news_url` 去重，图片表以 `image_resource_url`，快讯表以 `(from_media, news_url)`，榜单表以 `(ranking_type, trade_date, row_key)`，公告表以 `announcement_id`，研报表以 `info_code`。
- 表**不是** CodeFirst 管理；`Data/Devtools/PgDevelop.cs` 可从库反向重新生成 SqlSugar 实体（DbFirst）。
- 所有表有 `update_time_func()` 触发器自动刷新 `update_time`，upsert 时 ignore 这三列即可。
- 轮询热路径依赖部分索引 `idx_news_list_download_status`（启动时自动创建）。

## 已知坑（改代码前必读）

1. **SqlSugar `ToSqlString` 默认 200 行限制**：批量生成 INSERT 会自动分页。`SpiderNewsBatchDao`（Data/）里有绕过实现（`IsNoPage = true` + 手拼 `ON CONFLICT`），写批量 SQL 时照抄它。**单条数据时 ToSqlString 以 `returning "id"` 结尾、多条以 `;` 结尾**（均无法直接拼 `ON CONFLICT`，2026-09 金十财料栏目单行批次实测触发过 returning 形态），去尾统一用 `SpiderNewsBatchDao.TrimInsertSqlTail`（两种尾巴都剥）；不要用 `[..LastIndexOf(';')]` 截断——单条时 LastIndexOf 返回 -1 会抛参数越界（有单测锁定该边界）。
2. **手拼 `ON CONFLICT (列)` 要求该列上有唯一索引 / 约束**，缺失时 PostgreSQL 整批报错（`there is no unique or exclusion constraint matching the ON CONFLICT specification`）。列表任务里列表行与原始内容同事务，异常会一起回滚，表现为"该源一行数据都进不来、其它源正常"。`Pg.CheckBatchUpsertUniqueIndexes` 在启动时会显式告警；缺约束时按 docs/operations.md 的巡检 SQL 核对并手工补建（批量 upsert 依赖四张表的唯一列）。
3. 本地无 PG 时运行主程序，各 Job 每轮抛连接异常并按间隔重试，属预期噪音；验证代码改动用 `dotnet test`，不要靠运行主程序判断对错。
4. **配置根固定为程序目录**（`Program.cs` 显式设 `ContentRootPath = AppContext.BaseDirectory`）：appsettings 全在项目目录，若按 Host 默认用工作目录找配置，从仓库根执行 `dotnet run --project` 会静默回退代码默认连接串连到本地库（2026-09 检验时踩过，表现为全部 Job 报 `3D000 数据库不存在`）；修复后任意目录运行都能正确装载配置。
5. 时间格式强绑定：列表接口 `yyyy-MM-dd HH:mm:ss`、详情接口 `yyyy/MM/dd HH:mm:ss`（`DfListInfo`/`DfContentInfo` 的 `To*Model()` 各自使用 `TimeTools` 常量），格式不匹配会抛 `FormatException`。
6. `Data/Devtools/`（DbFirst 生成器）是开发期工具，不参与生产链路。
7. 主项目 `Lark/` 下的飞书 SDK 当前无调用方（推送功能已移除），保留备用；重新启用时凭据走 `DatabaseOptions` 同款 Options 模式加回配置节。
8. `spider_news_content_origin` 存整篇原始 JSON、图片表只增不删，长期运行需自行归档清理（原 `db/optimization.sql` 已移除，清理 SQL 可从 git 历史找回）。
9. 命名空间刻意用复数 `KSpider.Exceptions`（避开与 `System.Exception` 类型撞名）；`KSpider.Spider.News.Web.Eastmoney.Playwright` 下调用 Playwright 库同理需全限定。
10. 环境变量前缀是 `K_SPIDER__`（含双下划线）：`K_SPIDER__DATABASE__CONNECTIONSTRING` → `Database:ConnectionString`。此前缀写错会静默失效（曾踩过）。
11. **财联社签名绑定前端版本号**：`sign = MD5(SHA1(参数按 key 升序拼接))`，其中 `sv`（前端版本号，当前 8.7.9）写死在 `ClsNewsResource`。财联社升级前端后接口会开始返回 `errno 10012 签名错误`（`NewsCheckJob` 探测日志会暴露），更新 `Sv` 即可；`ClsNewsSpiderTest.SignMatchesVerifiedVector` 锁了一组实测向量，改算法必须同步该用例。
12. **财联社电报列表 `rn` 超过 50 会静默返回空数组**（errno 仍为 0，看起来像"没有新闻"），已在 `ClsNewsResource.MaxPageSize` 钳制。另外它的时间游标是**严格小于**语义，`NextCursor` 取本页最老一条 ctime + 1，否则同一秒内的其它条目会被永久跳过（边界条目重复由 `ON CONFLICT DO NOTHING` 吸收）。
13. **金十快讯接口必须带 `x-app-id` / `x-version` 头**，缺失直接 502（值写在 `Jin10NewsResource`，被拒时对照网页端请求更新）。它的 `max_time` 游标是**含边界**语义（`NextCursor` 直接用最老一条时间，边界重复由去重吸收）；约 20% 条目是 PLUS 专享，正文为空、只有 `vip_title` 可用（实现已兜底，详见 docs/news-pipeline.md）。
14. 六个快讯源（财联社/新浪/见闻/金十/同花顺/格隆汇）走独立的 `FlashNewsJob` 管线写 `spider_flash_news`（15 秒一轮、拉到即终态），与网页抓取型管线（三张表 + 状态机）完全分离；不要把快讯源注册进 `NewsSpiderRegistry`。加新快讯源时照抄 `Spider/News/Flash/Cls/` 或 `Spider/News/Flash/Ths/` 的结构（时间游标 / 页码游标 / 接口自带游标三种先例）；格隆汇的教训——**带 timestamp 类防缓存参数的接口，侦察时必须带上再判断语义**，否则会拿到服务端缓存的固定批误判"参数不生效"。
15. **财联社一个网站两种管线**：电报在快讯注册表，文章频道在网页注册表（`Spider/News/Web/Cls/`），**共用 `ClsMedia=2`**——枚举标识"网站来源"，管线归属由注册表决定；文章与电报共用一套全局 id（一个 id 只属一种内容类型），`/detail/{id}` 落不同表不会撞键。文章频道的**翻页游标不保证单调**（列表按 SortScore 编辑混排、服务端不按 rn 裁页），末页只以空页为准，重叠靠入库去重吸收；`source` 可空（回退"财联社"）；品见已接入（`ClsArticleSpider` 第二列表族，伪栏目号 `pinjian` + category 115，拼装流整页即全量无翻页，文章 id 同空间复用详情流程）；招财号未接入（机构 UGC 平台，网页侧无确认可用的匿名列表接口）。侦察与接入方法论见 docs/web-source-playbook.md。
16. **反爬验证的浏览器策略依赖 Chromium**：目标机需执行一次 `playwright install chromium`（步骤见 docs/operations.md 的部署章节）。未安装时 `BrowserChallengeSolver` 失败并在日志里给出该提示，**识别与告警仍然生效**、抓取链路不受影响——别把它当成"验证模块坏了"。
17. **会话回放走 `HttpClientTools.ApplyCookies`（写进 handler 的 CookieContainer），不要给请求手动加 `Cookie` 头**：手动头会与容器里的同名旧值拼成两份同名 cookie（实测 `sid=SOLVED; sid=STALE`，服务端取哪份未定义，重复 cookie 本身也是注入指纹）。回放只带 cookie、**不带 UA** —— 请求头由 `HttpClientTools` 统一伪装（`DisguiseUserAgent` 常量），浏览器侧必须复用同一串 UA，否则指纹不一致会被再拦一次。注意 `CreateByHost` 的 CookieContainer 是进程内共享的，`ApplyCookies` 会按名清理并覆盖同名项，改这段要连带跑 `VerifiedHttpTest`。
18. **验证失败的代价**：`VerificationRequiredException` 按"这条数据失败"处理（消耗 `fail_count`），且该主机进冷却期（默认 10 分钟；429 按源站 `Retry-After` 退避）。因此识别器误判的表现是"某个源整批数据失败 + 冷却期内不再尝试"，改识别器特征后务必跑 `VerificationDetectorTest` 的 `RealBusinessPayloadIsNotChallenge` 用例（真实响应全量不误判）。
19. **SqlSugar `Ado.ExecuteCommand(sql, obj)` 的参数对象必须匿名对象**（或 `SugarParameter[]`）：直接传 record / 具名类实例会抛"parameter format is wrong"（`SystemStatusDao` 已踩，传入一律 `new { ... }` 平铺属性）。另外手写 SQL 里给 PG 的 `timestamp without time zone` 列传 `DateTime.Now` 参数时，Npgsql 会按 Kind=Local 编成 timestamptz 引发类型不匹配——构造参数统一 `DateTime.SpecifyKind(value, DateTimeKind.Unspecified)`（分析层 `NormalizeWindow` 已处理）。
20. **`k-spider-web` 开发模式的静态文件伺服自项目目录**（`staticwebassets.runtime.json` 清单），`bin` 下没有 `wwwroot` 物理目录：判断"前端产物是否存在"必须用 `WebRootFileProvider.GetFileInfo("index.html").Exists`，用 `WebRootPath` 拼 `File.Exists` 在 `dotnet run` 下恒为 false（已踩）；发布产物则两者一致。
21. **手动触发类指令是异步的**：`POST /api/jobs/{name}/{action}` 返回 202 只代表已受理，生效靠 `NodeStateJob` 3 秒轮询消费；爬虫进程停着时指令停在 `pending`，进程恢复后被消费。自动化脚本判断生效要轮询 `/api/jobs/commands` 或 `/api/status/jobs`，不要立刻断言。
22. **新浪财经一个网站两种管线**（与财联社同款先例）：7x24 快讯在快讯注册表，文章源在网页注册表（`Spider/News/Web/Sina/`），**共用 `SinaMedia=3`**，文章 category 用 202-223 段。文章源的三个实测边界：① 滚动接口 `num>50` 被静默钳到 50；② 栏目滚动页 `roll/c/{cid}.shtml` **整页即全量、没有翻页**（`?page=` 只跳回首页），页面时间 `(09月28日 22:15)` 无年份，年份从条目 URL 路径 `/yyyy-MM-dd/doc-` 补全，无日期路径条目跳过（实测 200 条里 14 条）；③ 频道页上挂着一批已下线的死链 cid（230808/264124/40811 等，`roll/c` 下一律 404），**栏目清单以实测存活为准，不要照抄频道页链接**。详情页整页 HTML 存 origin（`NewsContentOriginType.Html`），文末 `appendQr_wrap` 推广二维码块解析时整体跳过。
23. **华尔街见闻文章接口的两个静默坑**：① 列表 `limit>30` 时接口返回 `data:""`（code 仍 20000，不是钳制而是无数据），`WscnArticleResource.MaxPageSize` 已钳制，解析层对"data 非对象"显式报错而非当空页；② 详情接口 `extract` 参数**必填**（0=带图 HTML / 1=纯文本），缺失报 `60327 "extract 不正确"`。文章流只配一个全量栏目（`global` 标签覆盖 119/120，多栏目必重复抓），分类号逐条从 `categories` 多标签按优先级推断；付费文（`is_priced`，约 7%）正文截断仍入库，付费标记写两表的 `is_paid` 列（列表侧来自接口、origin 侧从详情回读），付费条目 uri 的 `?layout=` 查询串入库前剥掉。
24. **同一台机只跑一个主程序实例**：多实例没有任何互斥防护——连接数与源站请求按实例数叠加打满远端库，且 `node_id` 取主机名，多实例会互相覆盖 `spider_node_status` 的调度态、抢占消费 `spider_job_command` 指令，表现为状态闪烁、指令"已 done 但没生效"（2026-09 自测踩过：残留 4 个实例把库打饱和，接口层表现为莫名超时）。部署用 `deploy/` 的 systemd 单元天然单例；本机调试收进程用**精确进程路径** `pkill -f "net10.0/k-spider-dotnet$"`，不要 `pkill -f k-spider-dotnet`（会把路径里同样含仓库名的 `k-spider-web` 一起误杀）。
25. **金十文章接口的四个实测坑**：① 列表（`reference-api.jin10.com/reference`）与详情（`/reference/getOne`）是**两套 `x-app-id`**（`irINJPgCgrndSp0F` / `arU9WZF7TC9m7nWn`），两套头不通用、缺头一律 502，值写死在 `Jin10ArticleResource`；② 列表 `page_size` 上限 100，超限返回**显式 400**（`value must be inside range [1, 100]`，不是静默钳制）；③ 付费专享条目（`vip/super_vip/elite_vip` 任一非零）匿名请求详情时 `content` **整体为空**（不是截断），按"无详情数据不接入"在列表层跳过，VIP 专区（nav 77）/精英专区（nav 84）两栏目因此未接；④ 综合流（nav 28）**不是全量超集**（早餐/财料栏目在综合流前 500 条命中不足一成），必须多栏目接入，栏目间约九成重叠由 `news_url` 去重吸收；末页判断用**原始条数**（被跳过的付费/视频条目不参与），否则满页会被误判成末页提前停翻。

## 提交规范

### 分支

- `main`：默认主分支，始终可构建、测试全绿。
- 功能分支 `feat/<简述>`，修复分支 `fix/<简述>`，合并后删除。

### 提交信息（Conventional Commits）

```
<type>(<scope>?): <中文描述>
```

| type | 用途 |
| --- | --- |
| feat | 新功能（scope 标注模块，如 `feat(news): …`、`feat(sync): …`） |
| fix | 缺陷修复 |
| docs | 仅文档变更 |
| refactor | 重构（不改行为） |
| test | 仅测试变更 |
| chore | 构建/工具/依赖变更（如 `chore(deps): …`） |

示例：

```
feat(news): 新闻列表任务增加自适应翻页
fix(sync): 修复本地库缺列时同步插入失败
refactor: 合并公共库到主项目
```

### 提交前自查清单

```bash
./scripts/verify.sh    # 文档链接检查 + 构建 0 错误 0 警告 + 离线测试全通过
git status             # 无产物文件混入（bin/obj/.idea 等）
```

文档同步：结构性 / 约定性变更须同步更新 README.md、AGENTS.md 与 `docs/` 对应章节。
`docs/` 是面向维护者的设计文档（设计原则 / 架构 / 新闻管线 / 反爬验证 / 数据模型 / 运维手册 / 网页型源接入规范），
其中 [docs/README.md](docs/README.md) 有"代码变更 → 必须更新哪份文档"的映射表，改代码前先扫一眼那张表。

凭据红线：不向仓库提交真实凭据；环境相关值进配置/Options，由部署方用环境变量覆盖。
