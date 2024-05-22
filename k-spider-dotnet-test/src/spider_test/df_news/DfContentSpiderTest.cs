using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.exception;
using k_spider_dotnet.model;
using k_spider_dotnet.script;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.log;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace k_spider_dotnet_test.spider_test.df_news;

[TestClass]
public class DfContentSpiderTest
{
    private readonly ILogger Logger = LogFactory.GetLogger<DfContentSpiderTest>();

    [TestMethod]
    public void TestOnePageDownload()
    {
        using var connection = Pg.Connection();
        var spiderContent = new DfContentSpider();
        var newsItem = new SpiderNewsListModel
        {
            NewsUrl = "http://stock.eastmoney.com/news/1436,202202112273036063.html"
        };
        try
        {
            var spiderOriginInfo = connection.Queryable<SpiderNewsContentOriginModel>()
                .Where(it => it.NewsUrl == newsItem.NewsUrl).First();

            var dfContentInfo = spiderContent.GetContentInfoByJson(spiderOriginInfo.NewsOriginContent ?? "",
                spiderOriginInfo.NewsUrl ?? "");
            var imgDbList = dfContentInfo.ImgInfos.Select(item => new SpiderNewsImageListModel
                    { NewsUrl = item.NewsUrl, ImageResourceUrl = item.ResourceUrl, ImageName = item.ImgName })
                .ToList();
            newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.SuccessSyncDetailInfo;
            connection.Ado.BeginTran();
            SpiderNewsDao.UpsetSpiderNewsContent(connection, dfContentInfo.ToSpiderNewsContentModel());
            SpiderNewsDao.UpsetSpiderNewsImageList(connection, imgDbList);
            SpiderNewsDao.UpdateSpiderNewsListInfo(connection, newsItem);
            connection.Ado.CommitTran();
        }
        catch (Exception e)
        {
            var needUpdateDb = false;
            switch (e)
            {
                case DownloadHttpRequestException:
                    Logger.LogError("[DfNewsContentJob] download  err  : {} , url : {} ", e, newsItem.NewsUrl);
                    newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                    needUpdateDb = true;
                    break;
                case HtmlFormException:
                    Logger.LogError("[DfNewsContentJob] content  html form err : {} ,  url : {}", e,
                        newsItem.NewsUrl);
                    newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                    needUpdateDb = true;
                    break;
                case KDbException:
                    Logger.LogError("[DfNewsContentJob] content  db err : {} ,  url : {}", e, newsItem.NewsUrl);
                    break;
                default:
                    Logger.LogError("[DfNewsContentJob] content  unknow other err : {} ,  url : {}", e,
                        newsItem.NewsUrl);
                    newsItem.DownloadStatusCode = (int)NewsDownloadStatusCode.FailedSyncDetailInfo;
                    break;
            }

            if (needUpdateDb)
                try
                {
                    SpiderNewsDao.UpdateSpiderNewsListInfo(connection, newsItem);
                }
                catch (Exception exception)
                {
                    Logger.LogError("[DfNewsContentJob] UpdateSpiderNewsListInfo err  : {} ,  url : {}", exception,
                        newsItem.NewsUrl);
                }
        }
        finally
        {
            connection.Ado.RollbackTran();
        }
    }
}