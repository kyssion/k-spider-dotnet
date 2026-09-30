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
| `cls_pinjian_assembled_page1.json` | `GET https://www.cls.cn/v5/web/pinjian/assembled2?app=…&os=web&rn=100&sv=8.7.9&sign=…`（签名算法同电报） | 2026-09-30 (东八区) | 财联社「品见」拼装流整页（5 个专题：11 个置顶专题卡 ctype=1 + 24 篇真实文章 ctype=0，rn/last_time 不影响返回即整页全量无翻页）；真实文章与财联社全局 id 同空间，首条《潮讯 | 世界级酒吧齐聚SIP鸡尾酒节…》（id=2482499，ctime=1789378891，author=责编：若瑜） |
| `df_list_344.json` | `GET https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?…&column=344&page_index=1&page_size=20&…` | 2026-09-19 01:52 | 东方财富「财经导读」栏目列表 20 条 |
| `df_article_real.json` | `GET https://newsinfo.eastmoney.com/kuaixun/v2/api/article/202609183878840472?guid=…` | 2026-09-19 01:52 | 东方财富正文接口（含图片段落，正文 9827 字符） |
| `sina_live_page1.json` / `sina_live_page2.json` | `GET https://zhibo.sina.com.cn/api/zhibo/feed?page=1/2&page_size=20&zhibo_id=152&tag_id=0&dire=f&dpc=1` | 2026-09-19 11:06 (东八区) | 新浪 7x24 快讯两页各 20 条 |
| `sina_article_roll_page1.json` / `sina_article_roll_page2.json` | `GET https://feed.mix.sina.com.cn/api/roll/get?pageid=153&lid=2516&k=&num=50&page=1/2` | 2026-09-29 00:41 (东八区) | 新浪财经滚动接口（综合 lid=2516）两页各 50 条，页间零重叠；首页首条《财报前夕，美光"超级多头"重申2000美元目标价》（ctime=1790613243） |
| `sina_article_column_56592.html` | `GET https://finance.sina.com.cn/roll/c/56592.shtml`（页面 HTML 原样保存） | 2026-09-29 00:41 | 「上市公司」栏目滚动页（SSR 整页列表），200 条 li 里 14 条无日期路径（覆盖跳过分支），首条《准万亿城市"卡位战"，悬念再起》（09月28日 23:54） |
| `sina_article_detail.html` | `GET https://finance.sina.com.cn/stock/usstock/c/2026-09-29/doc-initmeav9622448.shtml`（页面 HTML 原样保存） | 2026-09-29 00:41 | 新浪文章详情页（SSR），正文在 `div#artibody`，含 1 张 `div.img_wrapper` 正文图与文末 appendQr 推广二维码块（锁定"二维码不入图片列表"），标题《财报前夕，美光"超级多头"重申2000美元目标价》 |
| `sina_article_detail_rich.html` | `GET https://finance.sina.com.cn/stock/usstock/c/2026-09-28/doc-initmeau2811260.shtml`（页面 HTML 原样保存） | 2026-09-29 00:41 | 含 `blockquote` 引用块的文章详情页，标题《奥多比预测今年美国假日季线上购物将创历史新高》 |
| `wscn_article_list_page1.json` / `wscn_article_list_page2.json` | `GET https://api-one-wscn.awtmt.com/apiv1/content/articles?limit=30`（第二页带 `cursor=1790663006,1790651700`） | 2026-09-29 14:24 (东八区) | 见闻文章全量流两页各 30 条，页间零重叠；首页首条《加入个人AI Agent大战！豆包被曝将推个人助理产品"Spell"，4月已内测》（display_time=1790663006） |
| `wscn_article_detail.json` | `GET https://api-one-wscn.awtmt.com/apiv1/content/articles/3782700?extract=0` | 2026-09-29 14:24 | 免费长文详情（正文约 3700 字 + 1 图），标题《"用户日增速10%"！23岁天才辍学生造出Meta Muse最大劲敌，14人团队》 |
| `wscn_article_detail_paid.json` | `GET https://api-one-wscn.awtmt.com/apiv1/content/articles/3782635?extract=0` | 2026-09-29 14:24 | 付费文详情（is_priced，正文截断约 700 字），锁定"付费预览仍解析入库"路径 |
| `wscn_live_page1.json` / `wscn_live_page2.json` | `GET https://api-one.wallstcn.com/apiv1/content/lives?channel=global-channel&client=pc&limit=20`（第二页带 `cursor=1789785930`） | 2026-09-19 11:06 | 华尔街见闻 live 两页各 20 条 |
| `jin10_flash_page1.json` / `jin10_flash_page2.json` | `GET https://flash-api.jin10.com/get_flash_list?channel=-8200&vip=1`（带 `x-app-id` / `x-version` 头，第二页带 `max_time`） | 2026-09-19 11:06 | 金十快讯两页各 20 余条，含 4 条 PLUS 专享（正文锁定）条目 |
| `jin10_article_list_28.json` / `jin10_article_list_30.json` / `jin10_article_list_53.json` | `GET https://reference-api.jin10.com/reference?nav_bar_id=28/30/53&page=1&page_size=20`（带 `x-app-id: irINJPgCgrndSp0F` / `x-version: 1.0.1` 头） | 2026-09-29 18:10 (东八区) | 金十「市场参考」文章列表三栏目（综合/金十早餐/热点头条）各 20 条；综合页含 6 条付费专享条目（如 231298，锁定"vip 跳过"分支）；头条页首条《特朗普周二会见AI巨头，OpenAI同日开发者大会料推常驻AI智能体》（id=231303，display_datetime=2026-09-29 17:32:06） |
| `jin10_article_detail.json` | `GET https://reference-api.jin10.com/reference/getOne?id=231303&type=news`（带 `x-app-id: arU9WZF7TC9m7nWn` 头，与列表是两套 app-id） | 2026-09-29 18:10 | 免费长文详情（content 为 HTML 片段，标签集 h2/img/p/strong，约 2800 字符） |
| `jin10_article_detail_rich.json` | `GET https://reference-api.jin10.com/reference/getOne?id=231299&type=news` | 2026-09-29 18:10 | 纯 `figure` 图片文（期货热图），锁定"figure 图片提取"路径 |
| `df_report_list_qtype0.json` / `df_report_list_qtype1.json` / `df_report_list_qtype2.json` | `GET https://reportapi.eastmoney.com/report/list?pageSize=100&beginTime=2026-09-28&endTime=2026-09-30&pageNo=1&qType=0/1/2`（无签名无专用头） | 2026-09-30 (东八区) | 东财研报三类列表各一页（个股 39 条/行业 100 条满页 TotalPage=2/宏观 74 条）；三类条目同构，差异只在填充：个股有 stock/目标价/预测，行业有 industryName，宏观大多为空 |
| `df_report_detail_stock.html` | `GET https://data.eastmoney.com/report/zw_stock.jshtml?infocode=AP202609301830020241`（页面 HTML 原样保存） | 2026-09-30 | 个股研报详情页（SSR），摘要在 `div.ctx-content` 的 p 段落（首段带全角空格缩进与个股标识行），标题《首次覆盖：电子大宗气体龙头厂商，受益半导体景气周期及国产替代》 |
| `df_report_detail_industry.html` | `GET https://data.eastmoney.com/report/zw_industry.jshtml?infocode=AP202609301830023938` | 2026-09-30 | 行业研报详情页（同模板结构，验证三类模板路径差异） |
| `df_report_detail_macro.html` | `GET https://data.eastmoney.com/report/zw_macresearch.jshtml?infocode=AP202609301830024638` | 2026-09-30 | 宏观研报详情页（同上） |

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
