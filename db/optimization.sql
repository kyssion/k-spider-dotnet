-- =====================================================================================
-- k-spider 数据库优化脚本（已按安全性复审，幂等，可整份重复执行）
--
-- 适用 : PostgreSQL 15+ , 库 k_script_spider
-- 执行 : psql -h <host> -U postgres -d k_script_spider -f db/optimization.sql
--        ※ 必须用 psql 执行；不要加 -1 (单事务) 参数 —— 脚本包含 CONCURRENTLY 建索引与存储过程内分批提交
-- 备份 : 第 4 节删除数据不可逆 , 执行前建议备份 :
--        pg_dump -t spider_news_list -t spider_news_content -t spider_news_content_origin \
--                -t spider_news_image_list k_script_spider > backup_$(date +%F).sql
-- 失败 : 任何一步因锁超时(3秒)失败 , 直接整份重跑即可 , 已完成的部分会自动跳过
-- 参数 : 保留期硬编码在第 4 节 ( 新闻 30 天 / 股票 730 天 ) , 需要调整就改对应数字
-- 依据 : 逐条说明见 db/optimization.md
-- =====================================================================================

\set ON_ERROR_STOP on
SET lock_timeout = '3s';

\echo '==== 1/5 必要结构变更 ( fail_count 列 + 轮询索引 ) ===='

-- 1.1 失败重试计数列 ( 新版代码依赖 , 主程序启动时也会自动补 )
ALTER TABLE public.spider_news_list
    ADD COLUMN IF NOT EXISTS fail_count integer DEFAULT 0 NOT NULL;

-- 1.2 轮询部分索引升级 : INCLUDE(news_time) 让健康检查的 MIN(news_time) 走纯索引
--     CONCURRENTLY 不阻塞写入 ( origin 任务每 3 秒都在 UPDATE 这张表 )
DROP INDEX IF EXISTS public.idx_news_list_download_status;
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_news_list_download_status
    ON public.spider_news_list (download_status_code, id) INCLUDE (news_time)
    WHERE download_status_code IN (0, 2, 3, 4);

\echo '==== 2/5 结构优化 ( 冗余约束 / 命名 / 列宽 ) ===='

-- 2.1 删除股票表冗余唯一约束 : (date,stock_id) 与 (stock_id,date) 唯一性等价 ,
--     代码全部走 (stock_id,date) 方向索引 , 删约束后写入开销减半
ALTER TABLE public.stock_cn_level1_archived_daily_origin DROP CONSTRAINT IF EXISTS uk_cn_daily_stock;
ALTER TABLE public.stock_hk_level1_archived_daily_origin DROP CONSTRAINT IF EXISTS uk_hk_daily_stock;

-- 2.2 约束改名 : 去掉调试期 "test" 残留 ( DO 块保证重复执行不报错 )
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_constraint
               WHERE conname = 'uk_news_content_test_url'
                 AND conrelid = 'public.spider_news_content'::regclass) THEN
        ALTER TABLE public.spider_news_content
            RENAME CONSTRAINT uk_news_content_test_url TO uk_news_content_url;
    END IF;
END $$;

-- 2.3 news_from 扩容到 100 , 与 spider_news_content 对齐 ( varchar 加宽是纯元数据操作 )
ALTER TABLE public.spider_news_list ALTER COLUMN news_from TYPE varchar(100);

\echo '==== 3/5 数据质量修复 ( 空 URL 清理 + 摘要回填 ) ===='

-- 3.1 清理空 URL 脏行 ( 顺序 : 子表在前 ; 预期各表为 0 或极少 )
DELETE FROM public.spider_news_content_origin  WHERE news_url IS NULL OR btrim(news_url) = '';
DELETE FROM public.spider_news_content         WHERE news_url IS NULL OR btrim(news_url) = '';
DELETE FROM public.spider_news_image_list      WHERE news_url IS NULL OR btrim(news_url) = '';
DELETE FROM public.spider_news_list            WHERE news_url IS NULL OR btrim(news_url) = '';

-- 3.2 存量 "摘要=标题" 占位数据回填 ( 存储过程内每 5000 行提交一次 , 避免大事务 ;
--     IS DISTINCT FROM 条件保证回填过的行不再命中 , 必然收敛且可重复执行 )
CREATE OR REPLACE PROCEDURE pg_temp.backfill_news_summary()
LANGUAGE plpgsql AS $proc$
DECLARE
    n bigint;
BEGIN
    LOOP
        WITH batch AS (
            SELECT c.id
            FROM public.spider_news_content c
            JOIN public.spider_news_list l ON l.news_url = c.news_url
            WHERE l.news_summary IS NOT NULL AND btrim(l.news_summary) <> ''
              AND c.news_summary = c.news_title
              AND c.news_summary IS DISTINCT FROM l.news_summary
            ORDER BY c.id
            LIMIT 5000
        )
        UPDATE public.spider_news_content c
        SET news_summary = l.news_summary
        FROM public.spider_news_list l
        WHERE c.id IN (SELECT id FROM batch)
          AND l.news_url = c.news_url
          AND c.news_summary IS DISTINCT FROM l.news_summary;
        GET DIAGNOSTICS n = ROW_COUNT;
        COMMIT;
        EXIT WHEN n = 0;
        RAISE NOTICE '[摘要回填] 本批 % 行', n;
    END LOOP;
