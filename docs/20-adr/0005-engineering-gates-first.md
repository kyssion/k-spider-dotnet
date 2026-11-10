# 0005 工程基座加固先于分布式实现

> 状态：已确认（待办为历史快照，进度见 roadmap）
> 最后更新：2026-11-09
> 决策人：项目所有者（执行指令）
> 关联：[roadmap.md](../00-overview/roadmap.md)、[testing.md](../30-guides/testing.md)、[dev-workflow.md](../30-guides/dev-workflow.md)、[0006-frontend-static-gates.md](0006-frontend-static-gates.md)、[0007-ci-parallel-jobs.md](0007-ci-parallel-jobs.md)
> 影响：解决方案、测试项目、`Directory.Build.props`、`Directory.Packages.props`、`nuget.config`、`scripts/`、`verify.sh`、CI

## 背景

v2 的契约与文档已经先行，但代码侧仍以骨架为主：后端服务测试由 v1 过渡测试承担，v2 服务没有自己的测试项目；
编译质量、前端静态检查与依赖可复现性也弱于文档治理。直接进入 M2 会在缺少 v2 守卫的情况下扩大代码面，
契约漂移只能靠评审发现。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. 直接进入 M2 | 更快看到分布式链路 | v2 无守卫，返工成本高；文档与实现更容易脱节 |
| B. 先补 M1.5 工程基座，再进入 M2 | 先建立可执行的 v2 守卫与依赖基线 | 前期投入时间；门禁本身也需要维护 |
| C. 只补后端测试，跳过前端与依赖门禁 | 折中，短期成本最低 | 前端与依赖风险继续裸露，两类问题都不会让后端测试红灯 |

## 决策

选择 B：先补 v2 守卫测试项目、编译门禁、依赖可复现与开发脚本，再进入 M2。
首片只建立架构/契约守卫与配置门禁；服务级测试随 M2/M3 的真实代码落地，不预写空测试。

## 后果

- 正面：v2 代码从第一天起有机器守卫；依赖与构建基线可复现；本地与 CI 的差异可登记。
- 负面（需要接受的代价）：v1 过渡测试仍会保留一段时间；新增测试项目与门禁需要维护；
  组件测试范围待首个真实页面评估。
- 需要同步修改的位置：`k-spider-dotnet.sln`、`tests/k-spider-guards/`、`Directory.Build.props`、
  `Directory.Packages.props`、`nuget.config`、`scripts/dev.sh`、`scripts/stop.sh`、`scripts/verify.sh`、
  `.github/workflows/ci.yml`、[testing.md](../30-guides/testing.md)、[dev-workflow.md](../30-guides/dev-workflow.md)。
- 触发重新评估的条件：v2 服务测试落地后门禁维护成本过高；或新增依赖导致 CI 时间显著超出可接受范围。

## 待办

- [x] 建立 v2 架构/契约守卫测试项目并加入解决方案与门禁
- [x] 编译门禁、`nuget.config`、CPM 传递依赖钉版
- [x] `dev.sh` / `stop.sh` 与本地校验链路
- [ ] v2 服务测试随 M2/M3 落地
- [x] 前端静态门禁（ESLint / Prettier / Vitest / 体积预算）
- [x] CI 依赖审计与测试报告
- [x] CI 并行（[0007-ci-parallel-jobs.md](0007-ci-parallel-jobs.md)）
- [ ] 替换 v1 过渡测试
