using KSpider.Common.Logger;
using KSpider.Exceptions;
using KSpider.Model;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace KSpider.Data;

/// <summary>
///     网页型管线单条 / 小批量 DAO : 连接由 Job 层创建传入 , 事务由 Job 层管理 ;
///     异常统一包装为 KDbException ( 调用方捕获后不消耗 fail_count ) 。
///     upsert 一律 IgnoreColumns( id , create_time , update_time ) : update_time 由触发器自动刷新 ;
///     整批手拼 ON CONFLICT 的批量版在 SpiderNewsBatchDao
/// </summary>
public class SpiderNewsDao
{

    /// <summary>
    ///     单条 upsert 原始内容 ( 按 news_url 判重 , 存在则整体更新 )
    /// </summary>
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
            throw new KDbException($"[UpsetSpiderNewsContentOrigin] err : {e}", e);
        }
    }


    /// <summary>
    ///     单条 upsert 解析后详情 ( 按 news_url 判重 )
    /// </summary>
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
            throw new KDbException($"[UpsetSpiderNewsContent] err : {e}", e);
        }
    }

    /// <summary>
    ///     批量 upsert 图片 ( 按 image_resource_url 判重 )
    /// </summary>
    public int UpsetSpiderNewsImageList(SqlSugarClient connection,
        List<SpiderNewsImageListModel> spiderNewsImageList)
    {
        try
        {
            // 按 ( news_url , image_resource_url ) 去重 : 一篇文章有多张图 , 只按 news_url 去重会把图丢到只剩一张
            spiderNewsImageList =
                spiderNewsImageList.GroupBy(item => new { item.NewsUrl, item.ImageResourceUrl })
                    .Select(item => item.First()).ToList();

            var storageAble = connection.Storageable(spiderNewsImageList).WhereColumns(it => it.ImageResourceUrl)
                .ToStorage();
            return storageAble.AsInsertable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand() +
                   storageAble.AsUpdateable.IgnoreColumns("id", "create_time", "update_time").ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpsetSpiderNewsImageList] err : {e}", e);
        }
    }


    /// <summary>
    ///     批量整行更新列表行
    /// </summary>
    public int UpdateSpiderNewsListInfo(SqlSugarClient connection, List<SpiderNewsListModel> newsListItem)
    {
        try
        {
            return connection.Updateable(newsListItem).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    /// <summary>
    ///     只回写状态机两列 ( download_status_code / fail_count ) , 不覆盖列表行其它字段
    /// </summary>
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
            throw new KDbException($"[UpdateSpiderNewListDownloadStatus] err : {e}", e);
        }
    }

    /// <summary>
    ///     单行整行更新列表行
    /// </summary>
    public int UpdateSpiderNewsListInfo(SqlSugarClient connection, SpiderNewsListModel newsListItem)
    {
        try
        {
            return connection.Updateable(newsListItem).ExecuteCommand();
        }
        catch (Exception e)
        {
            throw new KDbException($"[UpdateSpiderNewsListInfo] err : {e}", e);
        }
    }

    /// <summary>
    ///     批量 upsert 列表行 ; IgnoreColumns 带 download_status_code :
    ///     新插入行走库默认 status=0 进流水线 , 已存在行不覆盖状态 ( 状态只由后续任务推进 )
    /// </summary>
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
            throw new KDbException($"[UpsetSpiderNewsListInfo] err : {e}", e);
        }
    }
}
