# 日常开发流程

> 状态：持续更新
> 最后更新：2026-11-12
本文范围：常用命令、端口、脚本行为、脚本选型、进程收尾、提交与推送、环境排错。首次环境准备见
[getting-started.md](getting-started.md)。

把日常开发中会反复用到的命令与约定集中到一处，避免 README、AGENTS 和各技术文档各写一份。
其他文档需要命令时链接本文，不复制命令块。

## 常用命令

| 目的 | 命令 |
| --- | --- |
| 构建后端 | `dotnet build k-spider-dotnet.sln` |
| 全量验证 | `./scripts/verify.sh` |
| 文档引用检查（单独跑） | `node scripts/check-doc-references.mjs` |
| 文档重复检查（单独跑） | `node scripts/check-doc-duplication.mjs` |
| 文档结构检查（单独跑） | `node scripts/check-doc-structure.mjs` |
| 文档状态检查（单独跑） | `node scripts/check-doc-status.mjs` |
| 技能清单检查（单独跑） | `node scripts/check-skills-manifest.mjs` |
| 后端格式校验 | `dotnet format k-spider-dotnet.sln --verify-no-changes --no-restore` |
| 后端格式修复 | `dotnet format k-spider-dotnet.sln` |
| 构建并同步前端产物 | `bash scripts/build-web.sh` |
| 前端校验闭环 | `cd web && pnpm verify` |
| 前端格式修复 | `cd web && pnpm format` |
| 前端开发服务器 | `cd web && pnpm dev` |
| v2 守卫 + admin 测试 | `dotnet test k-spider-dotnet.sln --configuration Release` |
| 后端 v1 过渡测试（排除 Live） | `dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory!=Live"` |
| 安装 Playwright 浏览器 | `pnpm --dir web exec playwright install chromium` |
| 前端 E2E | `cd web && pnpm test:e2e` |
| 前端 Live E2E（真实 admin + PG） | `cd web && pnpm test:e2e:live` |
| 启动 admin + web | `./scripts/dev.sh` |
| 停止开发期进程 | `./scripts/stop.sh` |
| 启动 admin | `dotnet run --project src/k-spider-admin` |
| admin 核心测试（单独跑） | `dotnet test tests/k-spider-admin-tests/k-spider-admin-tests.csproj --configuration Release` |
| admin PostgreSQL Live 测试 | `K_SPIDER_LIVE_DB='<连接串>' dotnet test tests/k-spider-admin-tests/k-spider-admin-tests.csproj --filter "TestCategory=Live"` |
| 启动 crawler | `dotnet run --project src/k-spider-crawler` |
| 初始化业务表 | `psql -d k_spider -f db/001_business_tables.sql` |
| 初始化系统设施表 | `psql -d k_spider -f db/002_sys_tables.sql` |
| 初始化认证表 | `psql -d k_spider -f db/003_auth_tables.sql` |

## 端口

| 用途 | 端口 | 定义位置 | 变更时还必须同步 |
| --- | --- | --- | --- |
| admin REST（开发） | `5094` | [appsettings.json](../../src/k-spider-admin/appsettings.json) 的 `Admin:Endpoints:RestPort` | [vite.config.ts](../../web/vite.config.ts) 的 `/api` 代理 |
| admin gRPC（开发） | `5095` | [appsettings.json](../../src/k-spider-admin/appsettings.json) 的 `Admin:Endpoints:GrpcPort` | 节点连接配置与部署端口 |
| web 开发服务器 | `5173` | [vite.config.ts](../../web/vite.config.ts) | 文档中的端口说明 |
| 前端 E2E preview | `5899` | [playwright.config.ts](../../web/playwright.config.ts) | `webServer` 与 `use.baseURL` |
| crawler | 无 HTTP 端口 | 当前为 worker 骨架 | 不适用 |

端口数字的唯一来源是本表；其他文档只允许原样引用，不写第二种写法。漏同步的变化会出现
“服务起来了但前端连不上”或 E2E 超时。

