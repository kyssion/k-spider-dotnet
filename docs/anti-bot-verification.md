# 反爬验证识别与通过

本文记录 `Spider/Verify/` 模块的设计：为什么需要它、运行时怎么走、契约是什么、怎么扩展、有哪些限制。

## 一、要解决的问题

五个源全部是**纯 HTTP 抓取**（`Common/Http/HttpClientTools` 的伪装头），没有浏览器兜底。一旦源站上了反爬，抓取侧的表现是：

| 表现 | 原来的后果 |
|---|---|
| 403 / 429 / 503 | 抛 `HttpRequestException` → 该源本轮全失败，日志里只有状态码 |
| JS 计算 cookie 门禁（加速乐 / 阿里云盾 / 瑞数） | 拿到的是薄壳页 HTML，解析器报"响应不是合法 JSON" |
| Cloudflare 类 JS 挑战 | 同上，且挑战页是 200，看起来"请求成功了" |
| HTTP 200 但载荷报风控 | 解析器报"响应缺少 data"，**最危险**：接口看着正常，一条数据都没有 |
| 滑块 / 图形 / 短信验证码 | 同上 |

这类故障的共同点是**看起来像解析问题，实际是访问被拦**。`Spider/Verify/` 要做的事：

1. **识别**：把"这次响应被反爬拦住了、属于哪一类"从响应里判出来，而不是让它伪装成解析失败；
2. **通过**：能自动过的（JS / Cloudflare 类挑战）自动过掉并继续抓，抓取侧代码不用改；
3. **如实报告**：过不了的明确升级告警（`NewsCheckJob` 汇总），**绝不悄悄返回空数据** —— 与 `NewsCheckJob` 防"静默失效"是同一个出发点。

> 使用边界：本模块只用于抓取公开数据、突破"频率限制 / 人机校验"这类技术门槛。抓取前请确认目标站的 robots 与服务条款允许，并保持默认的抓取节奏（不为了过验证而放大请求量）。

## 二、运行时形态

```
Spider/*  ( 各源爬虫 )
   │  VerifiedHttp.GetStringAsync / SendStringAsync        ← 抓取层唯一 HTTP 入口
   ▼
HTTP 请求 ( 自动带当前主机已取得的会话 cookie )
   │  响应 → VerificationProbe ( 状态码 + 响应头 + 响应体取样 64KB )
   ▼
VerificationPipeline.Detect        识别器注册表 ( 5 个 , 按具体度排序 , 命中即停 )
   │
   ├─ 未命中 ─▶ 正常返回 ( 只多跑一遍纯字符串判定 , 无网络与浏览器开销 )
   │
   └─ 命中 ─▶ VerificationPipeline.HandleAsync  策略按 Cost 升序尝试
                ├─ BrowserChallengeSolver ( 10 )  浏览器跑一遍 JS / Cloudflare 挑战 → 导出 cookie
                ├─ BrowserSliderSolver    ( 20 )  滑块拖拽 ( 默认策略不放行 )
                └─ ManualEscalationSolver ( 99 )  兜底 : 明确告警 + 要求人工介入
                        │
                        ├─ 成功 ─▶ VerificationSessionStore 落会话 → 重放请求 ( 最多 MaxSolveAttempts 次 )
                        └─ 失败 ─▶ 该主机进冷却期 → VerificationRequiredException ( 带验证类型 )
```

两条硬约束贯穿整个模块：

- **同一个主机串行处理**：多源任务并行跑（`NewsFlashJob` 4 源并发、`NewsContentOriginJob` 每 3 秒一轮），不串行化就会同时起好几个浏览器，把目标站压出更强的风控。
- **失败后冷却**（默认 10 分钟）：冷却期内只识别、只告警、不再尝试，避免对目标站持续施压。限流（429）按源站给的 `Retry-After` 退避（服务端说 60 秒就退 60 秒，不被策略默认值盖过去）；`Retry-After` 的两种写法（秒数 / HTTP 日期）都认。
- **会话回放走 handler 的 `CookieContainer`，不手动拼 `Cookie` 头**：手动加头会与容器里的同名旧值拼成两份同名 cookie（服务端取哪份未定义，且重复 cookie 本身就是注入指纹）。统一由容器拼装，既消掉重复，也保住了服务端自己下发的 cookie（如财联社 WAF 的 `acw_tc`）的回传。见 `HttpClientTools.ApplyCookies` 与 `VerifiedHttpTest`。

