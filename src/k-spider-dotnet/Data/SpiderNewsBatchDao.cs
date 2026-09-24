using KSpider.Common.Logger;
using KSpider.Exceptions;
using KSpider.Model;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace KSpider.Data;
public class SpiderNewsBatchDao
{

    /// <summary>
    ///     去掉 ToSqlString 输出尾部的分号 , 以便手拼 ON CONFLICT 子句。
    ///     实测 ( SqlSugar 5.1.4.216 , IsNoPage = true ) : 多条 ( ≥2 ) 以 ";" 结尾 ,
    ///     单条以 VALUES 段结尾不带分号 ( 也无 returning ) ;
    ///     不要用 [..LastIndexOf(';')] 截断 —— 单条时 LastIndexOf 返回 -1 会抛参数越界 ( 有单测锁定 )。
    /// </summary>
    public static string TrimInsertSqlTail(string insertSql)
    {
        var sql = insertSql.TrimEnd();
        return sql.EndsWith(';') ? sql[..^1] : sql;
    }

    public int UpsetSpiderNewsContentOnConflict(SqlSugarClient connection,
        List<SpiderNewsContentModel> contentInfo, int maxBatchNumber)
    {
        try
        {
            contentInfo = contentInfo.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            // 替换之前使用 WhereColumns 方法 , 这个方法本质上是会查询一下url , 对数据库压力会变大
            // connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand()
            if (contentInfo.Count == 0) return 0;
            if (contentInfo.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < contentInfo.Count; i += maxBatchNumber)
                    allNumber += UpsetSpiderNewsContentOnConflict(connection,
                        contentInfo.GetRange(i, Math.Min(maxBatchNumber, contentInfo.Count - i)), maxBatchNumber);

                return allNumber;
            }

            // IsNoPage : 绕过 ToSqlString 默认 200 行自动分页 , 整批生成一条 INSERT 再手拼 ON CONFLICT
            var item = connection.Insertable(contentInfo).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = TrimInsertSqlTail(item.ToSqlString());

            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (news_url) DO UPDATE SET news_url          = EXCLUDED.news_url,
                                                                  news_title        = EXCLUDED.news_title,
                                                                  news_summary      = EXCLUDED.news_summary,
                                                                  news_from         = EXCLUDED.news_from,
                                                                  news_time         = EXCLUDED.news_time,
                                                                  news_keyword      = EXCLUDED.news_keyword,
                                                                  news_content_json = EXCLUDED.news_content_json,
                                                                  news_content_text = EXCLUDED.news_content_text
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsetSpiderNewsContentOnConflict] err : {e}", e);
        }
    }

    public int UpsetSpiderNewsContentOriginOnConflict(SqlSugarClient connection,
        List<SpiderNewsContentOriginModel> contentInfo, int maxBatchNumber)
    {
        try
        {
            contentInfo = contentInfo.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            // 替换之前使用 WhereColumns 方法 , 这个方法本质上是会查询一下url , 对数据库压力会变大
            // connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand()
            if (contentInfo.Count == 0) return 0;
            if (contentInfo.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < contentInfo.Count; i += maxBatchNumber)
                    allNumber += UpsetSpiderNewsContentOriginOnConflict(connection,
                        contentInfo.GetRange(i, Math.Min(maxBatchNumber, contentInfo.Count - i)), maxBatchNumber);

                return allNumber;
            }

            var item = connection.Insertable(contentInfo).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (news_url) DO UPDATE SET news_url              = EXCLUDED.news_url,
                                                                  news_origin_content   = EXCLUDED.news_origin_content,
                                                                  news_origin_type      = EXCLUDED.news_origin_type,
                                                                  status                = EXCLUDED.status,
                                                                  message               = EXCLUDED.message
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsetSpiderNewsContentOriginOnConflict] err : {e}", e);
        }
    }

    public int UpsetSpiderNewsImageListOnConflict(SqlSugarClient connection,
        List<SpiderNewsImageListModel> spiderNewsImageList, int maxBatchNumber)
    {
        try
        {
            // 按 ( news_url , image_resource_url ) 去重 : 一篇文章有多张图 , 只按 news_url 去重会把图丢到只剩一张
            spiderNewsImageList =
                spiderNewsImageList.GroupBy(item => new { item.NewsUrl, item.ImageResourceUrl })
                    .Select(item => item.First()).ToList();

            // 替换之前使用 WhereColumns 方法 , 这个方法本质上是会查询一下url , 对数据库压力会变大
            // connection.Storageable(itemList).WhereColumns(it => it.ImageResourceUrl).ExecuteCommand()
            if (spiderNewsImageList.Count == 0) return 0;
            if (spiderNewsImageList.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < spiderNewsImageList.Count; i += maxBatchNumber)
                    allNumber += UpsetSpiderNewsImageListOnConflict(connection,
                        spiderNewsImageList.GetRange(i, Math.Min(maxBatchNumber, spiderNewsImageList.Count - i)),
                        maxBatchNumber);

                return allNumber;
            }

            var item = connection.Insertable(spiderNewsImageList).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (image_resource_url) DO UPDATE SET news_url    = EXCLUDED.news_url,
                                                                  image_resource_url    = EXCLUDED.image_resource_url,
                                                                  image_name            = EXCLUDED.image_name

                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsetSpiderNewsImageListOnConflict] err : {e}", e);
        }
    }

    public int UpsertSpiderNewsListOnConflict(SqlSugarClient connection, List<SpiderNewsListModel> newsList,
        int maxBatchNumber)
    {
        try
        {
            newsList = newsList.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            // 替换之前使用 WhereColumns 方法 , 这个方法本质上是会查询一下url , 对数据库压力会变大
            // connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand()
            if (newsList.Count == 0) return 0;
            if (newsList.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < newsList.Count; i += maxBatchNumber)
                    allNumber += UpsertSpiderNewsListOnConflict(connection,
                        newsList.GetRange(i, Math.Min(maxBatchNumber, newsList.Count - i)), maxBatchNumber);

                return allNumber;
            }

            var item = connection.Insertable(newsList).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (news_url) DO NOTHING;
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertSpiderNewsListOnConflict] err : {e}", e);
        }
    }

    /// <summary>
    ///     实时快讯批量写入 : 新行插入 , 已存在行更新内容字段 ( 快讯常在发布后数分钟内修正/补充 ,
    ///     首页每轮重拉 , DO UPDATE 让修正随下一轮 15 秒 poll 自然回填 )。
    ///     唯一键是 (from_media, news_url)。
    ///     WHERE raw_content IS DISTINCT FROM : 原始 JSON 未变就不更新 —— 各落库字段都派生自原始 JSON ,
    ///     避免每轮重拉首页对既有行的空转 UPDATE 刷 update_time ( 触发器只应在内容真变时刷新 ,
    ///     否则 update_time 失去"最后修改时间"语义 , 同步侧也会反复搬运未变的行 ) ;
    ///     附带效果是返回的影响行数只统计真实插入与更新。
    /// </summary>
    public int UpsertFlashNewsOnConflict(SqlSugarClient connection, List<SpiderFlashNewsModel> flashNews,
        int maxBatchNumber)
    {
        try
        {
            flashNews = flashNews
                .GroupBy(item => new { item.FromMedia, item.NewsUrl })
                .Select(item => item.First()).ToList();
            if (flashNews.Count == 0) return 0;
            if (flashNews.Count > maxBatchNumber)
            {
                var allNumber = 0;
                for (var i = 0; i < flashNews.Count; i += maxBatchNumber)
                    allNumber += UpsertFlashNewsOnConflict(connection,
                        flashNews.GetRange(i, Math.Min(maxBatchNumber, flashNews.Count - i)), maxBatchNumber);
                return allNumber;
            }

            var item = connection.Insertable(flashNews).IgnoreColumns("id", "create_time", "update_time");
            item.InsertBuilder.IsNoPage = true;
            var insertSql = TrimInsertSqlTail(item.ToSqlString());
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (from_media, news_url) DO UPDATE SET title       = EXCLUDED.title,
                                                                          content     = EXCLUDED.content,
                                                                          keyword     = EXCLUDED.keyword,
                                                                          level       = EXCLUDED.level,
                                                                          stock_list  = EXCLUDED.stock_list,
                                                                          image_urls  = EXCLUDED.image_urls,
                                                                          raw_content = EXCLUDED.raw_content
                             WHERE spider_flash_news.raw_content IS DISTINCT FROM EXCLUDED.raw_content
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsertFlashNewsOnConflict] err : {e}", e);
        }
    }
}
