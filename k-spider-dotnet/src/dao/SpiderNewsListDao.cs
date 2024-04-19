using k_spider_dotnet.dal.db;
using k_spider_dotnet.model;

namespace k_spider_dotnet.dao;

public class SpiderNewsListDao
{
    public void InsertSpiderNewsList(List<SpiderNewsListModel> newsList)
    {
        var connection = Pg.Connection();
        // 批量添加如果news相同更新
        connection.Storageable(newsList).WhereColumns(it => it.NewsUrl).ExecuteCommand();
    }
}