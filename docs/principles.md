# 设计原则与工程约定

本文记录**为什么这样写代码**。具体操作步骤与命令见 [AGENTS.md](../AGENTS.md)，此处只讲原则及其落地方式。

## 一、三条最高优先级原则

1. **不过度设计、不过度封装**
   优先最简单可用的实现；不为假想的扩展点提前加抽象层（接口 / 泛型 / 继承层级）。
   判断标准：**出现第二个真实使用者时才提取抽象**。
   落地例子：新闻多源接口 `INewsSpider` 是在确定要接第二个源（财联社）时才从东财单源代码里抽出来的，而不是一开始就设计好；
   `Spider/Stock/IStockSpider.cs` 的模板方法模式同理（有两个交易所实现才抽）。

2. **可读性优先**
   代码首先是给人读的。注释 / 日志用中文；新代码与所在文件的既有风格保持一致；
   宁要直白的长代码，不要绕弯的短代码。
   落地例子：`NewsListJob` 里宁可显式写"翻到存量区间就停"的循环，也不抽 paging 策略类。

3. **兼顾性能，但不做没有测量依据的优化**
   热路径（轮询查询、批量写入、HTTP 调用）必须注意连接复用、批量操作与索引支撑；
   不为性能牺牲可读性。
   落地例子：列表任务批量 `ON CONFLICT DO NOTHING` 写入（避免逐条查询的写放大）、
   解析任务一次性预加载 origin（避免循环内逐条查询）、轮询走部分索引；
   但普通的一次性流程（如健康检查）就用最直白的写法。

## 二、分层与依赖方向

```
Job/ (编排)  →  Spider/ (抓取与解析)  →  Tool/ (HTTP/HTML/JSON/时间等公共工具)
    ↓
Data/ (连接工厂 + DAO)  →  Model/ (SqlSugar 实体)
```

| 层 | 职责 | 不做的事 |
|---|---|---|
| `Job/` | 调度节奏、事务边界、状态机推进、失败重试与日志 | 不写 HTTP 细节，不解析业务 JSON |
| `Spider/` | 请求构造、响应解析、归一化成实体、源特有的字段映射 | 不碰数据库，不管事务 |
| `Data/` | 连接工厂、DAO 的 CRUD / 批量 SQL、异常包装 | 不做业务判断，不持有事务状态 |

依赖方向单向向下，且**实体只有一套**（`Model/`）：`k-spider-sync` 与 `k-spider-test` 都引用主项目的实体，不各自维护副本。

## 三、数据访问约定

- **DAO 接收 Job 创建的连接**：事务由 Job 层管理，DAO 只负责执行语句。这样一次业务动作的原子边界一目了然。
- **事务写法**：`BeginTran → 业务 → CommitTran`，`catch` 中 `RollbackTran`（SqlSugar 对无活动事务的 Rollback 是安全空操作），不要放在 `finally` 里。
- **upsert 语义按场景选择**：
  | 场景 | 语义 | 原因 |
  |---|---|---|
  | 写列表行 | `ON CONFLICT (news_url) DO NOTHING` | 已存在的行由后续阶段推进状态，不能被列表重抓覆盖 |
  | 写原始内容 | `ON CONFLICT (news_url) DO UPDATE` | 重新下载的原始内容应覆盖旧值 |
  | 写结构化详情 | `ON CONFLICT (news_url) DO UPDATE` | 解析规则升级后重跑应能修正历史数据 |
- **批量写入用手拼 `ON CONFLICT`**：SqlSugar 的 `ToSqlString` 默认 200 行分页，批量场景需要 `IsNoPage = true`，实现集中在 `Data/SpiderNewsBatchDao.cs`，要写批量 SQL 时照抄它。
- **异常统一包装**：数据访问异常包成 `KDbException`，抓取/解析异常包成 `DownloadHttpException` 族（`HtmlFormException` 等）。分开的目的是让上层能区分**数据库抖一下**和**数据本身有问题**——两者的重试策略不同（见下）。

## 四、错误处理与重试约定

新闻管线用**状态机 + 计数**表达失败，不用队列重投：

- 成功链：`0 未下载 → 3 已下载原始 → 1 已解析详情`；失败态：`2 解析失败 / 4 下载失败`。
- 失败态在 `fail_count < NewsPipelineConst.MaxFailCount(3)` 时自动重试，达到上限后停在终态，不再消耗资源。
- **`KDbException` 不消耗重试次数**：数据库暂时不可用与"这条数据解析不了"是两件事，前者应无限等待恢复而不是把数据判死。
- 每条新闻的失败都记日志并带 news_url，便于按条排障。

抓取层对"接口改版"这类**静默失效**特别敏感，因此：

- 列表任务有"当前页无新 URL 即停止翻页"的自适应判断，源改版导致 URL 不变时能自然停下而不是空转。
- `NewsCheckJob` 每 5 分钟探测各源栏目接口，接口返回空数据即记错误日志（防"看起来正常但拿不到新闻"）。
- `LiveConnectivityTest`（`TestCategory=Live`）真连实网验证"能调通 + 能拿到数据集 + 能解析"，源改版时先跑它。

## 五、测试分层

| 层 | 用例 | 依赖 | 何时跑 |
|---|---|---|---|
| 离线夹具回归 | `ClsRealDataTest` / `DfRealDataTest` / `ClsNewsSpiderTest` / `DfContentSpiderTest` 等 | 无网络无数据库，读 `TestData/` 真实响应夹具 | 每次提交（`./scripts/verify.sh`、CI） |
| 真实接口连通性 | `LiveConnectivityTest`（`TestCategory=Live`） | 需要联网，直接请求线上 URL | 人工验证、排障、上线前 |

约定：

- **解析回归用真实响应，不手搓 JSON**：夹具是接口原样返回（未裁字段），来源与抓取时间登记在 `TestData/README.md`。手搓的 JSON 只能验证"我以为的格式"，真实响应能验证"实际格式"。
- 联网用例必须能从离线门禁中排除，且网络不可达时报告**跳过**而不是失败；但接口能连上却拿不到数据、解析失败一律判失败——这正是这类用例的价值。
- 纯函数优先：为让解析可离线测试，把"响应 → 实体"的映射从网络方法里抽成公开静态方法（如 `ClsNewsSpider.ParseListPage`、`DfListSpider.ParseListResponse`），网络方法只是"请求 + 调用它"。

## 六、命名、注释与提交

- 命名空间 PascalCase（`KSpider.*`），实体带 `Model` 后缀；`KSpider.Exceptions` 刻意用复数以避开 `System.Exception` 撞名。
- 注释解释**为什么**（尤其是与直觉相反的取舍、踩过的坑），不复述代码在做什么。
- 提交信息用 Conventional Commits + 中文描述，`type` 取 feat / fix / docs / refactor / test / chore；提交前跑 `./scripts/verify.sh`。
- 凭据红线：不向仓库提交真实凭据；环境相关值走配置 / Options，由部署方用环境变量覆盖。
