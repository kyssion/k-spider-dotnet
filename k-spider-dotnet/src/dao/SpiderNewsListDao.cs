using k_spider_dotnet.model;
using SqlSugar;

namespace k_spider_dotnet.dao;

public class SpiderNewsListDao
{

    public static int UpsetSpiderNewsContent(SqlSugarClient connection, SpiderNewsContentTestModel contentInfo)
    {
        return connection.Storageable(contentInfo).WhereColumns(it => it.NewsUrl).ExecuteCommand();
    }
    
    // 使用 news_url 过滤  , 更新或者插入新的数据
    public static int UpsertSpiderNewsList(SqlSugarClient connection, List<SpiderNewsListModel> newsList,
        int maxBatchNumber)
    {
        // 替换之前使用 WhereColumns 方法 , 这个方法本质上是会查询一下url , 对数据库压力会变大
        // connection.Storageable(itemList).WhereColumns(it => it.NewsUrl).ExecuteCommand()
        if (newsList.Count == 0) return 0;
        if (newsList.Count > maxBatchNumber)
        {
            var allNumber = 0;
            for (var i = 0; i < newsList.Count; i += maxBatchNumber)
                allNumber += UpsertSpiderNewsList(connection,
                    newsList.GetRange(i, Math.Min(maxBatchNumber, newsList.Count - i)), maxBatchNumber);

            return allNumber;
        }

        var item = connection.Insertable(newsList);
        // todo 这里是一个坑 ， sqlsurge tostring 默认使用的200 行的 导出也就是说每200个数据就会有一个insert 不能重用需要重写一下 。 
        item.InsertBuilder.IsNoPage = true;
        item.InsertBuilder.IsReturnPkList = true;
        var insertSql = item.ToSqlString();
        insertSql = insertSql[..insertSql.LastIndexOf(';')];
        var sqlTemple = $"""
                         {insertSql}
                         ON CONFLICT (news_url) DO UPDATE SET from_media         = EXCLUDED.from_media,
                                                              news_url           = EXCLUDED.news_url,
                                                              news_title         = EXCLUDED.news_title,
                                                              news_summary       = EXCLUDED.news_summary,
                                                              news_from          = EXCLUDED.news_from,
                                                              news_time          = EXCLUDED.news_time,
                                                              news_download_time = EXCLUDED.news_download_time,
                                                              category           = EXCLUDED.category
                         """;
        return connection.Ado.ExecuteCommand(sqlTemple);
    }
}