## 三、契约

目录按角色划分，每个区一条准入判据：

| 区 | 准入判据 | 内容 |
|---|---|---|
| 根 | 外部调用方需要认识的 | 两个扩展点接口 + `VerifiedHttp` / `VerificationRegistry` / `VerificationPolicy` |
| `Model/` | 被 ≥2 个区引用、纯数据无行为 | `VerificationKind`(+Traits) / `VerificationProbe` / `VerificationChallenge` / `VerificationSolving`(Request+Result) / `VerificationOutcome`(+Block) / `VerificationSession` |
| `Pipeline/` | 协调其它组件或持有时效状态 | `VerificationPipeline` + `VerificationSessionStore` |
| `Detector/` | 回答"被拦住了吗、是哪种" | 识别器实现（纯判定、不联网） |
| `Solver/` | 回答"怎么过" | 策略实现；`Solver/Browser/` 收纳依赖 Playwright 的手段与公共基建（`BrowserGate`） |

依赖不变量：**家族（Detector / Solver）只依赖根契约与 Model，两个家族互不引用**；跨家族协作（浏览器策略重跑识别器）经根接口注入完成。

| 类型 | 职责 |
|---|---|
| `IVerificationDetector` | 识别器：`Detect(VerificationProbe) → VerificationChallenge?`。**纯判定、不联网**，否则没法离线回归 |
| `IVerificationSolver` | 通过策略：声明能处理哪些 `VerificationKind` 与 `Cost`，`SolveAsync` 成功时交出可复用的 `VerificationSession` |
| `VerificationKind` | 识别与处理之间**唯一的耦合面**：新增一种验证方式只需加枚举值 + 一个识别器 + 一个策略，两侧互不引用 |
| `VerificationProbe` | 一次响应的可判定信息（url / 状态码 / 头 / 响应体取样）。刻意不依赖 `HttpResponseMessage`，因此识别器可以离线用夹具跑 |
| `VerificationPolicy` | 按主机控制：是否开启、放行哪些类型、重试与超时上限、冷却与会话 TTL |
| `VerificationSessionStore` | 已通过验证的会话缓存（按主机），进程内复用 |
| `VerificationPipeline` | 编排：识别 → 选策略 → 逐个尝试 → 落会话 / 进冷却。**实例持有状态**，生产用 `VerificationRegistry.Pipeline`，测试可构造独立实例 |
| `VerifiedHttp` | 抓取层门面：带会话、被拦自动过验证并重放请求 |
| `VerificationRequiredException` | 过不了时抛出，带 `VerificationKind`，按"这条数据失败"处理（消耗 `fail_count`） |

## 四、内置识别器

`VerificationRegistry` 里的注册顺序 = 判定优先级，**特征越具体的越靠前**（否则 403 挑战页会被笼统的"拒绝访问"先截胡）。

| 识别器 | 判定类型 | 依据 |
|---|---|---|
| `CloudflareDetector` | `CloudflareChallenge` | `cf-mitigated` 响应头；或挑战页特征（`Just a moment` / `cf_chl_` / `challenge-platform` / `cf-turnstile` 等） |
| `JsCookieGateDetector` | `JsCookieGate` | 厂商脚本特征（`__jsl_clearance` 加速乐 / `acw_sc__v2` 阿里云盾 / `$_ts` 瑞数 / `safeline` 雷池）；或通用兜底：8KB 以内的薄壳页里 `document.cookie` + 跳转 |
| `CaptchaDetector` | `SliderCaptcha` / `SmsCaptcha` / `ImageCaptcha` | 厂商 DOM 标识（`nc_1_n1z` / `geetest_slider_button` / `captcha_img` / `smsCode` 等）；中文提示语需配合表单元素 |
| `JsonRiskControlDetector` | `RiskControl` | 非 HTML 载荷里**只沿对象层级**（不进业务数据数组）找 `msg` / `message` / `error` 等提示字段，命中风控措辞（访问频繁 / 被拦截 / forbidden 等） |
| `HttpGateDetector` | `AccessDenied` / `RateLimited` / `ServerGate` / `VerifyRedirect` | 401 / 403 / 412；429；503 仅在带 `Retry-After` 时判（否则可能是服务真挂了）；3xx 跳转目标含验证语义 |
| `HttpGateDetector` | `AccessDenied` / `RateLimited` / `ServerGate` / `VerifyRedirect` | 401 / 403 / 429；503 仅在带 `Retry-After` 时判（否则可能是服务真挂了）；3xx 跳转目标含验证语义 |

