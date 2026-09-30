# k-spider-dotnet

7x24 小时金融数据爬虫：抓取财经新闻快讯（当前接入东方财富 35 个栏目、财联社文章频道 13 个栏目与品见拼装流、财联社电报、新浪财经 7x24、华尔街见闻 live、金十数据快讯、同花顺 7x24、格隆汇 live，多源框架可扩展）与研报（东财研报中心个股/行业/宏观三类）、盘面榜单（东财数据中心龙虎榜/大宗/两融）、公告（巨潮资讯沪深京法定披露分类白名单），存入 PostgreSQL，支持远端到本地的增量数据同步。

## 功能

- **网页新闻管线**（三段接力，状态机驱动，失败自动重试）：列表发现 → 原始内容下载 → 结构化解析（段落/图片/列表/表格），按 `news_url` 全局去重，当前接入东方财富 35 个栏目、财联社文章频道 13 个栏目与品见拼装流、同花顺文章 10 个栏目、每日经济新闻 3 个栏目。
- **实时快讯管线**（15 秒一轮，独立于网页管线）：财联社电报、新浪财经 7x24、华尔街见闻 live（7 频道）、金十数据快讯、同花顺 7x24、格隆汇 live——"列表即全文"型源，拉到即终态直写 `spider_flash_news`（含重要度/关联标的），发布到入库最坏延迟约 16 秒；`NewsCheckJob` 按源监控实时性滞后。
- **研报管线**（5 分钟一轮，第三条管线）：东财研报中心个股/行业/宏观三类——列表接口即全部结构化元数据（评级/个股/机构/目标价/盈利预测），直写 `spider_research_report` 按 `info_code` 去重；摘要正文由详情页二段回填，失败计数控重试。
- **盘面榜单管线**（30 分钟一轮，第四条管线）：东财数据中心龙虎榜/大宗交易/两融——交易级结构化数值快照，直写 `spider_ranking` 按（类型/交易日/自然键）去重，发布即终态。
- **公告管线**（10 分钟一轮，第五条管线）：巨潮资讯沪深京法定披露——分类白名单（定期报告/业绩/股权激励/风险提示等 10 类）文档元数据（证券+标题+PDF 链接），直写 `spider_announcement`，披露即终态。
- **反爬验证识别与通过**（`Spider/Verify/`）：所有抓取请求统一走 `VerifiedHttp`，被 JS 门禁 / Cloudflare 挑战 / 频率限制 / 载荷级风控拦住时自动识别并处理——JS 与 Cloudflare 类挑战用浏览器过掉并把 cookie 回放给 HTTP 链路，过不了的（滑块/图形/短信验证码、限流）明确告警而不是悄悄返回空数据。识别器与通过策略都是注册表扩展，新增验证方式只需加一个类加一行注册（见 [docs/anti-bot-verification.md](docs/anti-bot-verification.md)）。
- **健康检查**：各源栏目接口可用性探测 + 分源流水线积压/失败统计 + 反爬验证阻塞告警 + 研报实时性监控（每 5 分钟）。
- **Web 控制台**（独立进程 `k-spider-web`，React + TypeScript）：总览（任务状态/管线积压/快讯实时性/反爬告警）、新闻与快讯查询、数据分析（入库趋势/分布/关键词）、任务手动触发/暂停/恢复；爬虫运行状态与控制指令经 3 张系统表跨进程传递，与采集进程完全解耦、可分开部署。
- **数据搬运**：独立进程 `k-spider-sync` 将远端库的 8 张表（4 张网页新闻 + 快讯表 + 研报表 + 榜单表 + 公告表）增量同步到本地（新行按 Id 增量插入；已有行按 `update_time` 水位同步更新，使远端状态流转/内容修正传播到本地；水位持久化在本地 `sync_transfer_watermark` 表，SqlSugar，单表失败不阻断其余表）。

## 解决方案结构

