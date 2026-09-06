using KSpider.Logger;
using KSpider.Exceptions;
using KSpider.Model;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace KSpider.Data;
public class SpiderNewsDao
{

    public int UpsetSpiderNewsContentOrigin(SqlSugarClient connection, SpiderNewsContentOriginModel contentInfo)
    {
        try
        {
            var storageAble = connection.Storageable(contentInfo).WhereColumns(it => it.NewsUrl).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContent] err : {e}", e);
        }
    }


    public int UpsetSpiderNewsContent(SqlSugarClient connection, SpiderNewsContentModel contentInfo)
    {
        try
        {
            var storageAble = connection.Storageable(contentInfo).WhereColumns(it => it.NewsUrl).ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsContent] err : {e}", e);
        }
    }

    public int UpsetSpiderNewsImageList(SqlSugarClient connection,
        List<SpiderNewsImageListModel> spiderNewsImageList)
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
            throw new KDbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }


    public int UpdateSpiderNewsListInfo(SqlSugarClient connection, List<SpiderNewsListModel> newsListItem)
    {
        try
        {
            return connection.Updateable(newsListItem).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    public int UpdateSpiderNewListDownloadStatus(SqlSugarClient connection, SpiderNewsListModel newsListModel)
    {
        try
        {
            return connection.Updateable(newsListModel)
                .UpdateColumns(it => new { it.DownloadStatusCode, it.FailCount })
                .ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    public int UpdateSpiderNewsListInfo(SqlSugarClient connection, SpiderNewsListModel newsListItem)
    {
        try
        {
            return connection.Updateable(newsListItem).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    public int UpsetSpiderNewsListInfo(SqlSugarClient connection, List<SpiderNewsListModel> newsListItem)
    {
        try
        {
            newsListItem =
                newsListItem.GroupBy(item => item.NewsUrl).Select(item => item.First()).ToList();

            var storageAble = connection.Storageable(newsListItem).WhereColumns(it => it.NewsUrl)
                .ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time", "download_status_code")
                       .ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time", "download_status_code")
                       .ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException("[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }
}
