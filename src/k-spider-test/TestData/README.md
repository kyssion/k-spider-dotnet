# TestData — 真实抓取响应夹具

解析回归测试用的**真实接口响应**（原样保存，未做字段裁剪）。测试通过 `AppContext.BaseDirectory/TestData` 读取，
csproj 已配置 `CopyToOutputDirectory=PreserveNewest`，因此测试仍然完全离线。

| 文件 | 来源接口 | 抓取时间 | 内容 |
|---|---|---|---|
| `cls_roll_page1.json` | `GET https://www.cls.cn/v1/roll/get_roll_list?app=CailianpressWeb&last_time=0&os=web&refresh_type=1&rn=20&sv=8.7.9&sign=…` | 2026-09-19 01:51 (东八区) | 财联社电报最新 20 条 |
| `cls_roll_page2.json` | 同上，`last_time=1789746672`（page1 最老一条 ctime + 1） | 2026-09-19 01:51 | 财联社电报第二页 20 条，用于游标连续性用例 |
| `cls_roll_with_image.json` | 同上，`rn=50` 后摘出带图的那一条（id=2487578） | 2026-09-19 01:52 | 财联社电报带图条目（正文图 1 张 + 封面同图） |
| `cls_depth_list_1000_page1.json` | `GET https://www.cls.cn/v3/depth/list/1000?app=…&id=1000&last_time=0&os=web&rn=20&sv=8.7.9&sign=…` | 2026-09-25 03:00 (东八区) | 财联社「头条」频道文章列表 55 行（服务端不按 rn 裁剪） |
| `cls_depth_list_1000_page2.json` | 同上，`last_time=1790247667`（page1 末条 ctime；列表按 SortScore 混排，末条并非最老时间） | 2026-09-25 03:00 | 「头条」第二页 30 行，与 page1 有 13 行重叠（重叠靠入库去重吸收） |
| `cls_article_detail.html` | `GET https://www.cls.cn/detail/2492814`（页面 HTML 原样保存） | 2026-09-25 03:00 | 财联社文章详情页（SSR），正文在 `__NEXT_DATA__` 的 `articleDetail.content`，标题《谷歌TPU，下周出发去太空》 |
| `cls_article_detail_rich.html` | `GET https://www.cls.cn/detail/2492703`（页面 HTML 原样保存） | 2026-09-25 03:30 | 含 `blockquote` 引用块的文章详情页（批量实测 20 篇中顶层标签分布：p/strong/img/a/h1-h3/blockquote），标题《甲骨文重磅项目现风险信号：据称正为数据中心延期留后路》 |
| `df_list_344.json` | `GET https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?…&column=344&page_index=1&page_size=20&…` | 2026-09-19 01:52 | 东方财富「财经导读」栏目列表 20 条 |
| `df_article_real.json` | `GET https://newsinfo.eastmoney.com/kuaixun/v2/api/article/202609183878840472?guid=…` | 2026-09-19 01:52 | 东方财富正文接口（含图片段落，正文 9827 字符） |
| `sina_live_page1.json` / `sina_live_page2.json` | `GET https://zhibo.sina.com.cn/api/zhibo/feed?page=1/2&page_size=20&zhibo_id=152&tag_id=0&dire=f&dpc=1` | 2026-09-19 11:06 (东八区) | 新浪 7x24 快讯两页各 20 条 |
| `sina_article_roll_page1.json` / `sina_article_roll_page2.json` | `GET https://feed.mix.sina.com.cn/api/roll/get?pageid=153&lid=2516&k=&num=50&page=1/2` | 2026-09-29 00:41 (东八区) | 新浪财经滚动接口（综合 lid=2516）两页各 50 条，页间零重叠；首页首条《财报前夕，美光"超级多头"重申2000美元目标价》（ctime=1790613243） |
| `sina_article_column_56592.html` | `GET https://finance.sina.com.cn/roll/c/56592.shtml`（页面 HTML 原样保存） | 2026-09-29 00:41 | 「上市公司」栏目滚动页（SSR 整页列表），200 条 li 里 14 条无日期路径（覆盖跳过分支），首条《准万亿城市"卡位战"，悬念再起》（09月28日 23:54） |
| `sina_article_detail.html` | `GET https://finance.sina.com.cn/stock/usstock/c/2026-09-29/doc-initmeav9622448.shtml`（页面 HTML 原样保存） | 2026-09-29 00:41 | 新浪文章详情页（SSR），正文在 `div#artibody`，含 1 张 `div.img_wrapper` 正文图与文末 appendQr 推广二维码块（锁定"二维码不入图片列表"），标题《财报前夕，美光"超级多头"重申2000美元目标价》 |
| `sina_article_detail_rich.html` | `GET https://finance.sina.com.cn/stock/usstock/c/2026-09-28/doc-initmeau2811260.shtml`（页面 HTML 原样保存） | 2026-09-29 00:41 | 含 `blockquote` 引用块的文章详情页，标题《奥多比预测今年美国假日季线上购物将创历史新高》 |
| `wscn_live_page1.json` / `wscn_live_page2.json` | `GET https://api-one.wallstcn.com/apiv1/content/lives?channel=global-channel&client=pc&limit=20`（第二页带 `cursor=1789785930`） | 2026-09-19 11:06 | 华尔街见闻 live 两页各 20 条 |
| `jin10_flash_page1.json` / `jin10_flash_page2.json` | `GET https://flash-api.jin10.com/get_flash_list?channel=-8200&vip=1`（带 `x-app-id` / `x-version` 头，第二页带 `max_time`） | 2026-09-19 11:06 | 金十快讯两页各 20 余条，含 4 条 PLUS 专享（正文锁定）条目 |

