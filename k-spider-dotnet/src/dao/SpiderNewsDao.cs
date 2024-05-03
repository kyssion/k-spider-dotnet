using k_spider_dotnet.exception;
using k_spider_dotnet.model;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace k_spider_dotnet.dao;

public class SpiderNewsBatchDao
{
    private static readonly ILogger Log = LogFactory.GetLogger<SpiderNewsDao>();

    public static int UpsetSpiderNewsContentOnConflict(SqlSugarClient connection,
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

            var item = connection.Insertable(contentInfo).IgnoreColumns("id", "create_time", "update_time");
            // todo 这里是一个坑 ， sqlsurge tostring 默认使用的200 行的 导出也就是说每200个数据就会有一个insert 不能重用需要重写一下 。 
            item.InsertBuilder.IsNoPage = true;
            item.InsertBuilder.IsReturnPkList = true;
            var insertSql = item.ToSqlString();
            insertSql = insertSql[..insertSql.LastIndexOf(';')];

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
            throw new DbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }

    public static int UpsetSpiderNewsContentOriginOnConflict(SqlSugarClient connection,
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
            // todo 这里是一个坑 ， sqlsurge tostring 默认使用的200 行的 导出也就是说每200个数据就会有一个insert 不能重用需要重写一下 。 
            item.InsertBuilder.IsNoPage = true;
            item.InsertBuilder.IsReturnPkList = true;
            var insertSql = item.ToSqlString();
            insertSql = insertSql[..insertSql.LastIndexOf(';')];
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
            throw new DbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }

    public static int UpsetSpiderNewsImageListOnConflict(SqlSugarClient connection,
        List<SpiderNewsImageListModel> spiderNewsImageList, int maxBatchNumber)
    {
        try
        {
            spiderNewsImageList =
                spiderNewsImageList.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            // 替换之前使用 WhereColumns 方法 , 这个方法本质上是会查询一下url , 对数据库压力会变大
            // connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand()
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
            // todo 这里是一个坑 ， sqlsurge tostring 默认使用的200 行的 导出也就是说每200个数据就会有一个insert 不能重用需要重写一下 。 
            item.InsertBuilder.IsNoPage = true;
            item.InsertBuilder.IsReturnPkList = true;
            var insertSql = item.ToSqlString();
            insertSql = insertSql[..insertSql.LastIndexOf(';')];
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
            throw new DbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }

    public static int UpsertSpiderNewsListOnConflict(SqlSugarClient connection, List<SpiderNewsListModel> newsList,
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
            // todo 这里是一个坑 ， sqlsurge tostring 默认使用的200 行的 导出也就是说每200个数据就会有一个insert 不能重用需要重写一下 。 
            item.InsertBuilder.IsNoPage = true;
            item.InsertBuilder.IsReturnPkList = true;
            var insertSql = item.ToSqlString();
            insertSql = insertSql[..insertSql.LastIndexOf(';')];
            var sqlTemple = $"""
                             {insertSql}
                             ON CONFLICT (news_url) DO NOTHING;
                             """;
            return connection.Ado.ExecuteCommand(sqlTemple);
        }
        catch (Exception e)
        {
            throw new DbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }
}

public class SpiderNewsDao
{
    private static readonly ILogger Log = LogFactory.GetLogger<SpiderNewsDao>();

    public static int UpsetSpiderNewsContentOrigin(SqlSugarClient connection, SpiderNewsContentOriginModel contentInfo)
    {
        try
        {
            var storageAble = connection.Storageable(contentInfo).WhereColumns(it => it.NewsUrl).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpsetSpiderNewsContent] err : {e}", e);
        }
    }


    public static int UpsetSpiderNewsContent(SqlSugarClient connection, SpiderNewsContentModel contentInfo)
    {
        try
        {
            var storageAble = connection.Storageable(contentInfo).WhereColumns(it => it.NewsUrl).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpsetSpiderNewsContent] err : {e}", e);
        }
    }

    public static int UpsetSpiderNewsImageList(SqlSugarClient connection, List<SpiderNewsImageListModel> spiderNewsImageList)
    {
        try
        {
            spiderNewsImageList =
                spiderNewsImageList.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            var storageAble = connection.Storageable(spiderNewsImageList).WhereColumns(it => it.ImageResourceUrl)
                .ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }


    public static int UpdateSpiderNewsListInfo(SqlSugarClient connection, List<SpiderNewsListModel> newsListItem)
    {
        try
        {
            return connection.Updateable(newsListItem).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    public static int UpdateSpiderNewListDownloadStatus(SqlSugarClient connection, SpiderNewsListModel newsListModel)
    {
        try
        {
            return connection.Updateable(newsListModel).UpdateColumns(it => it.DownloadStatusCode).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    public static int UpdateSpiderNewsListInfo(SqlSugarClient connection, SpiderNewsListModel newsListItem)
    {
        try
        {
            return connection.Updateable(newsListItem).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }
    
    public static int UpsetSpiderNewsListInfo(SqlSugarClient connection, List<SpiderNewsListModel> newsListItem)
    {
        try
        {
            newsListItem =
                newsListItem.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            var storageAble = connection.Storageable(newsListItem).WhereColumns(it => it.NewsUrl)
                .ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time", "download_status_code").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time" , "download_status_code").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new DbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }
}