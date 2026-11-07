# 贡献指南

> 本文只做入口与检查清单；规则本身以 `docs/` 与 [AGENTS.md](AGENTS.md) 为唯一来源，不在此复制。

## 开始之前

1. 读 [README.md](README.md) 了解仓库结构，读 [vision.md](docs/00-overview/vision.md) 了解定位与非目标；
2. 按 [getting-started.md](docs/30-guides/getting-started.md) 完成环境、数据库与首次验证；
3. 用 AI 代理协作时，先读 [AGENTS.md](AGENTS.md)（红线为唯一权威清单）。

## 一次改动怎么走

变更分类、所需产物与强制顺序见 [change-protocol.md](docs/50-ai/change-protocol.md)。一句话版本：

1. 定向：读 [AGENTS.md](AGENTS.md) → [docs/README.md](docs/README.md) → 相关架构文档；
2. 补产物：功能写规格，结构/契约先登记 ADR 或待确认项；
3. 确认：涉及公共接口、数据模型、DDL、调度、认证或不可逆操作时，先获得项目所有者确认；
4. 实现：契约 → 数据访问 → 服务 → 接口/前端；
5. 验证：运行 `./scripts/verify.sh`，保留实际命令与输出；
6. 收尾：停掉自己启动的后台进程，确认没有临时文件；
7. 提交：只有项目所有者明确指示时才提交或推送。

## 提交前检查

- [ ] `./scripts/verify.sh` 全绿（文档四检查 + 技能清单 + 前端构建/E2E + 后端构建/格式/测试）；
- [ ] 没有删除、跳过或弱化测试；
- [ ] 契约/DDL/调度/认证变更已确认并同步权威文档；
- [ ] 文档遵循「一处编写，其余引用」，新增文件已登记到所属目录 `README.md`；
- [ ] 凭据、Cookie、个人数据与调试输出均未入库；
- [ ] `git status --short` 只包含预期文件。

## 文档

文档索引、写作规则与内容归属见 [docs/README.md](docs/README.md)；架构文档骨架见
[tech-doc-template.md](docs/30-guides/tech-doc-template.md)。文档检查失败时按输出逐条修正，不绕过。

## 安全

漏洞报告渠道与已知限制见 [SECURITY.md](SECURITY.md)；不要在公开 issue 中披露利用细节。
