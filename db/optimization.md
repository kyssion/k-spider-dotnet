# 数据库优化操作手册

> **可直接执行的版本见 [optimization.sql](optimization.sql)**（本手册的说明性文档 , 两边条目一一对应）。
> 适用范围 : PostgreSQL 15+ , 库 `k_script_spider`
> 来源 : 2026-09 数据层审核（已逐条复查代码依赖与锁行为，仅收录确认安全的语句）
> 原则 : 全部语句幂等（可重复执行）；标注"不可逆"的条目必须先备份

## 0. 执行前护栏（每次操作会话先跑）

```sql
-- DDL 排队等锁时 3 秒快速失败 , 避免锁队列阻塞线上任务
SET lock_timeout = '3s';
```

```bash
# 涉及第 4 节(删除数据, 不可逆)时 , 先备份目标表
pg_dump -t spider_news_list -t spider_news_content -t spider_news_content_origin \
        -t spider_news_image_list k_script_spider > backup_before_cleanup_$(date +%F).sql
```

执行方式 : `psql -h <host> -U postgres -d k_script_spider -f <本文件拆出的对应段落>` , 或逐段粘贴执行。
窗口建议 : 避开工作日 20:00 股票任务 ; 大批量操作(第 3/4 节)选新闻低峰(凌晨)。

---

## 1. 必要结构变更（唯一必须项）

新版本代码依赖 `fail_count` 列与轮询部分索引。**主程序启动时会自动幂等执行本节**（`Pg.EnsureSpiderNewsListDbObjects()`），
因此只有一种情况需要手工执行 : **`k-spider-sync` 写入的本地库从未被新版主程序连接过**（sync 的 INSERT 语句包含 fail_count 列 , 缺列会报错）。

```sql
-- 1.1 失败重试计数列
ALTER TABLE public.spider_news_list
    ADD COLUMN IF NOT EXISTS fail_count integer DEFAULT 0 NOT NULL;

-- 1.2 流水线轮询部分索引
CREATE INDEX IF NOT EXISTS idx_news_list_download_status
    ON public.spider_news_list (download_status_code, id)
    WHERE download_status_code IN (0, 2, 3, 4);
```

兼容性 : 纯增量 , 不删不改任何既有列 , 存量数据零迁移。

---

## 2. 结构优化（可选 , 已确认无代码依赖）

### 2.1 删除股票表冗余唯一约束

A股/港股日线表各有一对方向相反的等价唯一索引（`UNIQUE(date,stock_id)` 约束 + `UNIQUE(stock_id,date)` 索引）,
唯一性语义重复 , 每次 upsert 双倍索引维护。已核实全部代码走 `Storageable.WhereColumns(stock_id, date)`,
无任何 `ON CONFLICT (date, stock_id)` 用法 , 删约束安全。

```sql
ALTER TABLE public.stock_cn_level1_archived_daily_origin DROP CONSTRAINT IF EXISTS uk_cn_daily_stock;
ALTER TABLE public.stock_hk_level1_archived_daily_origin DROP CONSTRAINT IF EXISTS uk_hk_daily_stock;
```

回滚 : `ALTER TABLE ... ADD CONSTRAINT uk_cn_daily_stock UNIQUE (date, stock_id);`（大表回滚会锁表重建索引 , 低峰执行）

### 2.2 升级轮询索引（服务 DfCheckJob 的 MIN(news_time)）

⚠️ 必须用 `CONCURRENTLY`（普通 CREATE INDEX 建索引期间阻塞写入 , 会卡住 3 秒轮询的 origin 任务）。
不能在事务块内执行 ; 若中途失败会留下 INVALID 索引 , 删掉重跑即可。

```sql
DROP INDEX IF EXISTS public.idx_news_list_download_status;
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_news_list_download_status
    ON public.spider_news_list (download_status_code, id) INCLUDE (news_time)
    WHERE download_status_code IN (0, 2, 3, 4);
```

### 2.3 约束改名（去掉调试期 "test" 残留）

```sql
ALTER TABLE public.spider_news_content
    RENAME CONSTRAINT uk_news_content_test_url TO uk_news_content_url;
```

### 2.4 news_from 扩容（varchar 扩宽是纯元数据操作 , 瞬时完成）

```sql
ALTER TABLE public.spider_news_list ALTER COLUMN news_from TYPE varchar(100);
```

注意 : 扩宽容易缩窄难（若已有超 30 字符数据 , 缩回 30 会失败）, 实际为单向变更。

---

## 3. 数据质量修复（可选）

### 3.1 清理空 URL 脏行（先查后删 , 顺序 : 子表在前）

```sql
-- 预检 : 预期各表为 0 或极少
SELECT 'list' AS t, count(*) FROM public.spider_news_list           WHERE news_url IS NULL OR btrim(news_url) = ''
UNION ALL SELECT 'content', count(*) FROM public.spider_news_content      WHERE news_url IS NULL OR btrim(news_url) = ''
UNION ALL SELECT 'origin',  count(*) FROM public.spider_news_content_origin WHERE news_url IS NULL OR btrim(news_url) = ''
UNION ALL SELECT 'image',   count(*) FROM public.spider_news_image_list    WHERE news_url IS NULL OR btrim(news_url) = '';

-- 清理
DELETE FROM public.spider_news_content_origin  WHERE news_url IS NULL OR btrim(news_url) = '';
DELETE FROM public.spider_news_content         WHERE news_url IS NULL OR btrim(news_url) = '';
DELETE FROM public.spider_news_image_list      WHERE news_url IS NULL OR btrim(news_url) = '';
DELETE FROM public.spider_news_list            WHERE news_url IS NULL OR btrim(news_url) = '';
```

