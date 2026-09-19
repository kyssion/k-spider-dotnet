# TestData — 真实抓取响应夹具

解析回归测试用的**真实接口响应**（原样保存，未做字段裁剪）。测试通过 `AppContext.BaseDirectory/TestData` 读取，
csproj 已配置 `CopyToOutputDirectory=PreserveNewest`，因此测试仍然完全离线。

| 文件 | 来源接口 | 抓取时间 | 内容 |
|---|---|---|---|
| `cls_roll_page1.json` | `GET https://www.cls.cn/v1/roll/get_roll_list?app=CailianpressWeb&last_time=0&os=web&refresh_type=1&rn=20&sv=8.7.9&sign=…` | 2026-09-19 01:51 (东八区) | 财联社电报最新 20 条 |
| `cls_roll_page2.json` | 同上，`last_time=1789746672`（page1 最老一条 ctime + 1） | 2026-09-19 01:51 | 财联社电报第二页 20 条，用于游标连续性用例 |
| `cls_roll_with_image.json` | 同上，`rn=50` 后摘出带图的那一条（id=2487578） | 2026-09-19 01:52 | 财联社电报带图条目（正文图 1 张 + 封面同图） |
| `df_list_344.json` | `GET https://np-listapi.eastmoney.com/comm/web/getNewsByColumns?…&column=344&page_index=1&page_size=20&…` | 2026-09-19 01:52 | 东方财富「财经导读」栏目列表 20 条 |
| `df_article_real.json` | `GET https://newsinfo.eastmoney.com/kuaixun/v2/api/article/202609183878840472?guid=…` | 2026-09-19 01:52 | 东方财富正文接口（含图片段落，正文 9827 字符） |
| `sina_live_page1.json` / `sina_live_page2.json` | `GET https://zhibo.sina.com.cn/api/zhibo/feed?page=1/2&page_size=20&zhibo_id=152&tag_id=0&dire=f&dpc=1` | 2026-09-19 11:06 (东八区) | 新浪 7x24 快讯两页各 20 条 |
| `wscn_live_page1.json` / `wscn_live_page2.json` | `GET https://api-one.wallstcn.com/apiv1/content/lives?channel=global-channel&client=pc&limit=20`（第二页带 `cursor=1789785930`） | 2026-09-19 11:06 | 华尔街见闻 live 两页各 20 条 |
| `jin10_flash_page1.json` / `jin10_flash_page2.json` | `GET https://flash-api.jin10.com/get_flash_list?channel=-8200&vip=1`（带 `x-app-id` / `x-version` 头，第二页带 `max_time`） | 2026-09-19 11:06 | 金十快讯两页各 20 余条，含 4 条 PLUS 专享（正文锁定）条目 |

维护方式：接口改版或解析逻辑变更时重新抓一份覆盖同名文件，并同步用例里依赖夹具的固定值
（财联社游标 `1789746672`、正文时间 `2026/09/19 01:09:08` 等）。
