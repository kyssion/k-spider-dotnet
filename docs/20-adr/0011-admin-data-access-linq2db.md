# 0011 admin 数据访问采用 linq2db 与裸 SQL 组合

> 状态：已确认
> 最后更新：2026-11-10
> 决策人：项目所有者（执行指令）
> 关联：[AGENTS.md](../../AGENTS.md)、[admin.md](../10-architecture/admin.md)、[distributed.md](../10-architecture/distributed.md)、[0010-admin-modular-monolith.md](0010-admin-modular-monolith.md)
> 影响：`Directory.Packages.props`、`src/k-spider-admin/k-spider-admin.csproj`、admin 的 `Repository/`

## 背景

admin 需要读写 `sys_node`、`sys_task`、`channel`、`channel_category`、`sys_user` 与原始数据表。
其中任务状态迁移、对账扫描和取消/重派依赖条件更新、JSONB 合并与索引命中，单靠 LINQ 表达会
降低可读性；同时项目已经确定数据访问目标选型为 linq2db，但此前尚未引入依赖。

## 备选项

| 选项 | 优点 | 代价 |
| --- | --- | --- |
| A. linq2db + 条件更新/对账扫描裸 SQL（选择） | 简单 CRUD 有类型映射；关键并发语义可用手写 SQL 精确表达 | 新增 linq2db 依赖；SQL 需要单独评审与测试 |
| B. 全部使用 Npgsql 裸 SQL | 依赖最少，SQL 完全可控 | 映射与分页重复代码多，易出现字段漂移 |
| C. 引入 EF Core | 生态成熟、迁移工具完善 | 与项目既定选型冲突，JSONB 条件更新仍需裸 SQL，体积与启动成本更高 |
| D. 继续使用 v1 的 SqlSugar | 与 v1 代码一致 | v1 已归档，v2 明确不追求兼容；与 AGENTS 目标选型冲突 |

## 决策

选择 A。admin 的 `Repository/` 统一使用 linq2db 连接 PostgreSQL，简单查询与插入使用 linq2db
映射；条件更新、JSONB 合并、对账扫描、取消/重派与终态清理使用参数化裸 SQL。数据库异常统一包
`KDbException`；取消异常直接透传；所有查询显式传 `CancellationToken`。依赖版本在
`Directory.Packages.props` 统一维护，项目文件只写无版本 `PackageReference`。

## 后果

- 正面：并发状态迁移可以写成可审计的 SQL；简单 CRUD 不重复手写映射；数据访问只有 `Repository/` 一个落点。
- 负面（需要接受的代价）：引入 linq2db 与 Npgsql 运行依赖；SQL 需要与文档中的状态机和索引保持同步；
  linq2db 的 PostgreSQL provider 通过 Npgsql 适配，需要保证两者版本兼容。
- 需要同步修改的位置：`Directory.Packages.props`、`src/k-spider-admin/k-spider-admin.csproj`、
  `src/k-spider-admin/Repository/`、[admin.md](../10-architecture/admin.md)、
  [data-model.md](../10-architecture/data-model.md)、[distributed.md](../10-architecture/distributed.md)。
- 触发重新评估的条件：linq2db 与 .NET 运行时/驱动出现不兼容；或 repository 中裸 SQL 占比高到
  不再需要 ORM，可评估退回 Npgsql 直连。
