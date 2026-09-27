# 网页型源接入规范（Playbook）

本文固化**网页抓取型源**（有独立详情页，如东方财富、财联社文章频道）的**侦察方法与接入流程**。
回答两个问题：接入一个新源前要收集哪些数据（侦察清单）；按什么步骤与标准把它接进管线（流程与验收）。

> **备注：规范按需调整**
> 本规范是对当前实践的固化快照，不是一成不变的铁律。规范固化的是**方法与验收标准**（证据优先、逐项实测、夹具回归），
> 不是某个接口的具体形态。遇到以下情况时，以实测为准灵活处理，并把结论回填到本文与 [news-pipeline.md](news-pipeline.md)：
> - 源站改版（接口路径 / 参数 / 页面模板 / 数据内嵌方式变化）
> - 鉴权或签名升级（如财联社 `sv` 版本号变更导致 `errno 10012`）
> - 反爬收紧（新增请求头校验、频控、验证码）—— 验证模块见 [anti-bot-verification.md](anti-bot-verification.md)
> - 出现新内容形态（现有两种管线无法描述，如纯视频、纯图集、需要登录的内容）
> 排障时优先跑 `dotnet test --filter "TestCategory=Live"` 定位是接口层还是解析层变了，再决定改代码还是改规范。

## 一、管线归属判断（先回答一个问题）

**列表接口返回的是不是完整记录？**

- 是（财联社电报、金十快讯这类"列表即全文"）→ **实时快讯型** `IFlashNewsSpider`：一个方法 `GetFlashPage`，拉到即终态直写 `spider_flash_news`，15 秒轮询。
- 否、正文需要二次请求（东财新闻、财联社文章）→ **网页抓取型** `INewsSpider`：三段接力（列表 → 原始内容 → 解析），状态机驱动。

两种管线注册表分开（`NewsSpiderRegistry` / `FlashNewsSpiderRegistry`），一个源只属于一种管线。
同一网站可以有两种内容形态（财联社 = 电报 + 文章频道），此时**共用同一个 FromMedia 枚举值**（财联社统一为 `ClsMedia`），分属两个注册表、管线归属由注册表决定；枚举标识的是"网站来源"，不是"管线"。

## 二、侦察清单：动手写代码前要收集的四组数据

全部结论必须来自**对真实接口的实测**（浏览器 F12 / curl / 临时脚本），不允许凭猜测写死。逐项记录到源明细表。

### 1. 列表侧（怎么"发现"新闻）

| 要确认的数据 | 说明 |
|---|---|
| 列表数据接口 | 优先找 XHR 接口而不是解析页面 HTML；SPA 站点从 `__NEXT_DATA__` / 页面 chunk 里反查 |
| 栏目/频道参数全集 | 每个栏目的标识与名称；频道配置有时本身就是接口（财联社 `common_config` 的 `column_bar`） |
| 翻页模型 | 页码 / 时间游标 / 接口自带 cursor；**边界语义必须实测**（含边界还是严格小于、游标是否单调、短页能否判末页） |
| 单页上限与越界行为 | 上限会被静默钳制（财联社电报 `rn>50` 返回空数组）还是根本不生效（文章频道不按 `rn` 裁页）——两种都要靠实测发现 |
| 每条的字段与时间格式 | 时间格式**强绑定**解析，格式错了会 `FormatException` 计入重试 |
| 鉴权 / 签名 | 是否需要 sign / cookie / 特殊请求头；签名算法从前端 bundle 逆向，并用一组实测向量锁进单测 |
| 去重键选型 | 选**稳定形态**的 URL，不选带可变参数（版本号、trace）的形态，避免键漂移 |

### 2. 详情侧（怎么"拿到正文"）

- **优先找返回结构化数据的详情接口**（东财 `kuaixun/v2/api/article/{id}`）；没有接口时看详情页是否服务端渲染（财联社文章正文内嵌在 `__NEXT_DATA__`，抓整页存 JSON 载荷即可）；两者都没有、正文需浏览器渲染才动用 Playwright。
- 从页面 URL 反推详情参数的规则（东财两种 URL 形态都兼容）。
- 正文 HTML 的**标签全集**与每种标签到 `NewsContentSegment`（`TEXT/IMG/TABLE/UL/OTHER`）的映射；未知标签**跳过片段并记日志**，不许让单条未知标签炸掉整篇。
- 详情与列表的时间格式可能**不同**，各自绑定，不能共用。

### 3. 反爬侧（怎么"不被拦"）

- 抓取请求一律走 `Spider/Verify/VerifiedHttp`（**不要直接调 `HttpClientTools`**）：它按 host 复用客户端、自动带已通过验证的会话 cookie，并在被拦住时识别验证类型、自动过验证后重放请求。请求头伪装仍由 `HttpClientTools.CreateByHost` 负责（按 host 复用客户端）；**不要手动设 `Accept-Encoding`**（会关掉自动解压）。
- 域名不固定（列表与详情不同 host）时分别建客户端，并分别确认是否被拦。
- **侦察时顺手确认反爬形态**（判据见 [anti-bot-verification.md](anti-bot-verification.md) 的识别器表）：用 curl 打一次列表接口，看状态码、响应头（`cf-mitigated` / `Server` / `Retry-After`）与响应体是不是薄壳页；返回 200 但载荷里带 `msg`/`message` 提示语的，先确认不是风控再当解析问题查。
- 若新源用的是本模块还不认识的验证方案：按 [anti-bot-verification.md](anti-bot-verification.md) 的扩展套路补一个识别器（纯判定）+ 一个通过策略，并在夹具目录加一个特征样本。
- 频控不需要爬虫侧实现：调度间隔（2 分钟）+ 每栏目最多 4 页 + "整页已存在即停"已经封顶；被限流（429）时验证模块会按 `Retry-After` 退避并告警，不要靠加大请求量去"撞过去"。