**误判比漏判更贵**（误判 = 白起一次浏览器 + 这条数据失败 + 进冷却），因此识别器刻意保守：

- 内容型识别器**只对 HTML 生效**：五个源的正常业务响应都是 JSON，若对 JSON 也跑页面特征匹配，一篇提到验证码的新闻就会判成被拦截。
- 中文泛词不作特征：`验证码` / `图形验证码` 这类提示语必须配合 `<form>` / `<input>` 才算数（`CaptchaDetector` 的两档特征）。
- **风控提示只在响应 envelope 上找，不进业务数据数组**：业务数据都在数组里（新闻条目列表），进去扫等于把每条新闻的 `desc` / `info` 当风控提示读——一条含 `risk` 字样的英文快讯就能让整个源误判进冷却。
- 词表里不放中性词：裸 `risk` / `verify`（英文里太常见）、`校验` / `请求异常`（源站自己的参数或内部报错）都不算风控。
- 财联社的 `签名错误`（errno 10012）**不归**风控：它是前端版本号过期，有独立处理路径（见 [news-pipeline.md](news-pipeline.md)），归到风控会把运维引向错误方向。
- 回归用例里把五个源的真实响应夹具全部跑一遍断言"不误判"（`VerificationDetectorTest.RealBusinessPayloadIsNotChallenge`）。

## 五、内置通过策略

| 策略 | Cost | 处理类型 | 做法 |
|---|---|---|---|
| `BrowserChallengeSolver` | 10 | `CloudflareChallenge` / `JsCookieGate` / `BrowserCheck` / `VerifyRedirect` / `AccessDenied` / `ServerGate` | 用 Playwright 打开被拦地址（新 headless + UA/platform/languages 与 HTTP 链路一致），每秒取一次 DOM 重跑识别器，识别不到挑战即认为已放行，按域导出 cookie 交给 HTTP 链路复用 |
| `BrowserSliderSolver` | 20 | `SliderCaptcha` | 两段式：先每秒重跑识别器等挑战脚本自动放行（阿里云 WAF 类挑战页对干净指纹的浏览器不弹滑块、脚本直接放行，财联社 `/detail/*` 实测如此），等不到再按选择器表探测滑块与轨道（含新版阿里云 WAF 的 `aliyunCaptcha-sliding-slider`），用"先快后慢 + 纵向抖动 + 松手前微调"的轨迹拖动一次，再重跑识别器确认 |
| `ManualEscalationSolver` | 99 | 全部 | 兜底：按验证类型给出处置建议并记错误日志，返回 `NeedsManualAttention` |

**限流（`RateLimited`）与载荷级风控（`RiskControl`）不在放行集里**：这两类没有任何自动手段，唯一合理的响应是退避（进冷却期，到点自动重试），所以既不进 `VerificationPolicy.DefaultAllowedKinds`，也不会升级成"需要人工介入"（见 `VerificationKindTraits.ResolvesByWaiting`）。

**浏览器只在被拦截时按需启动**，正常请求仍是纯 HTTP。过验证拿到的 cookie 存进 `VerificationSessionStore`，后续请求自动带上，不必每个请求都过一遍。

## 六、扩展套路

### 新增一种识别方式

1. 在 `VerificationKind` 加枚举值；
2. 写一个 `IVerificationDetector` 实现（只做纯判定，不联网）；
3. 在 `VerificationRegistry.DetectorList` 按"越具体越靠前"的位置加一行；
4. 在 `src/k-spider-test/TestData/` 放一个特征样本，在 `VerificationDetectorTest` 加命中用例，并把真实业务响应夹具补进"不误判"用例。

### 新增一种通过手段

1. 写一个 `IVerificationSolver` 实现，声明 `Kinds` 与 `Cost`（越小越先试）；
2. 在 `VerificationRegistry.SolverList` 加一行；
3. 若它处理的是默认不放行的类型（人机确认类），在 `VerificationPolicy.DefaultAllowedKinds` 里**不要**加 —— 让部署方按源显式放开。

### 按源调整策略

