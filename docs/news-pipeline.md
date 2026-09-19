# 新闻管线设计

新闻管线是**多源通用**的三段接力：列表发现 → 原始内容下载 → 结构化解析，全部由 `spider_news_list.download_status_code` 状态机驱动。

## 一、状态机

```
                 ┌──────────────── 失败重试 ( fail_count < 3 ) ───────────────┐
                 ▼                                                            │
  [0] 未下载 ──▶ [3] 已下载原始 ──▶ [1] 已解析详情                              │
        │              │                                                   │
        │ 下载失败       │ 解析失败                                          │
        ▼              ▼                                                   │
  [4] 下载失败 ────▶ [2] 解析失败 ────────────────────────────────────────────────┘
```

| 状态 | 含义 | 由谁写入 |
|---|---|---|
| `0` | 未下载原始内容 | `NewsListJob`（详情页型源） |
| `3` | 原始内容已入库 | `NewsContentOriginJob`（详情页型源）/ `NewsListJob`（快讯型源，列表即全文） |
| `1` | 详情已解析 | `NewsContentJob` |
| `4` | 下载失败 | `NewsContentOriginJob`；以及 `NewsContentJob` 发现 origin 缺失时回退 |
| `2` | 解析失败 | `NewsContentJob` |

- 失败态在 `fail_count < NewsPipelineConst.MaxFailCount(3)` 时自动重试，达到上限后停在终态不再消耗资源。
- **`KDbException` 不消耗重试次数**：数据库暂时不可用不该把数据判死，保持原状态等下一轮。
- `NewsContentJob` 若发现 `origin` 缺失（例如外键数据被清理），会把行退回 `4` 让下载阶段重拉，而不是直接判失败。

## 二、三个 Job

| Job | 间隔 | 单轮批量 | 取数条件 |
|---|---|---|---|
| `NewsListJob` | 2 分钟 | 每栏目最多 4 页 × 200 条 | 按栏目遍历（不查库） |
| `NewsContentOriginJob` | 3 秒 | 200 | `status = 0` 或 (`status = 4` 且 `fail_count < 3`) |
| `NewsContentJob` | 1 分钟 | 1000 | `status = 3` 或 (`status = 2` 且 `fail_count < 3`) |

两个消费端都**不做源过滤**，全源按 `id` 升序先进先出：这样单个源积压不会饿死其他源，也不必为每个源单独配额；数据里出现未注册的 `from_media` 时记错误日志并跳过（不消耗重试次数，属配置错误而非瞬时故障）。

### NewsListJob

1. **各 media 并行**：源之间互不依赖，各自独立连接、独立事务、独立节奏，整轮耗时取决于最慢的源。
2. **media 内保持原逻辑**：遍历该源的全部栏目，逐栏目按游标翻页（最多 4 页，下一页依赖上一页的游标），每页一个事务。
3. 每页先查一次"这页的 URL 里哪些已存在"，然后在该事务里批量 `ON CONFLICT (news_url) DO NOTHING` 写列表行；若该页带内联原始内容（快讯型源），同事务写入 `spider_news_content_origin` 并把对应列表行的状态直接置为 `3`。
4. **自适应停止**：当前页的 URL 全部已存在，说明该栏目已翻到存量区间，停止翻页。这让源改版导致列表不再更新时不会空转。
5. 单个 media 失败只回滚该源当前事务并记日志，不影响其它源，也不影响整轮任务；汇总日志按源输出写入数。

### NewsContentOriginJob

按 `from_media` 从注册表取源实现 → `GetContentOrigin` 下载原文 → 一个事务里 upsert 原始内容 + 更新列表行状态。
快讯型源（列表已带全文）不会走到这里，它们的原始内容在列表阶段就落库了；若因异常出现 origin 缺失，这类源的 `GetContentOrigin` 会直接返回失败态（它们没有可回查的单条接口）。

### NewsContentJob

1. 取待解析行后，**一次性按 URL 批量预加载** origin（避免循环内逐条查询）。
2. 按 `from_media` 分发到源实现的 `ParseContent`，解析成结构化片段与图片列表。
3. 列表接口的摘要比详情接口的占位摘要更完整，解析后会**用列表行的 `news_summary` 回填**。
4. 成功：一个事务里 upsert 详情 + upsert 图片 + 更新列表行（`status = 1`、`fail_count = 0`）。

