# AGENTS.md — AI 代理开发指南

本文件面向 AI 编码代理（ZCode / Claude Code / Cursor / Copilot Agent 等）。开始任何修改前请先读完本文件。

## 项目是什么

7x24 小时金融数据爬虫：从东方财富抓取新闻快讯（33 个栏目）与股票 Level1 日线快照（A股/港股），存入 PostgreSQL。解决方案共 3 个项目，目标框架 net10.0，ORM 统一使用 SqlSugar，基于 Generic Host + 依赖注入 + Options 模式。

## 常用命令

```bash
# 构建整个解决方案（改完代码必须跑 , 当前零警告零错误 , 不要引入新告警）
dotnet build k-spider-dotnet.sln

# 运行全部测试（离线可跑，不依赖网络与数据库，随时可执行）
dotnet test src/k-spider-test/k-spider-test.csproj

# 一键验证（= 上面两条命令 , CI 同款）
./scripts/verify.sh

# 本地运行主程序（需要可用的 PostgreSQL , 默认 Development 环境）
dotnet run --project src/k-spider-dotnet

# 本地切换环境调试（构建与运行参数对齐 , 确保对应环境配置文件被拷贝）
DOTNET_ENVIRONMENT=Test dotnet run --project src/k-spider-dotnet -p:SpiderEnvironment=Test

# Linux 自包含发布（部署用 , 产物只带目标环境配置）
dotnet publish src/k-spider-dotnet/k-spider-dotnet.csproj -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

环境要求：.NET SDK 10（`global.json` 锁 9.0.0 但 `rollForward: latestMajor` 会自动使用 10.x，已禁用预览版）。

## 解决方案结构

仓库根 = 解决方案根，三个项目位于 `src/` 下：

| 项目 | 类型 | 职责 |
|---|---|---|
| `src/k-spider-dotnet` | Exe | 主爬虫：新闻/股票抓取、解析、落库、Quartz 托管调度；含 Playwright（特殊页面抓取与栏目自检）与飞书 SDK（`Lark/`，当前无调用方） |
| `src/k-spider-sync` | Exe | 数据搬运：SqlSugar 把远端 PG 的 4 张新闻表按 Id 增量同步到本地，引用主项目实体 |
| `src/k-spider-test` | 类库 | MSTest 单元测试（全部离线） |

依赖方向：`k-spider-sync → k-spider-dotnet`（复用 `Model/` 实体、`Data/Pg` 连接工厂与 `Job/SpiderJob` 基类）；test 引用主项目。**实体只有一套**（主项目 `Model/`）。

主项目目录（命名空间 PascalCase 风格 `KSpider.*`；代码在项目根下）：

```
src/k-spider-dotnet/
├── Program.cs                 # Host 入口 : DI 注册 + AddSpiderJobs 集中调度注册 + 优雅停机
├── appsettings*.json          # 多环境配置 ( Development / Test / Production )
├── Config/                    # DatabaseOptions ( IOptions 绑定 )
├── Data/                      # Pg 连接工厂 + DAO ( DI Singleton ) + Devtools/ ( 开发期工具 )
├── Model/                     # SqlSugar 实体（DbFirst 生成，带 Model 后缀）
├── Job/                       # SpiderJob 基类 + News/ + Check/ + Stock/ 全部定时任务
├── Spider/                    # 爬虫实现：DataResource.cs（枚举/栏目分类）+ DfNews/ + DfStock/ + DfResearchReport/
├── Tool/                      # Html/（HtmlTools、HtmlTagName）+ Http/（HttpClient 伪装头、URL 工具）
├── Logger/ Json/ Collection/ Strings/ Time/   # 公共工具
├── Lark/                      # 飞书 SDK（当前无调用方，保留备用）
└── Exceptions/                # 异常体系（DownloadHttpException 族、KDbException）
```

仓库根：`Directory.Build.props`（公共属性）、`Directory.Packages.props`（**CPM 包版本中心**，加包两处改：这里加版本 + csproj 加无版本引用）、`db/`（DDL 与优化 SQL）、`deploy/`（systemd 模板）、`.github/workflows/ci.yml`（CI）。

## 核心数据流（改动前必须理解）

新闻管线由三个接力 Job 组成，通过 `spider_news_list.download_status_code` 状态机驱动：

```
DfNewsListJob (每2分钟, Job/News/)
  逐栏目抓列表 (最多4页, 当前页无新URL即停止翻页)
  → 批量 ON CONFLICT DO NOTHING 写 spider_news_list (status=0)

DfNewsContentOriginJob (每3秒, 每轮200条, 按Id先进先出)
  取 status=0 或 (status=4 且 fail_count<3)
  → 抓文章原始 JSON → spider_news_content_origin → status=3 (失败→4, fail_count+1)

DfNewsContentJob (每1分钟, 按Id先进先出)
  取 status=3 或 (status=2 且 fail_count<3) , 批量预加载 origin
  → HtmlAgilityPack 规则解析 → spider_news_content / spider_news_image_list
  → status=1 (失败→2, fail_count+1 ; origin 缺失→退回4重新下载)