```
k-spider-dotnet/                 仓库根 = 解决方案根
├── db/                          建库 DDL（k_script_spider.sql）
├── deploy/                      systemd 服务模板（爬虫 / Web 控制台）
├── docs/                        设计文档（设计原则 / 架构 / 新闻管线 / 反爬验证 / 数据模型 / 运维手册）
├── scripts/                     verify.sh 一键验证 / build-web.sh 前端构建
├── .github/workflows/ci.yml     CI（push/PR 构建测试，含前端构建）
├── Directory.Build.props        公共构建属性（net10.0 / Nullable 等）
├── Directory.Packages.props     中央包版本管理（CPM）
├── web/                         Web 控制台前端源码（React + TS + Vite + pnpm）
└── src/
    ├── k-spider-dotnet/         主爬虫（Host + DI + Quartz 托管调度 , 含 Playwright 特殊页面抓取）
    ├── k-spider-sync/           远端 PG → 本地 PG 增量同步（复用主项目实体）
    ├── k-spider-web/            Web 控制台服务端（只读查询/分析 API + 前端静态页伺服）
    └── k-spider-test/           MSTest 单元测试（离线可跑）
```

> .NET 10 / Generic Host + 依赖注入 + Options 模式；ORM 统一 SqlSugar，实体只维护一套（主项目 `Model/`）；命名空间 PascalCase（`KSpider.*`）。

## 快速开始

环境要求：.NET SDK 10、PostgreSQL 15+。

```bash
# 1. 初始化数据库（表结构 + 触发器 + 索引）
psql -h 127.0.0.1 -U postgres -f db/k_script_spider.sql k_script_spider

# 2. 配置 : 默认 Development 环境连本机 127.0.0.1:5432/k_script_spider
#    编辑 src/k-spider-dotnet/appsettings.Development.json 或用环境变量覆盖

# 3. 构建 + 测试
./scripts/verify.sh

# 4. 运行
dotnet run --project src/k-spider-dotnet
```

## 多环境配置

| 文件 | 环境 | 说明 |
|---|---|---|
| `appsettings.json` | 公共 | 基础默认值 |
| `appsettings.Development.json` | Development（默认） | 本地 PostgreSQL |
| `appsettings.Test.json` | Test | 测试环境 PG（连接串留空，部署注入） |
| `appsettings.Production.json` | Production | 线上 PG（连接串留空，部署注入） |

- **环境选择**：`DOTNET_ENVIRONMENT=Development|Test|Production`（Host 标准，未设置时本地默认 Development）
- **读取优先级**：基础文件 → 环境文件 → `K_SPIDER__` 前缀环境变量（如 `K_SPIDER__DATABASE__CONNECTIONSTRING`）→ 代码默认值
- **打包只带目标环境文件**：`dotnet publish ... -p:SpiderEnvironment=Production`，产物不含其他环境文件与真实凭据
- **凭据红线**：test/prod 连接串不入库，由部署方用环境变量注入

## 定时任务一览

| Job | 间隔 | 说明 | 默认 |
|---|---|---|---|
| `FlashNewsJob` | 15 秒 | 各快讯源并行拉取，直写 `spider_flash_news`（拉到即终态） | 启用 |
| `NewsListJob` | 2 分钟 | 网页型源抓列表，批量 ON CONFLICT 写入，自适应翻页 | 启用 |
| `NewsContentOriginJob` | 3 秒 | 按源分发下载原始内容（全源 FIFO，失败重试 ≤3 次） | 启用 |
| `NewsContentJob` | 1 分钟 | 按源分发解析原始内容为结构化内容（失败重试 ≤3 次） | 启用 |
| `NewsCheckJob` | 5 分钟 | 各源栏目接口探测 + 分源积压统计 + 反爬验证阻塞告警 + 研报实时性 + 节点快照上报 | 启用 |
| `ResearchReportJob` | 5 分钟 | 东财研报三类列表直写 `spider_research_report` + 摘要由详情页回填 | 启用 |
| `RankingJob` | 30 分钟 | 东财数据中心龙虎榜/大宗/两融直写 `spider_ranking`（发布即终态） | 启用 |
| `AnnouncementJob` | 10 分钟 | 巨潮资讯公告（分类白名单）直写 `spider_announcement`（披露即终态） | 启用 |
| `NodeStateJob` | 3 秒 | 状态通道：任务调度态上报 + Web 控制台指令消费（不可暂停） | 启用 |
| `TransferSpiderDataJob`（sync） | 2 分钟 | 远端 → 本地增量同步 | 启用 |

任务的启用/停用：`src/k-spider-dotnet/Program.cs` 的 `AddSpiderJobs` 中注释控制。Ctrl+C / SIGTERM 触发优雅停机（等待在跑任务完成）。

## 数据流

