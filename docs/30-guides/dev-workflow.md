# 日常开发流程

> 状态：持续更新
> 最后更新：2026-11-08
> 权威范围：常用命令、端口、脚本行为、进程收尾、提交与推送、环境排错。首次环境准备见 [getting-started.md](getting-started.md)。

## 定位与职责

把日常开发中会反复用到的命令与约定集中到一处，避免 README、AGENTS 和各技术文档各写一份。其他文档需要命令时链接本文，不复制命令块。

## 结构与约定

### 常用命令

| 目的 | 命令 |
|---|---|
| 构建后端 | `dotnet build k-spider-dotnet.sln` |
| 全量验证 | `./scripts/verify.sh` |
| 文档引用检查（单独跑） | `node scripts/check-doc-references.mjs` |
| 文档重复检查（单独跑） | `node scripts/check-doc-duplication.mjs` |
| 文档结构检查（单独跑） | `node scripts/check-doc-structure.mjs` |
| 文档状态检查（单独跑） | `node scripts/check-doc-status.mjs` |
| 技能清单检查（单独跑） | `node scripts/check-skills-manifest.mjs` |
| 后端格式校验 | `dotnet format k-spider-dotnet.sln --verify-no-changes --no-restore` |
| 构建并同步前端产物 | `bash scripts/build-web.sh` |
| 前端开发服务器 | `cd web && pnpm dev` |
| 后端 v1 过渡测试（排除 Live） | `dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory!=Live"` |
| 前端 E2E | `cd web && pnpm test:e2e` |
| 启动 admin | `dotnet run --project src/k-spider-admin` |
| 启动 crawler | `dotnet run --project src/k-spider-crawler` |
| 初始化业务表 | `psql -d k_spider -f db/001_business_tables.sql` |
| 初始化系统设施表 | `psql -d k_spider -f db/002_sys_tables.sql` |
| 初始化认证表 | `psql -d k_spider -f db/003_auth_tables.sql` |

### 端口

| 用途 | 端口 | 来源 |
|---|---|---|
| admin 开发监听 | `5094` | [launchSettings.json](../../src/k-spider-admin/Properties/launchSettings.json) 的 `http` 配置 |
| web 开发服务器 | `5173` | [vite.config.ts](../../web/vite.config.ts) |
| 前端 E2E preview | `5899` | [playwright.config.ts](../../web/playwright.config.ts) |
| crawler | 无 HTTP 端口 | 当前为 worker 骨架 |

前端 E2E 需要先构建 `web/dist`（`cd web && pnpm build`，或 `bash scripts/build-web.sh`）；Playwright 用 preview 伺服构建产物。

### 脚本行为

- `scripts/verify.sh`：依次跑文档引用 / 重复 / 结构 / 状态四检查、技能清单、前端构建、E2E、后端构建、格式校验与 v1 过渡测试（排除 `TestCategory=Live`）。默认要求 dotnet / Node / pnpm / Playwright 浏览器齐全；仅本地冒烟可用 `--skip-tests` 跳过测试，输出会显式标注。
- `scripts/build-web.sh`：`pnpm install --frozen-lockfile`（失败时回退普通 install）→ `pnpm build` → 清空 `src/k-spider-admin/wwwroot` 并复制 `web/dist`。
- `scripts/check-doc-references.mjs`：检查链接、行内路径、链接文字、章节引用、目录链接、裸文件名与措辞。
- `scripts/check-doc-duplication.mjs`：检查跨文件/同文件的精确重复正文行。
- `scripts/check-doc-structure.mjs`：检查状态块与日期、目录索引、ADR 必备章节、架构骨架、标题编号、代码块语言、待确认索引与过期路径。
- `scripts/check-doc-status.mjs`：从 [docs/README.md](../README.md)「文档状态」读取词表，校验状态取值、索引与正文一致、ADR 分组与规格验收结论。
- `scripts/check-skills-manifest.mjs`：检查 `.agents/dotnet-skills/manifest.json` 与 `.agents/skills/` 的受管技能是否一致。

## 核心流程

### 提交与推送

1. 交付前跑 `./scripts/verify.sh`，记录实际输出；
2. 确认没有残留的后台进程与临时文件；
3. 只有在项目所有者明确指示时才提交或推送；未经指示不提交、不推送、不改写远端历史；
4. 提交信息说明“为什么”，不只写“做了什么”。

### 后台进程收尾

本仓库暂未提供 `stop.sh`。后台启动的服务交付前必须按端口确认释放，例如：

```bash
lsof -nP -iTCP:5094 -sTCP:LISTEN
lsof -nP -iTCP:5173 -sTCP:LISTEN
```

有输出说明仍有进程占用；确认是自己启动的进程后再结束，否则先排查是否连到了旧实例。

## 数据与接口

- 数据库连接串、认证配置与调度参数由各服务配置提供；当前 admin 的 `appsettings.json` 只有日志与 `AllowedHosts`，业务配置随实现补齐；
- 配置优先级与凭据注入规则见 [AGENTS.md](../../AGENTS.md)；
- 前端开发期通过 Vite 代理访问 `/api`，生产同源部署不需要代理。

## 配置

- 配置优先级：`appsettings.json → appsettings.{环境}.json → K_SPIDER__ 前缀环境变量 → 代码默认值`；
- 仓库只保留配置键与默认值，不提交真实凭据；
- 新增配置键时，同步所属技术文档的“配置”章节和 [getting-started.md](getting-started.md) 的必要说明。

## 测试与守门

- 测试分层与 Live 用例边界见 [testing.md](testing.md)；
- 文档四检查、技能清单、前端构建/E2E、后端构建/格式/测试都已接入 `verify.sh` 与 CI；
- 交付前必须给出实际命令与输出，不得只写“已通过”。

## 待确认事项

- 是否需要为本地开发补 `stop.sh` / `dev.sh`，以及是否把前端 lint、typecheck、单测纳入门禁；
- crawler 的本地启动参数与端口（当前无 launchSettings）；
- 生产部署的进程托管、日志与回滚方式。
