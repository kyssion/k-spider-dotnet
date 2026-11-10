# 0007 CI 按域并行，质量口径与 verify 同源

> 状态：已确认（待办为历史快照，进度见 roadmap）
> 最后更新：2026-11-09
> 决策人：项目所有者（执行指令）
> 关联：[0004-docs-and-skill-governance-v2.md](0004-docs-and-skill-governance-v2.md)、[testing.md](../30-guides/testing.md)、[dev-workflow.md](../30-guides/dev-workflow.md)
> 影响：`.github/workflows/ci.yml`、`scripts/verify.sh`、CI 运行时间与失败定位

## 背景

ADR 0004 要求 verify 与 CI「同一顺序运行」文档检查、技能清单、前端构建/E2E、后端构建、格式校验与测试。
随着 v2 守卫、前端静态门禁与 E2E 加入，单 job 串行执行变长；而文档、前端、后端三组检查互不依赖，
串行等待没有收益。需要在不降低质量口径的前提下缩短 CI 墙钟，并保持本地 verify 仍是完整入口。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 保持单 job 串行 | 本地与 CI 顺序完全一致，排查路径单一 | CI 墙钟随检查项线性增长 |
| B. 拆 docs / backend / frontend 三个 job 并行（选择） | 墙钟缩短；失败按域定位；缓存可分别配置 | CI 顺序不再与 verify 一致；新增检查项要同时接入两边 |
| C. 只拆前端，后端与文档串行 | 改动较小 | 前端已是耗时大头，但文档与后端仍互相等待 |

## 决策

选择 B：CI 拆为 docs / backend / frontend 三个并行 job；`scripts/verify.sh` 仍串行执行同一组质量检查。
ADR 0004 的「同一顺序」条款由本 ADR 细化为「同一质量口径」：构建、格式与测试仍用 Release，
文档四检查、技能清单、前端校验 / E2E、后端构建 / 格式 / v2 守卫 / v1 过渡测试一个都不能少。

## 后果

- 正面：CI 墙钟缩短；失败按 job 定位；前端与后端可分别使用 Node / .NET 缓存。
- 负面（需要接受的代价）：本地 verify 与 CI 的执行顺序不再一致；新增检查项必须同时接入 verify 与对应 CI job；
  并行 job 的日志分散，需要按 job 查看。
- 需要同步修改的位置：`.github/workflows/ci.yml`、[verify.sh](../../scripts/verify.sh) 头部说明、
  [testing.md](../30-guides/testing.md)、[dev-workflow.md](../30-guides/dev-workflow.md)、[roadmap.md](../00-overview/roadmap.md)「M1.5」。
- 触发重新评估的条件：某个 job 再次成为明显瓶颈；或出现必须跨域共享构建产物的检查项。

## 待办

- [x] CI 拆为 docs / backend / frontend 并行 job
- [x] verify.sh 头部改为同一质量口径
- [ ] 观察 CI 时间与缓存命中，必要时调整 job 粒度