DfCheckJob (每5分钟, Job/Check/)
  33 栏目接口可用性探测 + 流水线各状态数量/最老待处理新闻统计
```

状态机：`0 未下载 → 3 已下载原始 → 1 已解析详情`，失败态 `2 解析失败 / 4 下载失败`。失败态在 `fail_count < NewsPipelineConst.MaxFailCount(3)` 时自动重试；数据库异常（KDbException）不消耗重试次数。

股票管线：`StockCnJob/StockHkJob`（Job/Stock/，Cron 工作日 20:00）读 `stock_cn_introduction` 股票池 → SSE 接口取当日 Level1 归档首帧（空数据跳过不落库）→ `stock_*_level1_archived_daily_origin`。

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
- 表结构变更：改主项目 `Model/` 实体 + `db/k-script-spider-datasource.sql` 两处；属于"增量演进"的列/索引可加到 `Pg.EnsureSpiderNewsListDbObjects()`（启动时幂等执行）。
- 新增 NuGet 包：`Directory.Packages.props` 加 `PackageVersion` + 项目 csproj 加无版本 `PackageReference`。
- 新增数据源爬虫参考 `Spider/DfStock/IStockSpider.cs` 的接口 + 模板方法模式；爬虫实现一律放 `Spider/` 目录。
- **Playwright 必须保留在主项目中**：部分特殊页面需要浏览器渲染抓取（`Spider/DfNews/Playwright/`），生产新闻链路是纯 HTTP（`Tool/Http/HttpClientTools.CreateByHost` 伪装 Chrome 头），两者分工明确；该命名空间下调用库入口需写全限定 `Microsoft.Playwright.Playwright`（避免与命名空间撞名）。

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

- 库名 `k_script_spider`，8 张表的完整 DDL 在 `db/k-script-spider-datasource.sql`（pg_dump 导出 + 增量演进段），新环境用它初始化；可选的索引/数据治理 SQL 在 `db/optimization.md`（附可执行版 `optimization.sql`，已实测幂等）。
- 唯一键约定：新闻三表以 `news_url` 去重，图片表以 `image_resource_url`，股票日线以 `(date, stock_id)`。
- 表**不是** CodeFirst 管理；`Data/Devtools/PgDevelop.cs` 可从库反向重新生成 SqlSugar 实体（DbFirst）。
- 所有表有 `update_time_func()` 触发器自动刷新 `update_time`，upsert 时 ignore 这三列即可。
- 轮询热路径依赖部分索引 `idx_news_list_download_status`（启动时自动创建）。

## 已知坑（改代码前必读）

1. **SqlSugar `ToSqlString` 默认 200 行限制**：批量生成 INSERT 会自动分页。`SpiderNewsBatchDao`（Data/）里有绕过实现（`IsNoPage = true` + 手拼 `ON CONFLICT`），写批量 SQL 时照抄它。
2. `Program.AddSpiderJobs` 里股票 Job 被注释停用（`StockCnJob/StockHkJob`）——这是有意的按需启用，不要顺手全部打开。
3. 本地无 PG 时运行主程序，各 Job 每轮抛连接异常并按间隔重试，属预期噪音；验证代码改动用 `dotnet test`，不要靠运行主程序判断对错。
4. 时间格式强绑定：列表接口 `yyyy-MM-dd HH:mm:ss`、详情接口 `yyyy/MM/dd HH:mm:ss`（`DfListInfo`/`DfContentInfo` 的 `To*Model()` 各自使用 `TimeTools` 常量），格式不匹配会抛 `FormatException`。
5. 股票 Job 的 `DateTime.Today` 依赖服务器时区，UTC 服务器上日期会错（systemd 模板已设 TZ=Asia/Shanghai，自管进程需确认）。
6. `Data/Devtools/`（DbFirst 生成器）与 `Spider/DfStock/Devtools/`（一次性下载工具）是开发期工具，不参与生产链路。
7. 主项目 `Lark/` 下的飞书 SDK 当前无调用方（推送功能已移除），保留备用；重新启用时凭据走 `DatabaseOptions` 同款 Options 模式加回配置节。
8. `spider_news_content_origin` 存整篇原始 JSON、图片表只增不删，长期运行用 `db/optimization.sql` 的清理段做归档（暂无自动保留策略）。
9. 命名空间刻意用复数 `KSpider.Exceptions`（避开与 `System.Exception` 类型撞名）；`KSpider.Spider.DfNews.Playwright` 下调用 Playwright 库同理需全限定。
10. 环境变量前缀是 `K_SPIDER__`（含双下划线）：`K_SPIDER__DATABASE__CONNECTIONSTRING` → `Database:ConnectionString`。此前缀写错会静默失效（曾踩过）。

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
| feat | 新功能（scope 标注模块，如 `feat(news): …`、`feat(stock): …`、`feat(sync): …`） |
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
./scripts/verify.sh    # 构建 0 错误 0 警告 + 全部测试通过
git status             # 无产物文件混入（bin/obj/.idea 等）
```

文档同步：结构性 / 约定性变更须同步更新 README.md 与 AGENTS.md 对应章节。

凭据红线：不向仓库提交真实凭据；环境相关值进配置/Options，由部署方用环境变量覆盖。