前端 E2E 需要先构建 `web/dist`（`cd web && pnpm build`，或 `bash scripts/build-web.sh`）；
Playwright 用 preview 伺服构建产物。Live E2E 不启动 preview，要求 admin 已在 5094 运行并连接目标 PostgreSQL；
`LIVE_BASE_URL`、`LIVE_USERNAME`、`LIVE_PASSWORD` 由环境注入，未设置时用例跳过。

## 脚本行为

- `scripts/verify.sh`：依次跑文档引用 / 重复 / 结构 / 状态四检查、技能清单、前端校验
  （lint / format / 单元 / build / 体积 / 注释一致性）、E2E、后端构建（XML 注释一致性）、格式校验、v2 守卫、admin 测试与 v1 过渡测试
  （排除 `TestCategory=Live`）。默认要求 dotnet / Node / pnpm / Playwright 浏览器齐全；
  仅本地冒烟可用 `--skip-tests` 跳过前端单元 / E2E 与后端测试，输出会显式标注。
- `scripts/build-web.sh`：`pnpm install --frozen-lockfile`（失败时回退普通 install）→ `pnpm build` →
  清空 `src/k-spider-admin/wwwroot` 并复制 `web/dist`。
- `scripts/dev.sh`：前台同时启动 admin（REST 5094 / gRPC 5095）与 web（5173），Ctrl+C 停止全部子进程。
- `scripts/stop.sh`：按端口停止 5094 / 5095 / 5173 / 5899，停后复查；未释放时非零退出。
- `scripts/check-doc-references.mjs`：检查链接、行内路径、链接文字、章节引用、目录链接、裸文件名与措辞。
- `scripts/check-doc-duplication.mjs`：检查跨文件/同文件的精确重复正文行。
- `scripts/check-doc-structure.mjs`：检查状态块与日期、目录索引、ADR 必备章节、架构骨架、标题编号、
  代码块语言、待确认索引与过期路径。
- `scripts/check-doc-status.mjs`：从 [docs/README.md](../README.md)「文档状态」读取词表，校验状态取值、
  索引与正文一致、ADR 分组与规格验收结论。
- `scripts/check-skills-manifest.mjs`：检查 `.agents/dotnet-skills/manifest.json` 与 `.agents/skills/`
  的受管技能是否一致。

## 脚本选型

| 场景 | 用什么 | 现有例子 |
| --- | --- | --- |
| 流程编排：调用命令、串步骤、前置检查 | bash | [verify.sh](../../scripts/verify.sh)、[build-web.sh](../../scripts/build-web.sh)、[dev.sh](../../scripts/dev.sh)、[stop.sh](../../scripts/stop.sh) |
| 数据处理：文本 / JSON / 文件树 | Node `.mjs`，仅标准库、零依赖 | [check-doc-references.mjs](../../scripts/check-doc-references.mjs)、[sync-dotnet-skills.mjs](../../scripts/sync-dotnet-skills.mjs) |
| 机器守卫：代码 / 契约 / 分层的断言 | C# 测试，与 `dotnet test` 同跑 | `tests/k-spider-guards/`（v2 守卫）、`tests/k-spider-admin-tests/`（admin 核心）、`src/k-spider-test/`（v1 过渡） |

说明：Node 不是「前端专属」，它是构建期的既有依赖；`.mjs` 是 Node 的 ES Module 标准扩展名。
bash 只做编排，递归目录、正则提取、路径解析这类逻辑交给 `.mjs`，避免 BSD / GNU 差异与中文引号问题。

## 提交与推送

1. 交付前跑 `./scripts/verify.sh`，记录实际输出；
2. 确认没有残留的后台进程与临时文件；
3. 只有在项目所有者明确指示时才提交或推送；未经指示不提交、不推送、不改写远端历史。

