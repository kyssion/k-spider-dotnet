# k-spider web 技术文档

> 顶层：展示与操作界面——通过同源 admin REST API 获取数据与执行操作。平台架构见 [system-overview.md](system-overview.md)，功能与数据源清单见 [features.md](../00-overview/features.md)，认证接口见 [auth.md](auth.md)，数据语义见 [data-model.md](data-model.md)。

> **状态**：骨架（最后核对：2026-11-04）；实施细节待补充，当前仓库只有最小可运行骨架，业务页面待实现。

## 一、定位与职责

（待补充：展示与操作边界；不直连数据库；与 admin 同源部署；认证与权限由 admin 承载）

## 二、技术选型

（已确定：React 19 + TypeScript + Vite、Tailwind CSS 4、React Router、TanStack Query、Zustand、Recharts、Playwright；待补充：选型理由、版本约束与升级策略）

## 三、目录结构

（待补充：`web/src/api` / `web/src/components` / `web/src/pages` / `web/src/stores` / `web/src/lib` 等目录与职责；当前只保留最小入口）

## 四、页面与路由

（首期：登录 / 用户管理、总览、渠道管理、任务控制、节点状态、原始数据只读浏览、账号设置；后续：分析看板、派生表管理、数据重放，依赖 process / 分析层契约。待补充：路由、权限、空态与错误态）

## 五、API 对接

（待补充：同源 `/api`、开发期 Vite 代理、DTO 类型、分页与筛选、错误处理、CSRF、登录态跳转；接口契约以 [admin.md](admin.md) 为准。admin REST 尚未定稿，web 实现前先确认首期 MVP 端点）

## 六、状态管理

（待补充：TanStack Query 管理服务端状态、Zustand 管理客户端筛选/UI 状态；缓存、刷新与失效策略）

## 七、UI 规范

（待补充：Tailwind 设计令牌、组件分层、布局、暗色模式、可访问性、表格/表单/图表约定）

## 八、构建与托管

（待补充：pnpm 安装与构建、`tsc --noEmit` 类型检查、`scripts/build-web.sh` 拷贝到 admin `wwwroot`、同源部署与环境配置）

## 九、测试

（待补充：Playwright E2E、mock API、Live 分层、门禁排除；夹具与真实接口响应的边界）

## 十、待定事项

（待补充：页面范围、组件库取舍、图表交互、权限粒度、i18n 等）
