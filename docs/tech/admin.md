# k-spider-admin 技术文档

> 旁路：管理控制服务——中央调度、集群维护、REST 管理 API、认证与账号、web 前端托管。平台架构见 [architecture.md](../architecture.md)；调度机制与状态机见 [distributed.md](../distributed.md)；认证行为见 [auth.md](../auth.md)；业务数据语义见 [data-model.md](../data-model.md)。

> **状态**：技术方案骨架，实施细节待补充；已确认的分布式与认证规则不在本文重复定义。

## 一、定位与职责

（待补充：服务边界；与 crawler/process 的调度关系；与 web 的同源部署关系；明确 admin 不在数据链路上）

## 二、模块设计

（待补充：hosting / scheduler / reconciliation / grpc / api / auth / web-hosting / repository / common 等目录与职责；机制与领域知识的边界）

## 三、启动与运行

（待补充：配置加载顺序、日志、REST / gRPC 分离监听端点、健康检查、优雅停机、admin 单机约束；gRPC 明文 h2c，TLS 按端点预留）

## 四、调度与对账

（待补充：读 `channel.schedule` 生成任务包、节点选择、派发互斥、对账循环、重派与取消的实现落点；机制唯一权威见 [distributed.md](../distributed.md)。`channel.schedule` 的 JSON 键、时区与错过轮次语义尚未定稿，实现前先确认）

## 五、gRPC 节点服务

（待补充：proto 位置、`Connect` 双向流生命周期、上行/下行消息处理、写流串行化、重连与探测；协议机制见 [distributed.md](../distributed.md)）

## 六、REST 管理 API

（待补充：`/api` 端点清单、请求/响应 DTO、分页与筛选、错误码、幂等约定；认证相关接口以 [auth.md](../auth.md) 为准）

## 七、认证与账号

（待补充：认证中间件、Cookie 与 Data Protection、Antiforgery、登录/账号管理端点、默认账号处理；行为唯一权威见 [auth.md](../auth.md)）

## 八、前端托管

（待补充：`wwwroot` 静态文件、SPA fallback、构建产物来源、同源部署、缓存策略）

## 九、数据访问

（待补充：`sys_node` / `sys_task` 读写、`channel` 读取、管理查询、事务边界、`KDbException`；业务表规则见 [data-model.md](../data-model.md)，物理结构见 `db/002_sys_tables.sql`）

## 十、配置

（待补充：`appsettings` 键、`K_SPIDER__` 环境变量、监听端口、数据库、认证、调度与对账参数）

## 十一、部署与运维

（待补充：发布方式、systemd 托管、日志与告警、升级与回滚、REST / gRPC 端口与 TLS 配置）

## 十二、测试

（待补充：调度状态机、对账与重派、REST API、认证、gRPC 契约、Live 分层）

## 十三、待定事项

（待补充：proto 字段与错误码、`task_detail` / `extra` JSON 契约、节点认证、REST / gRPC 端口与 TLS、对账参数默认值、后台页面范围等；与 [distributed.md](../distributed.md) 的待定章节保持一致）
