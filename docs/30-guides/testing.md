# 测试与门禁

> 状态：持续更新
> 最后更新：2026-11-08
> 权威范围：测试分层、Live 用例、夹具、验证闭环与门禁。命令清单见 [dev-workflow.md](dev-workflow.md)。

说明「什么测试放在哪里、哪些进出门禁、哪些只能手动跑」。红线「不得删除、跳过或弱化测试」见
[AGENTS.md](../../AGENTS.md)。

## 测试分层

| 层 | 位置 | 是否进门禁 | 说明 |
| --- | --- | --- | --- |
| 文档四检查 | `scripts/check-doc-*.mjs` | 是 | 引用、重复、结构、状态；判据见 [docs/README.md](../README.md) |
| 技能清单检查 | `scripts/check-skills-manifest.mjs` | 是 | 受管技能与磁盘一致 |
| 后端单元 / 契约 | 各服务测试项目（待落地） | 是（落地后） | 领域规则、状态机、REST/gRPC 契约 |
| v1 过渡测试 | `src/k-spider-test/` | 是 | 排除 `TestCategory=Live`；新服务测试落地后替换 |
| 前端 E2E | `web/tests/` | 是 | Playwright；当前为骨架冒烟，API 在用例内 mock |
| Live 连通性 | 按测试项目约定 | 否 | 依赖真实接口或线上可达，手动执行 |

## 夹具

- 夹具使用接口真实响应，新增夹具必须登记来源与抓取时间；
- 夹具不得包含真实凭据、Cookie 或个人数据；
- Live 用例用 `TestCategory=Live` 标记，门禁命令显式排除。

## 门禁边界

- 本地 `./scripts/verify.sh` 默认要求 pnpm 与 Playwright 浏览器齐全；仅本地冒烟可用 `--skip-tests`
  跳过 E2E 与后端测试，输出会显式标注。交付前以完整校验为准；
- 新服务测试项目落地后，必须加入解决方案与门禁，不能只留在本机；
- v1 测试项目是过渡门禁，新 crawler 测试落地后替换，不长期保留双轨。

## 验证闭环

1. 先确定测试层：领域规则、服务契约、接口集成、前端 E2E 或 Live；
2. 写失败用例（或先确认现有用例能复现问题）；
3. 实现修复或功能；
4. 跑对应测试，再跑 `./scripts/verify.sh`；
5. 交付说明列出命令与原始输出，以及未覆盖项。

## 契约与数据

- 后端契约测试以 DDL、REST/gRPC 契约和 [data-model.md](../10-architecture/data-model.md) 的规则为准；
- 前端 E2E 的真实后端用例后续补 Live spec，与后端 `TestCategory=Live` 的分层对齐；
- 文档与技能检查不依赖网络，可在离线环境运行。

## 配置与凭据

- 测试所需的环境变量、数据库连接与凭据由本地或 CI 注入，不写入仓库；
- Live 用例默认不读取生产凭据。

## 待确认事项

- 新服务测试项目的命名、目录与是否引入统一的测试夹具库；
- 前端是否补 lint、typecheck 与组件测试门禁；
- Live 用例的触发方式（本地脚本 / 手动 CI job）与凭据来源；
- v1 测试替换的时间点与旧用例迁移范围。

## 相关文档

- [dev-workflow.md](dev-workflow.md)：命令、端口与脚本行为；
- [add-a-feature.md](add-a-feature.md)：改动时如何补测试；
- [data-model.md](../10-architecture/data-model.md)、[distributed.md](../10-architecture/distributed.md)、
  [auth.md](../10-architecture/auth.md)：契约测试的权威来源；
- [definition-of-done.md](../50-ai/definition-of-done.md)：交付前自检。