## 三、多源抽象

`Spider/News/` 下的四个契约：

```csharp
public interface INewsSpider
{
    FromTypeOfNews FromMedia { get; }                  // 源标识 , 对应 spider_news_list.from_media
    IReadOnlyList<NewsColumn> Columns { get; }         // 本源栏目清单
    Task<NewsListPage> GetListPage(NewsColumn column, int pageSize, string? cursor);
    Task<NewsContentOrigin> GetContentOrigin(SpiderNewsListModel newsItem);
    NewsContentParseResult ParseContent(string originContent, string newsUrl);
}
```

- **游标对任务不透明**：`NewsListPage.NextCursor` 由各源自行解释（东财是页码字符串，财联社是 `last_time`），`null` 表示没有更多。
  代价是"下一页"的实现差异被封装在源内部，收益是任务侧只用一套循环就支持两种翻页模型。
- **`NewsListPage.InlineOrigins`** 是"列表即全文"源的出口：返回的原始内容会与列表行同事务落库，这些行直接进入解析阶段。
  原子性是硬要求——否则会留下"状态 3 却没有 origin"的悬空行，而这类源无法重拉。
- `NewsSpiderRegistry` 是唯一的装配点：新增源在这里加一行映射，任务、健康检查自动覆盖。
- 各源 `category` 用**独立编号段**（东财 1-22、财联社 101 起），不复用别源的语义。

## 四、已接入的源

| 源 | 标识 | 当前栏目 | 翻页模型 | 原始内容 | 备注 |
|---|---|---|---|---|---|
| 东方财富 | `DfMedia = 1` | 35 个栏目，映射到 22 个分类号 | `page_index` 页码翻页 | 详情接口 `newsinfo.eastmoney.com/kuaixun/v2/api/article/{id}` | 时间格式强绑定（见下） |
| 财联社电报 | `ClsMedia = 2` | 1 个栏目「电报」，`category = 101` | `last_time` 时间游标（严格小于） | 列表响应本身即全文，无单条接口 | 需要签名，单页上限 50 |
| 新浪财经 7x24 | `SinaMedia = 3` | 1 个栏目「7x24」，`category = 201` | `page` 页码翻页 | 列表即全文 | 无鉴权，正文以【标题】开头 |
| 华尔街见闻 live | `WscnMedia = 4` | 1 个栏目「全球宏观」，`category = 301` | 接口自带 `next_cursor` | 列表即全文 | 无鉴权，约 1/3 条目无标题 |
| 金十快讯 | `Jin10Media = 5` | 1 个栏目「快讯」，`category = 401` | `max_time` 时间游标（含边界） | 列表即全文 | 必须带客户端标识头；约 20% 为 PLUS 专享 |

> **「当前栏目」是开发进度，不是源的能力上限**。东财是开发最完整的源（35 个栏目），其余四个源目前每个只接了 1 个栏目，
> 但它们都能扩展出更多栏目：
>
> | 源 | 可扩展的栏目 | 依据 |
> |---|---|---|
> | 华尔街见闻 | `global-channel` / `a-stock-channel` / `forex-channel` / `commodity-channel` / `bond-channel` / `hk-stock-channel` / `us-stock-channel` 等 8 个以上频道 | 实测换 `channel` 参数均能取到数据 |
> | 金十 | 快讯条目自带 5 个频道分类（`channel` 字段取值 1-5） | 响应字段实测 |
> | 财联社 | 电报之外还有多个内容频道 | 接口支持 `category` 参数（v1 未验证分频道取数） |
> | 新浪 | 直播接口支持 `zhibo_id` / `tag_id` 切换不同直播与标签 | 接口参数 |
>
> **补栏目不是"加一行配置"那么轻**：这四个源目前把频道参数与 `category` 都写死在各自的 `*NewsResource` 常量里
> （`GlobalChannel` / `AllChannel` / `ZhiboId` 与 `*CategoryNumber`），`column.ColumnId` 只用于日志定位、
> 不参与取数。所以补一个栏目需要：把频道参数改为按 `NewsColumn` 传入（`ColumnId` 与接口参数值不保证一致，
> 见闻就是 `"global"` vs `"global-channel"`，需要各自定义映射）、让 `ToSpiderNewListModel` / `ParseContent`
> 接收该栏目的 `category`（否则多栏目会写成同一个分类号）、并在 `Columns` 里注册。
> 这是把一个源"从壳子做成完整源"的工作，见下"源成熟度"。

