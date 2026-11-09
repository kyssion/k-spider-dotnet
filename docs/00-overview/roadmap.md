# 路线图

> 状态：持续更新
> 最后更新：2026-11-09

路线图只保留**近期**与**已识别**的事项；远期规划除非已经写进规格文件，否则不列入。
模块落地状态以根 [README.md](../../README.md) 状态表为准，本文只写里程碑与计划。

当前阶段判断：契约与文档已经先行，v2 代码仍是骨架；下一步优先补工程门禁与 v2 测试，
而不是直接扩大实现面。顺序见本文「M1.5」。

## M0 · 契约与骨架（已完成）

- [x] 业务表、系统设施表与认证表 DDL 基线（三份均幂等）；
- [x] admin / crawler / web 可运行骨架，process 目录保留；
- [x] 数据模型、分布式、认证、crawler、admin、web 设计文档；
- [x] v1 代码、部署单元与旧库导出已归档（见根 [README.md](../../README.md) 项目结构）；
- [x] v1 测试暂留作为过渡门禁（见 [testing.md](../30-guides/testing.md)「测试分层」）。

## M1 · 文档治理 v2（已完成）

- [x] 文档状态词表统一，状态块与日期机器校验；
- [x] 引用、重复、结构、状态四个守卫脚本拆分，共用 `scripts/lib/` 解析层；
- [x] 目录索引覆盖、ADR 必备章节、规格验收结论、待确认索引机器校验；
- [x] 新增问题追踪与路线图，关闭「已知问题只存在于会话」的缺口；
- [x] verify 与 CI 同源：文档检查、构建、格式、测试使用同一顺序。

## M1.5 · 工程基座加固（进行中，先于 M2）

v2 的契约与文档已经先行，代码侧仍以骨架为主（状态见根 [README.md](../../README.md) 状态表）：
后端服务测试仍由 v1 过渡测试承担，v2 服务测试尚未建立。首批已补 v2 架构/契约守卫、编译与依赖门禁、
`dev.sh` / `stop.sh`；剩余前端静态门禁与 CI 加固随本里程碑继续。决策见
[0005-engineering-gates-first.md](../20-adr/0005-engineering-gates-first.md)。

- [x] 建立 v2 架构/契约守卫项目 `tests/k-spider-guards/`，加入 `.sln` 与门禁；
- [ ] v2 服务测试：状态机、契约；随后替换 v1 过渡测试（[issue-tracker.md](issue-tracker.md) #1）；
- [x] 编译门禁：`TreatWarningsAsErrors`、分析器、`EnforceCodeStyleInBuild`；
- [x] 依赖可复现：`nuget.config` 固定源、CPM 传递依赖钉版；
- [ ] 前端门禁：ESLint / Prettier / Vitest / 首屏体积预算（当前只有 `tsc` + 构建 + E2E 冒烟；[issue-tracker.md](issue-tracker.md) #12）；
- [x] `dev.sh` / `stop.sh`；
- [ ] CI 并行、依赖审计与测试报告产物（[issue-tracker.md](issue-tracker.md) #12）；
- [x] 统一 `verify.sh` 的 SDK 提示与 `global.json` 口径（[issue-tracker.md](issue-tracker.md) K）。

判据与目标门禁见 [testing.md](../30-guides/testing.md)「当前门禁与目标门禁」；本里程碑只定顺序，不改变任何业务契约。
M1.5 引入的任何新依赖（如前端测试框架）按红线 10 先登记 ADR / 待确认，不在路线图里直接视为已选型。

## M2 · 分布式与调度（M1.5 之后）

- [ ] 创建 `src/k-spider-grpc/`：proto 与生成代码，admin / crawler 共同引用；
- [ ] admin 调度循环：读 `channel.schedule` 生成任务包，按节点负载派发；
- [ ] 节点注册、心跳、任务下发、取消、探测与终态回报；
- [ ] 对账与重派：无活动超时、探测 K/T、重派上限与 `abandoned` 收敛；
- [ ] 状态机、派发 token、条件更新、取消检查点测试。

## M3 · crawler 首个适配器

- [ ] hosting / channel / engine / adapter / parser / kernel / common / repository 分层落地；
- [ ] HttpGateway、反爬识别与策略、按源限流与失败冷却；
- [ ] `write_mode` 三模式、双键水位、`update_time` 刷新、`KDbException` 边界；
- [ ] 首个数据源的真实响应夹具与回归用例。

## M4 · process 结构化契约

- [ ] 确定输出表、字段、索引与增量消费规则；
- [ ] 解析器/加工器注册与版本语义；
- [ ] 以 `node_type='process'` 接入节点与任务机制；
- [ ] 失败重试、幂等写入与重放测试。

## M5 · admin REST 与 web 首期页面

- [ ] REST 管理 API 首期端点与 DTO；
- [ ] 登录、用户管理、总览、渠道管理、任务控制、节点状态、原始数据只读浏览；
- [ ] 前端 API 客户端、路由守卫、CSRF 与错误态统一处理；
- [ ] admin 托管 `web/dist` 的同源部署链路。

## M6 · 分析与运维

- [ ] 语义层、查询引擎与物化/派生表（按真实需求立项）；
- [ ] 健康探测、告警通道与规则注册；
- [ ] 自包含发布、systemd 托管、日志与回滚方案。

## 暂不考虑

| 事项 | 不做的原因 |
| --- | --- |
| v1 代码/部署兼容层 | v2 契约已独立，兼容层会把历史包袱带进新结构；v1 只作归档参考 |
| 预建语义层 / 查询引擎 / 物化层 | 没有真实分析需求前先建会锁定错误抽象；按需求立项 |
| 采集侧深解析 | 采集库只存原始数据；解析产物归 process / 分析侧 |
| DDL 自动向下迁移 | 三份 DDL 是幂等基线；涉及数据删除的变更先确认备份与恢复方案 |
| 平台特定 AI 配置文件 | 不维护协作约定副本；技能发现适配器（如 `opencode.json`）只做路径映射，不承载规则 |
