# k-spider-crawler 技术文档

> 状态：已确认
> 最后更新：2026-11-08

本文定义抓取服务的分层、适配器边界、抓取流程、反爬与数据访问；模块状态见根
[README.md](../../README.md) 状态表。数据模型、调度与平台架构见本文「相关文档」。

## 定位与职责

原始数据的抓取：按 admin 派发的任务包执行渠道轮次，把列表条目与详情原始载荷写入 `data_source` / `data_detail`，不在采集侧做业务解析。源差异知识只落在 adapter + parser，其余各层保持领域无关。

## 结构与约定

### 分层

分层关系：**执行 → 实现**——channel 绑定任务（哪个渠道）、engine 执行（一轮怎么跑）、adapter 实现（这个源怎么抓）。

| # | 层 | 职责 |
|---|---|---|
| 1 | hosting | 服务启动与运行控制：初始化、系统信号监听、配置读取 |
| 2 | channel | 渠道任务绑定：按任务包中的 `channel_id` 加载渠道与适配器配置、管理本轮运行状态——不发现渠道、不自行定时、不亲自抓取 |
| 3 | engine | 抓取引擎：轮次推进（列表轮 / 详情回填轮）、游标与翻页控制、`exec_status` 流转、重试——通用机制，不含单源知识 |
| 4 | adapter | 源适配器：source / detail 的抓取实现（URL 拼装、签名、请求构造）——源差异知识的唯一落点 |
| 5 | parser | 最小解析：`record_key` 预映射、详情抓取目标——深解析归 process |
| 6 | kernel | 抓取内核：HttpGateway 唯一 HTTP 出口、反爬验证（识别/通过/策略）、按源限流与失败冷却；所有 adapter 必须经 Gateway 发请求，不得自建 HttpClient |
| 7 | common | 通用工具：无业务语义、无状态才进 |
| 8 | repository | 数据访问：连接管理、按 `write_mode` 三模式入库、双键水位——数据库操作全部封装于此 |

### 适配器契约

- source adapter（渠道列表抓取）、detail adapter（资源详情抓取）、parser（最小解析）；
- 线程安全与无状态：游标与页状态走参数/返回值，不用可变实例字段；
- 代码注册表按编码取用，未知编码加载即抛（fail-fast）；渠道行存 `source_adapter` / `detail_adapter` 编码；目录登记规则见 [data-model.md](data-model.md)；
- 列表即全文：详情与列表同源的场景，`detail_adapter` 与 `source_adapter` 同值。

## 核心流程

### 列表轮

admin 派发列表轮任务后，engine 按任务包中的 `channel_id` 加载渠道配置；source adapter 拉列表；parser 做最小解析（`record_key` 预映射、详情抓取目标）；repository 按渠道 `write_mode` 写入 `data_source`。

### 详情回填轮

按 `data_source.exec_status` 取待回填行；detail adapter 抓详情；repository 写入 `data_detail`；行存在即详情已抓取，`exec_status` 落成功。

### 翻页与多栏目

- 翻页控制：自适应停止（翻到存量区间即停）、停机回补上限、每轮批量上限，参数来自渠道 `extra`；
- 多栏目：栏目号/频道参数/伪栏目由 adapter + `channel.extra` 表达，不新增表结构；
- 两段回填：目标态只保留“列表 → 详情”两段；v1 的三段历史不再作为目标契约。

### 异常与重试

按渠道粒度兜底；本轮失败由 admin 对账或下一轮任务包重派，见 [distributed.md](distributed.md)。数据访问异常（`KDbException`）与数据本身失败分开：前者不消耗重试计数。

## 数据与接口

### 数据访问

- 连接管理、DDL / migration 幂等执行、按 `write_mode` 三模式入库、批量写入集中在 repository；规则唯一权威见 [data-model.md](data-model.md)，实现约定见 [AGENTS.md](../../AGENTS.md)；
- 双键水位的消费侧归 process（设计待确认）；crawler repository 只负责写入并正确刷新 `update_time`，不得依赖数据库默认值替代更新；
- 数据访问异常包 `KDbException`，不与数据失败混用重试计数。

### 抓取内核

- HttpGateway：唯一 HTTP 出口；每主机独立客户端、同主机串行 + 最小间隔、失败冷却（限流按源站 `Retry-After` 退避）、响应编码处理；
- 反爬验证：Detector（纯判定注册表，越具体越靠前）→ Solver（代价升序）→ Policy（按源放行集）；
- 识别：HTTP 门禁（403/401/412/429/503+Retry-After/3xx）、Cloudflare 类 JS 挑战、JS cookie 门禁（加速乐/阿里云盾/瑞数类）、浏览器检查页、验证码（滑块/图形/短信）、载荷级风控（200 但 envelope 报风控）；
- 通过：浏览器过挑战导出 cookie 回放 HTTP 链路（默认放行）；滑块按源显式放开；图形/短信只识别并升级告警；
- 会话与克制：会话按主机缓存复用、CookieContainer 回放（不手动拼 Cookie 头）、同主机串行、失败冷却、按源策略（放行集合可追加/收窄/关闭）；
- 防误判：内容型识别器只对 HTML 生效、风控词只扫 envelope、中文泛词需配合表单元素、词表不放中性词；
- 平台能力：全局限流器（按源令牌桶）、代理池（插件位）、浏览器渲染服务（按需启动）。

### 数据源

能力清单见 [features.md](../00-overview/features.md)“数据源清单”（v1 已实现源；v2 按适配器逐个迁移），本文不重复维护源清单。

## 配置

- crawler 的配置来源与优先级见 [AGENTS.md](../../AGENTS.md)；
- 渠道实例参数（栏目号、窗口、批量上限、凭据引用名等）走 `channel.extra`；凭据只存 `credential_key` 引用，真实值由部署方环境注入；
- `channel.extra` 的键名、类型、默认值与所属适配器尚未定稿，实现前须先与项目所有者确认并登记；
- crawler 自身的限流、冷却、批量等配置键待实现时定。

## 测试与守门

- 通用测试约定（夹具 / Live / 门禁）见 [testing.md](../30-guides/testing.md)；
- 本服务分层：契约测试（离线）/ 夹具回归（真实响应）/ Live 连通性（门禁排除）；
- 当前门禁由 v1 过渡测试承担，替换条件见 [testing.md](../30-guides/testing.md)「测试分层」；
- 适配器注册表、write_mode、水位刷新、`KDbException` 与重试边界必须有测试。

## 待确认事项

| 事项 | 说明 |
|---|---|
| `channel.extra` 键表 | 栏目号、窗口、批量上限、凭据引用等键名与类型（实现前确认） |
| 列表即全文 | `detail_adapter` 与 `source_adapter` 同值时，是否仍写 `data_detail` 行、raw 是否复用列表载荷（实现前确认） |
| 抓取配置键 | 限流、冷却、批量等 crawler 配置键（实现时定） |
| 反爬策略细节 | 识别器特征、放行集合与告警阈值（随实现补齐） |
| 双键水位消费侧 | process 的消费方式与水位设计 |

## 相关文档

- [system-overview.md](system-overview.md)、[data-model.md](data-model.md)、[distributed.md](distributed.md)：平台、数据与调度契约；
- [features.md](../00-overview/features.md)：数据源清单；
- [process.md](process.md)：下游消费方；
- [testing.md](../30-guides/testing.md)：夹具与 Live 分层。