### 东方财富（`Spider/DfNews/`）

- 列表：`np-listapi.eastmoney.com/comm/web/getNewsByColumns`，按 `column`（栏目号）+ `page_index` 翻页。
- 正文：由新闻页 URL 反推文章号（兼容 `/news/<栏目号>,<文章号>.html` 与 `/a/<文章号>.html` 两种形态）再请求详情接口，返回 HTML 片段后用 HtmlAgilityPack 解析成结构化片段。
- 详情页有三种模板（`contentwrap` / `newsContent` / `content_text`），解析器按序探测，都匹配不上才判失败。
- **时间格式强绑定**：列表接口 `yyyy-MM-dd HH:mm:ss`，详情接口 `yyyy/MM/dd HH:mm:ss`，格式不匹配会抛 `FormatException` 并计入解析失败。
- 播放兜底：`Spider/DfNews/Playwright/` 保留浏览器渲染能力（栏目自检用），生产链路是纯 HTTP。

### 财联社电报（`Spider/ClsNews/`）

- 端点：`www.cls.cn/v1/roll/get_roll_list`，参数 `app=CailianpressWeb&os=web&sv=8.7.9&refresh_type=1&rn=&last_time=`。
- **签名**：`sign = MD5(SHA1(参数按 key 升序拼接的 query))`，拼接不做 URL 编码、空值参数丢弃；`sv`（前端版本号）参与签名，财联社升级前端后需要同步更新（失效表现：`errno 10012`）。算法与实测向量锁在 `ClsSignature` 与 `ClsNewsSpiderTest.SignMatchesVerifiedVector`。
- **单页上限 50**：`rn > 50` 会静默返回空数组（`errno` 仍为 0），已在 `ClsNewsResource.MaxPageSize` 钳制。
- **游标是严格小于语义**：`NextCursor` 取本页最老一条 `ctime + 1`，否则同一秒内的其它条目会被永久跳过；边界那一条会被下一页重复取回，由入库去重吸收。
- 字段映射：`news_url = https://www.cls.cn/detail/{id}`（不用响应里的 `shareurl`，它带 `sv` 参数、版本一变去重键就变）；标题为空时用 `brief` 兜底（约半数电报没有标题）；`ctime` 是 unix 秒，固定按东八区换算，不依赖宿主时区；`subjects[].subject_name` 拼成关键字。
- 实测量级：约 370 条/天，50 条约覆盖 3.3 小时；图片属低频（50 条里约 1 条带图）。

### 新浪财经 7x24（`Spider/SinaNews/`）

- 端点：`zhibo.sina.com.cn/api/zhibo/feed`，参数 `page` / `page_size` / `zhibo_id=152` / `tag_id=0` / `dire=f` / `dpc=1`；无需鉴权，`page_size` 实测可到 100。
- 正文在 `rich_text`，约 86% 以 `【标题】` 开头——解析时取【】内为标题，其余用正文前 60 字兜底。
- `docurl` 是详情页地址（清单唯一键）；实测约 1% 条目缺 `docurl`，此时用合成去重键 `https://finance.sina.com.cn/7x24/#feed-{id}`。
- `tag[].name` 是业务标签（公司 / 宏观 / 央行 / 市场 等）写进关键字；正文是纯文本，图片在 `multimedia` 字段（实测 100 条仅 1 条非空），v1 不解析。

### 华尔街见闻 live（`Spider/WscnNews/`）

- 端点：`api-one.wallstcn.com/apiv1/content/lives`，参数 `channel=global-channel` / `client=pc` / `limit`；无需鉴权。
- 翻页直接用响应的 `data.next_cursor`（不含边界，两页无重叠）。
- 字段映射：`news_url` 取 `uri`；正文用 `content_text`（纯文本，接口同时给了 `content` 的 HTML 形态，避免再解析一次）；`display_time` 是 unix 秒，固定按东八区换算；`images` 数组产图片记录。
- 约 1/3 条目没有 `title`，用正文前 60 字兜底；关键字只用业务 `tags`，频道 `channels` 是内部英文 slug 不放进关键字（原始 JSON 里保留）。