```csharp
// 关闭某源的自动过验证 ( 只识别并告警 )
VerificationRegistry.SetPolicy(DfNewsResource.ListResourceHost, VerificationPolicy.Disabled);

// 放行滑块自动处理 ( 确认自己有权抓取该站后再放开 )
VerificationRegistry.SetPolicy("www.example.com", VerificationPolicy.WithKinds(VerificationKind.SliderCaptcha));
```

`VerificationRegistry` 是唯一装配点，管线、抓取门面与健康检查都从这里取，调用方无需改动。

当前仓库里真实放开的只有一处——财联社（`Program.cs`）：`/detail/*` 详情页被阿里云 WAF 人机验证拦截（2026-09-25 实测，纯 HTTP 一律拿到 200 + 滑块页，间歇性出现；对干净指纹的浏览器脚本会自动放行，不弹滑块），按源追加 `SliderCaptcha` 后由 `BrowserSliderSolver` 两段式处理。挑战页样本见 `TestData/verify_aliyun_waf_captcha.html`。

## 七、关键设计决策

| 决策 | 原因 | 代价 |
|---|---|---|
| 识别与处理用一个枚举解耦 | 新增验证方式只加识别器、新增手段只加策略，两侧独立演进 | 枚举成为共享词汇表，加值时要想清楚归属 |
| 识别器纯判定、不发起网络请求 | 可以拿夹具离线回归；这是"不误判"能守住的前提 | 需要把判定所需的响应信息（头 / 状态码 / 体取样）统一装进 `VerificationProbe` |
| 内容型识别器只对 HTML 生效 | 五个源正常响应都是 JSON，对 JSON 跑页面特征必然误判 | 若将来出现返回 HTML 业务数据的源，需要在识别器里区分"业务 HTML"与"挑战 HTML" |
| 浏览器渲染而不是复刻挑战算法 | 挑战脚本随时会变，复刻必然过时；浏览器执行是唯一稳定的通过方式 | 需要目标机安装 Chromium（`playwright install chromium`） |
| 浏览器走完整 Chromium 的新 headless + 抹掉自动化特征 | 默认的 `chrome-headless-shell`（旧 headless）是最容易被识别的一档：`navigator.webdriver=true`、`platform` 报宿主系统而 UA 声称 macOS、`languages` 与 `Accept-Language` 不一致。实测切到新 headless 并加初始化脚本后，`platform=MacIntel`、`languages=zh-CN,zh,en`、`window.chrome` 存在、插件数与真机一致（5） | 只是"降低特征"而非完整指纹伪装（WebGL 渲染器、字体、时区库等仍可能与真机不同）；每次启动多几十毫秒 |
| 会话回放走 `CookieContainer`，不手动拼 `Cookie` 头 | 手动加头会与容器里的同名旧值拼成两份同名 cookie（服务端取哪份未定义，重复 cookie 本身也是注入指纹）；统一由容器拼装还能保住服务端下发 cookie（如财联社 WAF 的 `acw_tc`）的回传 | 回放的 cookie 进了 handler 的全局容器，`ApplyCookies` 会按名清理同名旧值——改这段代码要连带看 `VerifiedHttpTest` |
| 请求头（UA 等）不回放，只由 `HttpClientTools` 统一伪装 | 两边指纹必须一致才不会又被拦；浏览器侧复用同一串 UA 常量 | 会话里不保存 UA，新增浏览器策略时不要自己另写 UA |
| 按主机串行 + 失败冷却 | 多源并行 + 3 秒轮询会同时起多个浏览器，等于对目标站加压 | 一个主机被拦时该主机的其它请求会短暂等待 |
| 被 cookie 类验证拦住即丢弃会话 | 说明手上这份会话已不被服务端接受，留着会让冷却期内每个请求都拿死 cookie 去撞 | 限流 / 风控与 cookie 无关，不动会话（否则下一个请求白多过一次验证） |
| 失败抛带类型的异常而不是返回空 | 静默返回空数据是最难发现、代价最大的故障形态 | 过验证失败会消耗该条数据的 `fail_count`（达上限后停为终态） |
| 滑块 / 图形 / 短信默认不自动处理 | 这三类要的是人机确认，默认自动化不合适；但识别与告警仍然生效 | 需要按源显式放开滑块（`WithKinds` 是**追加**而非替换）；图形 / 短信只能人工 |