### 4. 落库侧（数据长什么样）

- 网页型三段各自产出的形态是固定的：列表行（status=0）→ origin 原始内容（status=3）→ 结构化片段 + 图片（status=1）。
- 新源**不改表结构**：`from_media` / `category` 已在表上；category 用本源独立编号段（东财 1-22、财联社电报 101、财联社文章 102-114、新浪 201、见闻 301、金十 401）。

## 三、侦察方法（证据优先，以财联社文章频道为例）

1. **判定页面形态**：抓首页 HTML——有完整数据内嵌（SSR/`__NEXT_DATA__`）就省一步；纯空壳则是 SPA，数据在 XHR 里。
2. **从路由表反查页面 chunk**：Next.js 的 `_buildManifest.js` 列出全部路由与 chunk，目标页面的 chunk 里就有它调用的 API 路径与参数（`/v3/depth/list/${id}` 就是这样找到的）。
3. **签名算法逆向**：在 bundle 里定位 sign 函数（本例是模块化的 SHA-1 + MD5 组合），确认参数拼接规则（key 升序、空值丢弃），**算出的签名先与已知实测向量比对**（财联社 `ClsNewsSpiderTest.SignMatchesVerifiedVector`），排除自己工具链的问题（例如 shell 管道把换行符哈希进去导致"看起来签名失效"的假象）。
4. **逐项实测**：每个候选接口用最小参数打一遍；翻页用真实游标再拉一页，看边界条目是否重复、两页是否重叠、游标是否单调——**边界语义只信实测**（财联社文章的"混排不单调、重叠 13 行"就是实测才发现的）。
5. **顺手存夹具**：侦察时抓到的真实响应直接存 `src/k-spider-test/TestData/`（接口响应存 `.json`、SSR 页面存 `.html`），同目录 README 登记来源接口、抓取时间、参数——它们就是后面解析回归的夹具。

## 四、接入步骤（网页型）

照 AGENTS.md 的"新增网页抓取型新闻源"套路执行：

1. `<源>NewsResource.cs`（端点 / host / 栏目与分类号常量，把实测到的边界语义写成注释）；
2. `<源>SpiderModel.cs`（与 Resource / Spider 同目录直下，不再建 Model/ 子目录；接口响应模型 + `To*Model()` 映射，时间转换在这里做，可空字段写兜底）；
3. `<源>Spider.cs` 实现 `INewsSpider` 三段；列表与解析方法抽成 `public static` 供离线测试；**不得用可变实例字段存请求状态**（Job 按源并行）；HTTP 调用走 `VerifiedHttp`（需要自定义请求头时传请求工厂，`HttpRequestMessage` 不能重发）；
4. `FromTypeOfNews` 加枚举值 + `NewsSpiderRegistry` 注册一行；
5. 夹具入库 + 离线解析回归（`ClsArticleSpiderTest` 是最新范例：字段映射 / 过滤分支 / 游标语义 / 详情解析 / 坏数据抛错六个用例）；
6. `LiveConnectivityTest` 加一条"列表 → 原始 → 解析 → 游标续拉"全链路用例；
7. 文档同步：`news-pipeline.md` 源明细表 + 源小节 + 源成熟度表，已知坑写进 AGENTS.md。

## 五、验收标准（全部满足才算接完）

```bash
./scripts/verify.sh                                                # 文档链接 + 构建 0 警告 0 错误 + 离线测试全绿
dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory=Live"   # 全链路真实连通
```

- 离线回归：真实夹具驱动，字段映射、过滤分支、游标语义、坏数据路径都有断言；
- Live 用例：能调通、有数据、字段完整（url/from/time/from_media/category）、正文非空、游标能续拉；
- 注册表约束：`NewsSourceRegistryTest` 全绿（一源一管线、分类号段不越界不重叠）；
- 运行观察：首轮 `NewsCheckJob` 探测日志里新源每个栏目都返回有效数据，无未知标签告警，且没有 `VerificationRequiredException` / 验证冷却告警。

## 六、历史参照

| 源 | 侦察要点 | 落地 |
|---|---|---|
| 东方财富 | XHR 列表 + JSON 详情接口 + 三种老页面模板兼容 | `Spider/News/Web/Eastmoney/`，35 栏目 |
| 财联社文章 | 频道配置在 `common_config`、列表混排游标、详情 SSR `__NEXT_DATA__` | `Spider/News/Web/Cls/`，13 栏目 |
| 财联社电报（快讯型对照） | 签名逆向 + 单页上限 50 + 严格小于游标 | `Spider/News/Flash/Cls/` |