### 金十数据快讯（`Spider/Jin10News/`）

- 端点：`flash-api.jin10.com/get_flash_list`，参数 `channel=-8200`（全部快讯）/ `vip=1`，翻页用 `max_time=<时间串>`（URL 编码）。
- **必须带 `x-app-id` / `x-version` 两个头**，否则返回 502；两个值写在 `Jin10NewsResource`，被拒时对照网页端请求更新。
- 游标是**含边界**语义：`NextCursor` 取本页最老一条的 `time`，边界条目会被下一页重复取回，由入库去重吸收（不会漏条目）。
- 接口不接受页长参数，固定返回约 20 条；`id` 是时间戳风格字符串，`news_url` 用 `https://flash.jin10.com/detail/{id}`。
- **PLUS 专享条目**（实测约占 20%）`data.content` 为空、`data.lock=true`，正文需付费账号；实现用 `data.vip_title` 作为标题与正文兜底，至少保留新闻要点。是否专享可从原始 JSON 的 `data.lock` / `data.exclusive_to` 判断。
- 图片地址带尺寸后缀（形如 `.../demo.png/lite`），图片名取最后一个像文件名的路径段。

## 五、源成熟度（现状如实记录）

各源的开发完成度差异很大，评估"要不要优化管线"时必须区分**架构能力**与**当前开发进度**：

| 源 | 成熟度 | 说明 |
|---|---|---|
| 东方财富 | **完整** | 35 个栏目全部接入，详情接口 + HTML 结构化解析（段落/图片/表格/列表）、Playwright 兜底、时间格式强绑定 |
| 财联社 | **最小可用** | 只接「电报」1 个栏目；签名算法已逆向并有实测向量锁定 |
| 新浪 7x24 | **最小可用** | 只接「7x24」1 个栏目；图片字段（`multimedia`）未解析 |
| 华尔街见闻 | **最小可用** | 只接「全球宏观」1 个栏目；单条接口未利用（见已知限制） |
| 金十 | **最小可用** | 只接「快讯」1 个栏目；PLUS 专享条目只有标题 |

四个快讯源的共同短板：

1. **栏目数少**：每源 1 个，而各源实际有多个频道（见上方源明细表的可扩展栏目）。
2. **频道参数写死**：`GetListPage` 用的是 `WscnNewsResource.GlobalChannel` / `Jin10NewsResource.AllChannel` /
   `SinaNewsResource.ZhiboId` / `ClsNewsResource.TelegraphColumn` 这类常量，`column.ColumnId` 只进日志。
   多栏目需要先让频道参数按 `NewsColumn` 传入。
3. **category 写死**：`ToSpiderNewListModel()` / `ParseContent()` 里直接写 `*CategoryNumber` 常量，
   多栏目时会全部落成同一个分类号，需要把该栏目的 category 传下去（东财的做法可参考：
   `DfListInfo.Category` 由栏目资源带下来）。

因此"其余 media 只是壳子"是**当前事实**：它们的价值目前只在于验证多源框架跑得通，数据量与东财完全不在一个量级。
要真正把某个源做起来，工作量在"补栏目"而不在"加源"。

## 六、落库与去重

- 新闻三表都以 `news_url` 为唯一键，图片表以 `image_resource_url`，跨源也全局去重（同一 URL 只落一次）。
- 各表的 upsert 语义不同（列表 `DO NOTHING` / 原始内容与详情 `DO UPDATE`），原因见 [principles.md](principles.md) 的"数据访问约定"。
- `from_media` 标识来源，`category` 是源内部栏目分类号。
- 结构化内容片段（`spider_news_content.news_content_json`）是跨源共用的格式，定义为 `NewsContentSegment`：

| 字段 | 含义 |
|---|---|
| `ValueType` | `TEXT` / `IMG` / `TABLE` / `UL` / `OTHER`（字符串常量，已入库数据依赖这些取值，不要改） |
| `Value` | 文本内容；表格为二维数组 JSON；列表为一维数组 JSON |
| `ResourceUri` | 图片等资源的地址 |
| `TagType` | 源侧原始标签名，仅作溯源参考 |