## 八、已知限制

| 限制 | 说明 |
|---|---|
| 需要 Chromium | 浏览器策略依赖 `playwright install chromium`；未安装时策略失败并在日志里给出该提示，抓取链路不受影响（其余策略与识别仍然工作） |
| 指纹只是"降低特征" | 已对齐 UA / platform / languages / `window.chrome`，但 WebGL 渲染器、字体列表、`deviceMemory` 等仍可能与真机不同；对叠加了深度设备指纹的源（瑞数高版本等）不保证通过 |
| 滑块不保证一次通过 | 各家 DOM 与轨迹校验差异大，且可能叠加行为风控（鼠标轨迹模型、设备指纹）。失败会落到人工升级告警，不会静默丢数据 |
| 图形 / 短信验证码不自动处理 | 需要识图或人工确认，模块只识别并升级告警 |
| 会话是进程内缓存 | 重启后失效（重启后首个请求再过一次验证）；多实例部署时各实例各自维护会话。**冷却状态同样只在进程内**，重启即清空 |
| 出口 IP 被拉黑无解 | 403 大面积出现且浏览器也过不去时，通常要换出口 IP，模块层面无能为力 |
| 无法应对行为风控与登录墙 | 指纹校验、需要登录才能访问的接口不在此模块范围内 |
| 冷却期内该源持续失败 | 冷却期只识别不处理，该源的数据在此期间按失败计数，达 `MaxFailCount` 后停为终态；恢复抓取需等冷却结束（或人工处理）。限流 / 风控按退避处理，不消耗人工介入的注意力 |
| `VerifyRedirect` 在 HTTP 链路几乎不触发 | handler 默认自动跟随 3xx，管线看到的是最终页，真被赶去验证页时由内容识别器兜住。该分支是为将来关掉自动跟随留的 |
| GBK 挑战页的中文特征会失效 | 响应头没带 charset 时按 UTF-8 解码，GBK 薄壳页里的中文提示语会变乱码；厂商的 ASCII 标识（`__jsl_clearance` / `acw_sc__v2` 等）不受影响，故优先级低 |

## 九、验证与回归

| 用例 | 覆盖 |
|---|---|
| `VerificationDetectorTest` | 各形态挑战页能识别出正确类型；**五个源的真实响应夹具全部不误判**；签名错误 / 参数校验 / 业务条目里的 `risk` 都不算风控；正文提到验证码不算验证页；`Retry-After` 的两种写法都能解析 |
| `VerificationPipelineTest` | 通过后落会话、按代价升序尝试、全部失败进冷却并升级告警、策略禁止时只识别、限流与风控退避且不升级人工、`WithKinds` 是追加、cookie 类挑战丢弃失效会话（无关类型保留）、策略超时降级、并发调用复用会话 |
| `VerificationSessionStoreTest` | 存取、过期清理、大小写不敏感、cookie 头拼接 |
| `VerifiedHttpTest` | 本地回环服务观测**服务端实际收到的 Cookie 头**：会话回放只发一份且是新值，容器里的同名旧值不残留 |

全部离线，随 `./scripts/verify.sh` 与 CI 执行。挑战页夹具是**按公开特征构造的样本**（触发真实挑战不可控，且反复触发会对目标站造成压力），来源与说明登记在 `src/k-spider-test/TestData/README.md`；"不误判"用例用的则是真实抓取响应，两侧互补。

**浏览器策略（`Solver/Browser/`）不在离线门禁内**：它依赖真实 Chromium，跑起来是秒级进程启动，放进确定性门禁不合适。改动浏览器策略后手工验证一次（起一个本地自清除挑战页 → 跑 `BrowserChallengeSolver.SolveAsync` → 断言拿到放行 cookie），或直接观察被拦源恢复抓取的日志：成功时输出 `[BrowserChallengeSolver] 通过 ... 拦截`，失败时输出带 `playwright install chromium` 提示的 `浏览器启动 / 加载失败`。滑块策略同理：本地起两个页面 —— 自动放行页（特征若干秒后由脚本移除）与可拖拽滑块页（拖到轨道右端才清除）—— 分别覆盖 `BrowserSliderSolver` 的两段式路径（本次改动即按此方式验证：自动放行 3.9s / 拖拽通过 6.6s，消息分别为"挑战脚本自动放行"与"滑块通过"）。
