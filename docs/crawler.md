# k-spider-crawler 技术文档

> 底层：原始数据抓取服务。数据模型与表规则见 [data-model.md](data-model.md)，平台架构见 [architecture.md](architecture.md)。

## 一、定位与职责

原始数据的抓取。

## 二、模块设计

分层关系：**驱动 → 执行 → 实现**——channel 驱动（何时跑）、engine 执行（一轮怎么跑）、adapter 实现（这个源怎么抓）；源差异知识只落在 adapter + parser，其余各层保持领域无关。

| # | 层 | 职责 |
|---|---|---|
| 1 | **hosting** | 服务启动与运行控制：初始化、系统信号监听、配置读取 |
| 2 | **channel** | 渠道调度编排：读 channel 表发现渠道、按 schedule 节奏触发、管理运行状态——不亲自抓取 |
| 3 | **engine** | 抓取引擎：轮次推进（列表轮 / 详情回填轮）、游标与翻页控制、exec_status 流转、重试——通用机制，不含单源知识 |
| 4 | **adapter** | 源适配器：source / detail 的抓取实现（URL 拼装、签名、请求构造）——源差异知识的唯一落点 |
| 5 | **parser** | 最小解析：record_key 预映射、详情抓取目标——深解析归 process（process 的 parser 才是主要能力，crawler 的 adapter 才是核心） |
| 6 | **kernel** | 抓取内核：HttpGateway 唯一 HTTP 出口、反爬验证（识别/通过/策略）、按源限流与失败冷却；所有 adapter 必须经 Gateway 发请求，不得自建 HttpClient |
| 7 | **common** | 通用工具：无业务语义、无状态才进 |
| 8 | **repository** | 数据访问：连接管理、按 write_mode 三模式入库、双水位——数据库操作全部封装于此 |

## 三、核心流程

（待补充：列表轮 / 详情回填轮 / 调度节奏）

## 四、源适配器

（待补充：适配器契约、注册方式、已接入源清单）

## 五、抓取内核

（待补充：HttpGateway、反爬验证、限流与冷却）

## 六、数据访问

（待补充：连接管理、migration、write_mode 三模式入库、双水位）

## 七、配置

（待补充：appsettings 键、环境变量、凭据注入）

## 八、测试

（待补充：契约测试 / 夹具回归 / Live 用例）