维护方式：接口改版或解析逻辑变更时重新抓一份覆盖同名文件，并同步用例里依赖夹具的固定值
（财联社游标 `1789746672`、正文时间 `2026/09/19 01:09:08` 等）。

## 反爬验证特征样本（`verify_*.html`）

下面这批**不是抓取回来的响应**，而是按各反爬方案的公开特征构造的最小样本，用于回归 `Spider/Verify/` 的识别器。
这么做的原因：真实挑战页只在被拦时出现（不可控），反复触发还会对目标站造成压力；
而识别器要守的是"**特征命中**"与"**不误判**"两件事 —— 前者用构造样本锁定，
后者由上面那批真实业务响应夹具锁定（`VerificationDetectorTest.RealBusinessPayloadIsNotChallenge` 把全部真实夹具跑一遍断言不误判），两侧互补。

| 文件 | 对应验证方式 | 锁定的特征 |
|---|---|---|
| `verify_cloudflare_challenge.html` | Cloudflare 托管挑战 | `Verifying you are human` / `cf-turnstile` / `_cf_chl_opt` / `cdn-cgi/challenge-platform` |
| `verify_jsl_clearance.html` | 加速乐（JS 计算 cookie） | `__jsl_clearance` |
| `verify_acw_sc_v2.html` | 阿里云盾（JS 计算 cookie） | `acw_sc__v2` |
| `verify_slider_aliyun.html` | 阿里云盾滑块（旧版 NoCaptcha） | `nc-container` / `nc_scale` / `nc_1_n1z` |
| `verify_aliyun_waf_captcha.html` | 阿里云 WAF 人机验证页（新版 AliyunCaptcha） | `aliyunCaptcha` / `aliyunCaptcha-sliding-slider` / `aliyun_waf_aa`；实测样本来自财联社 `/detail/*`（2026-09-25，见 `docs/anti-bot-verification.md`） |
| `verify_slider_geetest.html` | 极验滑块 | `geetest_panel` / `geetest_slider_button` |
| `verify_image_captcha.html` | 图形验证码 | `captcha_img` / `name="captcha"` / `看不清，换一张` |
| `verify_sms_captcha.html` | 短信验证码 | `smsCode` / `获取验证码` |

维护方式：识别器特征变更时同步更新对应样本与用例；样本只保留判定所需的最小结构，不要往里加无关内容
（无关内容会掩盖"这个特征是否真的必要"）。
