# Definition of Done

> **状态**：现状（最后核对：2026-11-04）

一次变更只有在以下条件全部满足时才算完成：

- [ ] 代码 / DDL / 配置与相关文档、索引、状态日期同一次更新；
- [ ] 未确认的契约、接口、参数已标【待确认】并登记到 [20-adr/README.md](../20-adr/README.md)；
- [ ] 没有把目标设计写成现状，没有把猜测写成既定事实；
- [ ] 新增文档使用 [技术文档模板](../30-guides/tech-doc-template.md)，并加入 [文档索引](../README.md)；
- [ ] `node scripts/check-doc-references.mjs` 通过；
- [ ] `./scripts/verify.sh` 通过；
- [ ] 交付说明包含改动、验证命令与实际结果、未覆盖项。