### 3.2 存量"摘要=标题"占位数据回填（分批 , 反复执行到影响行数为 0）

带 `IS DISTINCT FROM` 条件保证幂等且必然收敛（回填过的行不再命中）。

```sql
WITH batch AS (
    SELECT c.id
    FROM public.spider_news_content c
    JOIN public.spider_news_list l ON l.news_url = c.news_url
    WHERE l.news_summary IS NOT NULL AND btrim(l.news_summary) <> ''
      AND c.news_summary = c.news_title
      AND c.news_summary IS DISTINCT FROM l.news_summary
    ORDER BY c.id
    LIMIT 10000
)
UPDATE public.spider_news_content c
SET news_summary = l.news_summary
FROM public.spider_news_list l
WHERE c.id IN (SELECT id FROM batch)
  AND l.news_url = c.news_url
  AND c.news_summary IS DISTINCT FROM l.news_summary;
```

---

## 4. 存储治理（可选 , 不可逆 , 先做第 0 节备份）

### 4.1 origin 表清理（每条新闻整篇原始 JSON 约 3-5KB , 已成功解析后不再被读取）

分批执行 , 反复到影响行数为 0。保留 30 天窗口足够覆盖重试与重放需求。

```sql
-- 4.1a 已成功解析(status=1)且超期的原始 JSON
WITH batch AS (
    SELECT o.ctid
    FROM public.spider_news_content_origin o
    JOIN public.spider_news_list l ON l.news_url = o.news_url
    WHERE l.download_status_code = 1
      AND l.news_time < localtimestamp - interval '30 days'
    LIMIT 10000
)
DELETE FROM public.spider_news_content_origin WHERE ctid IN (SELECT ctid FROM batch);

-- 4.1b "文章不存在"固定失败标记
DELETE FROM public.spider_news_content_origin
WHERE news_origin_content = '{"data":null,"errorcode":0,"id":"-1","message":"未获取到文章信息..","success":0}';

-- 4.1c 孤儿 origin（列表中已无对应行）
DELETE FROM public.spider_news_content_origin o
WHERE NOT EXISTS (SELECT 1 FROM public.spider_news_list l WHERE l.news_url = o.news_url);
```

### 4.2 图片记录 / 股票日线保留策略

```sql
-- 图片随新闻超期清理（分批同 4.1a 模式）
WITH batch AS (
    SELECT i.ctid
    FROM public.spider_news_image_list i
    JOIN public.spider_news_list l ON l.news_url = i.news_url
    WHERE l.news_time < localtimestamp - interval '30 days'
    LIMIT 10000
)
DELETE FROM public.spider_news_image_list WHERE ctid IN (SELECT ctid FROM batch);

-- 股票日线保留 2 年（行数较多时同样套用分批模板）
DELETE FROM public.stock_cn_level1_archived_daily_origin WHERE date < current_date - interval '730 days';
DELETE FROM public.stock_hk_level1_archived_daily_origin WHERE date < current_date - interval '730 days';
```

### 4.3 清理后空间回收（不阻塞读写 , 可中断）

```sql
VACUUM (ANALYZE) public.spider_news_content_origin;
VACUUM (ANALYZE) public.spider_news_image_list;
VACUUM (ANALYZE) public.spider_news_list;
VACUUM (ANALYZE) public.spider_news_content;
```

---

## 5. 跨库提醒

第 3/4 节在**远端库**执行后 , 不会经 `k-spider-sync` 传播到本地库（sync 只按 Id 增量搬新增行 , 不传播 UPDATE/DELETE）。
两张库需**各自执行一遍** , 先后顺序不影响结果。

## 6. 已知未收录项（暂缓 , 原因如下）

| 条目 | 暂缓原因 |
|---|---|
| `spider_news_list` 加 `CHECK (news_url <> '')` 约束 | 代码侧 `DfListSpider` 会产出空串 URL（缺 url 时静默降级）, 直接加约束会让整批 INSERT 抛异常、栏目持续失败。需先在 `DfNewsListJob` 过滤空 URL 并部署后 , 方可执行 |
| 美股表改名 `daliy → daily` | 需与代码 `[SugarTable]` 同一次部署 , 单边执行会报错 ; 表当前为空不急 |

## 7. 执行后验证

```sql
-- 索引就位
SELECT indexname FROM pg_indexes WHERE tablename IN ('spider_news_list','stock_cn_level1_archived_daily_origin');
-- 约束就位 / 冗余已除
SELECT conname, conrelid::regclass FROM pg_constraint
WHERE conrelid::regclass::text IN ('spider_news_list','spider_news_content','stock_cn_level1_archived_daily_origin');
-- 待处理积压概况（应与 DfCheckJob 日志一致）
SELECT download_status_code, count(*) FROM spider_news_list WHERE from_media = 1 GROUP BY 1 ORDER BY 1;
```