## 七、健康检查与巡检

`NewsCheckJob`（每 5 分钟）：

1. 遍历每个源的每个栏目，抓一页（10 条）验证接口仍返回有效数据；空数据即记错误日志——防的是"接口看起来 200 但内容没了"的静默失效。
2. 按源统计各状态数量，并输出全库最老未处理新闻的时间（用于判断积压）。

人工巡检常用 SQL：

```sql
-- 分源积压概况 ( 应与 NewsCheckJob 日志一致 )
SELECT from_media, download_status_code, count(*) FROM spider_news_list GROUP BY 1, 2 ORDER BY 1, 2;

-- 悬空行巡检 : 已置为已下载却没有 origin ( 快讯型源必须为 0 )
SELECT count(*) FROM spider_news_list l
LEFT JOIN spider_news_content_origin o ON o.news_url = l.news_url
WHERE l.download_status_code IN (3, 1) AND o.id IS NULL;

-- 失败重试已达上限的终态行 ( 需要人工判断是源改版还是脏数据 )
SELECT from_media, download_status_code, count(*) FROM spider_news_list
WHERE fail_count >= 3 AND download_status_code IN (2, 4) GROUP BY 1, 2;
```

接口层面的验证先跑连通性用例：

```bash
dotnet test src/k-spider-test/k-spider-test.csproj --filter "TestCategory=Live"
```

## 八、已知限制

| 限制 | 说明 | 可能的改进方向 |
|---|---|---|
| 栏目粒度信息未落库 | 东财 35 个栏目映射到 22 个分类号，部分栏目共用分类号（如 大盘分析 / 板块聚焦 / 热门股追踪 / 主力动态 / 港股聚焦 都记为 `CategoryHkStock = 9`），库里无法区分具体栏目 | 列表表增加"源内部栏目标识"列 |
| 财联社电报的追加更新抓不到 | 内容只在首次发现时落一次，源侧后续补充（`modified_time` 变化）不会回填 | 对最近 N 小时条目做重扫并 upsert 覆盖 |
| 快讯型源的 origin 丢失即终态 | 没有按 id 重拉接口，origin 缺失时只能退回重试到上限 | 保留兜底归档或按时间窗重扫 |
| 广告条目未过滤 | 响应里有 `is_ad` 字段，当前未做判断（实测 40 条真实数据里无广告） | 出现广告时在列表阶段过滤 |
| 金十 PLUS 专享条目只有标题 | 实测约 20% 条目正文需付费账号，实现用 `vip_title` 兜底，正文与标题相同 | 需要正文时接付费通道，或在下游按 `data.lock` 过滤 |
| 新浪快讯图片未解析 | 图片在 `multimedia` 字段，实测 100 条仅 1 条非空 | 出现高频图片时补该字段解析 |
| 跨源同题材重复 | 同一事件常被多源报道（如"德国政府缓解油价"同时出现在财联社 / 见闻 / 金十），当前只按 `news_url` 去重，不做内容级合并 | 需要时按标题 / 正文指纹做跨源归并 |
| 跨源 URL 碰撞未防护 | 四个快讯源共用 `spider_news_content_origin` 表且都用 `ON CONFLICT (news_url) DO UPDATE`。若两个源产出同一 URL（当前实测各源域名互不重叠，尚未发生），后写的会覆盖先写的原始内容，而两源解析器不同（东财是 `Art_Content` 的 JSON、快讯源是条目 JSON），覆盖后解析必然失败。另外三张表（`content_origin` / `content` / `image_list`）没有 `from_media` 列，无法按源隔离 | 真出现碰撞时给这三张表加 `from_media` 并在 `ON CONFLICT` 里带上，而不是改唯一键语义（`UNIQUE(news_url)` 是全局去重的保障，改成 media+url 反而允许重复落库） |
| 图片只记 URL 不下载 | `spider_news_image_list` 存的是资源地址与文件名，`DfContentSpider` 里下载逻辑是注释状态 | 需要离线留存时再启用 |
| 原文与图片表只增不删 | `spider_news_content_origin` 存整篇原始响应，长期运行需要归档 | 用 `db/optimization.sql` 的清理段做定期治理 |