```
网页抓取型 ( 东财 )                     实时快讯型 ( 财联社/新浪/见闻/金十 )
────────────────────────              ────────────────────────
各源列表 API ──NewsListJob──▶          快讯 API ──FlashNewsJob(15秒)──▶ spider_flash_news
spider_news_list (status=0)            ( 完整记录 : 标题/正文/标签/重要度/关联标的/原始JSON )
       │ NewsContentOriginJob (0→3)    拉到即终态 , 无状态机 ; 修正随下一轮回填
       ▼
spider_news_content_origin
       │ NewsContentJob (3→1)
       ▼
spider_news_content + spider_news_image_list

研报 ( 东财研报中心 , 第三管线 )            盘面榜单 ( 东财数据中心 , 第四管线 )
────────────────────────                  ────────────────────────
研报列表 API ──ResearchReportJob(5分钟)──▶  榜单 API ──RankingJob(30分钟)──▶ spider_ranking
spider_research_report                     ( 行即数值 : 龙虎榜/大宗/两融 , 类型+日期+行键去重 )
( 结构化元数据 : 评级/个股/机构/… )              拉到即终态 , 无状态机
       │ 摘要回填 ( 详情页 SSR )
       ▼
summary 填充 ( 拉到即终态 , 无状态机 )

公告 ( 巨潮资讯 , 第五管线 )
────────────────────────
公告查询 API ──AnnouncementJob(10分钟)──▶ spider_announcement
( 文档元数据 : 证券+标题+分类+PDF 链接 , announcement_id 去重 )
拉到即终态 , 无状态机
```

状态机：`0 未下载 → 3 已下载原始 → 1 已解析详情`，失败态 `2 / 4` 在 `fail_count < 3` 时自动重试。两个下载/解析 Job 按行上的 `from_media` 分发到对应源实现（注册表 `NewsSpiderRegistry`）。快讯型源（列表即全文）在列表阶段就直接写成 `status=3`，不经过下载 Job。研报、盘面榜单、公告走独立管线与独立表，不入新闻三表。8 张数据表完整 DDL 见 [db/k_script_spider.sql](db/k_script_spider.sql)。

## 部署（Linux）

```bash
dotnet publish src/k-spider-dotnet/k-spider-dotnet.csproj -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production
```

推荐 **systemd**（自动重启 + journald 日志轮转）：模板见 [deploy/k-spider-dotnet.service](deploy/k-spider-dotnet.service)，
拷贝到 `/etc/systemd/system/` 后 `systemctl enable --now k-spider-dotnet`。轻量场景可用产物目录内的 `run.sh`。

Web 控制台独立部署：先 `bash scripts/build-web.sh` 构建前端，再
`dotnet publish src/k-spider-web/k-spider-web.csproj -c Release -r linux-x64 --self-contained -p:SpiderEnvironment=Production`
（默认 `http://localhost:5800`，连接串用 `K_SPIDER__DATABASE__CONNECTIONSTRING` 注入与主程序同库，详见 [docs/operations.md](docs/operations.md)）。

## AI 辅助开发

- **[AGENTS.md](AGENTS.md)**：AI 代理操作手册（结构、命令、约定、扩展套路、已知坑），ZCode / Claude Code / Cursor 自动读取。
- **[docs/](docs/README.md)**：面向维护者的设计文档（[设计原则](docs/principles.md) / [架构](docs/architecture.md) / [新闻管线](docs/news-pipeline.md) / [反爬验证](docs/anti-bot-verification.md) / [数据模型](docs/data-model.md) / [运维手册](docs/operations.md) / [网页型源接入规范](docs/web-source-playbook.md)），含"代码变更 → 必须更新哪份文档"的映射。
- **全部测试**：`dotnet test src/k-spider-test/k-spider-test.csproj` —— 含真实接口连通性用例（直接请求两源线上 URL，验证能调通、能拿到数据集、能解析；断网时自动跳过）。
- **离线测试**：`dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory!=Live"` 不依赖网络与数据库，基于 `TestData/` 里的真实响应夹具做解析回归。
- **连通性排障**：`dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory=Live"`（源改版、财联社签名失效时先跑它）。
- **一键验证**：`./scripts/verify.sh` = 文档链接检查 + 前端构建与 E2E（mock API，离线确定）+ 构建 + 离线测试（CI 同款）。
- **前端测试**：`cd web && pnpm test:e2e`（Playwright + mock 夹具）；`pnpm test:e2e:live` 验真实后端（需先起 k-spider-web）。