提交信息使用 `type(scope): 说明` 格式，类型使用 `feat` / `fix` / `docs` / `refactor` / `test` / `chore`，
说明用中文写“为什么”；一次提交只做一件事，重构与功能拆开。
分支命名沿用 `feat/` / `fix/` / `docs/` / `chore/` 前缀。

## 后台进程收尾

`./scripts/dev.sh` 是前台运行，Ctrl+C 自带清理；以 `nohup ... &`、编辑器任务或 AI 一次性命令
后台启动的进程，交付前必须用 `./scripts/stop.sh` 收尾。脚本按端口定位 5094 / 5173 / 5899，
停后复查；未释放时非零退出。排查单个端口也可手工执行：

```bash
lsof -nP -iTCP:5094 -sTCP:LISTEN
```

有输出说明仍有进程占用；确认是自己启动的进程后再结束，否则先排查是否连到了旧实例。

## 配置与凭据

- 配置优先级与凭据注入规则见 [AGENTS.md](../../AGENTS.md)；
- 仓库只保留配置键与默认值，不提交真实凭据；
- 新增配置键时，同步所属技术文档的「配置」章节和 [getting-started.md](getting-started.md) 的必要说明。

## 数据访问与代理

- 数据库连接串、认证配置与调度参数由各服务配置提供；admin 的 `appsettings.json` 已包含数据库、
  端点、调度/对账与认证配置键，业务配置随实现补齐；
- admin 本地启动至少需要 `K_SPIDER__DATABASE__CONNECTIONSTRING`（或等价 appsettings 值）；
  Development 环境也可用 `dotnet user-secrets set "Database:ConnectionString" "<连接串>" --project src/k-spider-admin`
  保存本机默认测试库连接；user secrets 不进入仓库，其他环境仍由部署方注入；
- 前端开发期通过 Vite 代理访问 `/api`，生产同源部署不需要代理。

## 测试与门禁

- 测试分层与 Live 用例边界见 [testing.md](testing.md)；
- 文档四检查、技能清单、前端校验/E2E、后端构建/格式/测试都已接入 `verify.sh` 与 CI；
- CI 按 docs / backend / frontend 并行，质量口径与 verify 一致；决策见
  [0007-ci-parallel-jobs.md](../20-adr/0007-ci-parallel-jobs.md)。
- 交付前必须给出实际命令与输出，不得只写「已通过」。

## 环境排错

| 现象 | 处理 |
| --- | --- |
| `dotnet test` 输出 NU1900（NuGet 缓存目录权限） | 本机缓存权限问题，不影响测试；必要时清理 `~/.local/share/NuGet/http-cache` 后重试 |
| HTTPS 推送 `.github/workflows/` 被拒绝 | 当前 `origin` 已改为 SSH；若改回 HTTPS，PAT 需 `workflow` scope，见 [issue-tracker.md](../00-overview/issue-tracker.md) O |
| 端口被占用 | 先跑 `./scripts/stop.sh`；仍占用时按本文「后台进程收尾」检查旧实例 |
| Playwright 浏览器缺失 | 按本文「常用命令」安装 Playwright 浏览器 |
| 前端构建失败 | 按本文「脚本行为」重跑 `build-web.sh`，仍失败按报错定位依赖或类型 |
| 数据库连接失败 | 见 [getting-started.md](getting-started.md)「初始化数据库」 |
| `dotnet format` 校验失败 | 按本文「常用命令」的后端格式修复命令处理 |

## 待确认事项

- crawler 的本地启动参数与端口（当前无 launchSettings）；
- 生产部署的进程托管、日志与回滚方式。

## 相关文档

- [getting-started.md](getting-started.md)：首次环境准备与数据库初始化；
- [testing.md](testing.md)：测试分层、夹具与门禁；
- [add-a-feature.md](add-a-feature.md)：一次改动的完整步骤；
- [CONTRIBUTING.md](../../CONTRIBUTING.md)：提交前检查清单。