END $proc$;

CALL pg_temp.backfill_news_summary();
DROP PROCEDURE pg_temp.backfill_news_summary();

\echo '==== 4/5 存储治理 ( origin/图片清理 + 股票保留期 + VACUUM ) ※ 不可逆 ===='
-- 保留期参数 : 下面 30 = 新闻类保留 30 天 , 730 = 股票日线保留 730 天

-- 4.1a 已成功解析(status=1)且超期的原始 JSON ( 每条 3-5KB , 解析完成后不再被读取 )
CREATE OR REPLACE PROCEDURE pg_temp.clean_expired_origin()
LANGUAGE plpgsql AS $proc$
DECLARE
    n bigint;
BEGIN
    LOOP
        WITH batch AS (
            SELECT o.ctid
            FROM public.spider_news_content_origin o
            JOIN public.spider_news_list l ON l.news_url = o.news_url
            WHERE l.download_status_code = 1
              AND l.news_time < localtimestamp - interval '30 days'
            LIMIT 5000
        )
        DELETE FROM public.spider_news_content_origin
        WHERE ctid IN (SELECT ctid FROM batch);
        GET DIAGNOSTICS n = ROW_COUNT;
        COMMIT;
        EXIT WHEN n = 0;
        RAISE NOTICE '[origin 清理] 本批 % 行', n;
    END LOOP;
END $proc$;

CALL pg_temp.clean_expired_origin();
DROP PROCEDURE pg_temp.clean_expired_origin();

-- 4.1b "文章不存在"固定失败标记
DELETE FROM public.spider_news_content_origin
WHERE news_origin_content = '{"data":null,"errorcode":0,"id":"-1","message":"未获取到文章信息..","success":0}';

-- 4.1c 孤儿 origin ( 列表中已无对应行 )
DELETE FROM public.spider_news_content_origin o
WHERE NOT EXISTS (SELECT 1 FROM public.spider_news_list l WHERE l.news_url = o.news_url);

-- 4.2a 图片记录随新闻超期清理 ( 同样分批提交 )
CREATE OR REPLACE PROCEDURE pg_temp.clean_expired_images()
LANGUAGE plpgsql AS $proc$
DECLARE
    n bigint;
BEGIN
    LOOP
        WITH batch AS (
            SELECT i.ctid
            FROM public.spider_news_image_list i
            JOIN public.spider_news_list l ON l.news_url = i.news_url
            WHERE l.news_time < localtimestamp - interval '30 days'
            LIMIT 5000
        )
        DELETE FROM public.spider_news_image_list
        WHERE ctid IN (SELECT ctid FROM batch);
        GET DIAGNOSTICS n = ROW_COUNT;
        COMMIT;
        EXIT WHEN n = 0;
        RAISE NOTICE '[图片清理] 本批 % 行', n;
    END LOOP;
END $proc$;

CALL pg_temp.clean_expired_images();
DROP PROCEDURE pg_temp.clean_expired_images();

-- 4.2b 股票日线保留 730 天 ( 行量小 , 单语句即可 )
DELETE FROM public.stock_cn_level1_archived_daily_origin WHERE date < current_date - interval '730 days';
DELETE FROM public.stock_hk_level1_archived_daily_origin WHERE date < current_date - interval '730 days';

-- 4.3 空间回收 ( 不阻塞读写 )
VACUUM (ANALYZE) public.spider_news_content_origin;
VACUUM (ANALYZE) public.spider_news_image_list;
VACUUM (ANALYZE) public.spider_news_list;
VACUUM (ANALYZE) public.spider_news_content;

\echo '==== 5/5 执行后验证 ===='

-- 索引就位 ( 应看到 idx_news_list_download_status / stock_xx_id_date )
SELECT tablename, indexname FROM pg_indexes
WHERE tablename IN ('spider_news_list', 'stock_cn_level1_archived_daily_origin',
                    'stock_hk_level1_archived_daily_origin')
ORDER BY tablename, indexname;

-- 约束状态 ( 不应再有 uk_cn_daily_stock / uk_hk_daily_stock / uk_news_content_test_url )
SELECT conrelid::regclass AS table_name, conname
FROM pg_constraint
WHERE conrelid::regclass::text IN ('spider_news_list', 'spider_news_content',
                                   'stock_cn_level1_archived_daily_origin',
                                   'stock_hk_level1_archived_daily_origin')
ORDER BY 1, 2;

-- 流水线积压概况 ( 应与 DfCheckJob 日志一致 )
SELECT download_status_code, count(*) AS cnt
FROM public.spider_news_list
WHERE from_media = 1
GROUP BY 1 ORDER BY 1;

\echo '==== 全部完成 ===='
