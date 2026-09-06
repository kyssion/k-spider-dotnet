using k_spider_dotnet.job;
using k_spider_dotnet.logger;
using k_spider_dotnet.data;
using k_spider_dotnet.model;
using k_spider_dotnet.spider.df_news;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job.news;

public class DfNewsListJob : SpiderJob
{
    private const string JobName = "DfNewsJob";
    private const string JobDescription = "东方财富网站抓取新闻列表信息任务";

    private const int MaxPageNumber = 4;
    private const int PageSize = 200;

    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsListJob>();
    private static readonly DfListSpider DfListSpider = new();

    public override async Task Execute(IJobExecutionContext context)
    {
        const DfListOrderType orderType = DfListOrderType.ByTime;
        var insertNumber = 0;
        using var connection = Pg.Connection();
        foreach (var resourceItem in DfNewsResource.DfListUrlResourceList)
            try
            {
                for (var pageNumber = 1; pageNumber <= MaxPageNumber; pageNumber++)
                {
                    var dfListInfos = await DfListSpider.GetDfListInfoByPageUrl(resourceItem, pageNumber, PageSize,
                        orderType);
                    if (dfListInfos.Count == 0) break;

                    var dbDfListInfos = dfListInfos.Select(item => item.ToSpiderNewListModel()).ToList();
                    var existsUrls = GetExistsUrls(connection, dbDfListInfos);
                    // 批量 ON CONFLICT DO NOTHING 写入 , 避免逐条查询分流的写放大
                    insertNumber += SpiderNewsBatchDao.UpsertSpiderNewsListOnConflict(connection, dbDfListInfos, 200);

                    // 当前页已无新 URL 说明该栏目翻到了存量区间 , 不再继续翻页
                    if (dbDfListInfos.All(item => existsUrls.Contains(item.NewsUrl))) break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("[DfNewsListJob Execute]  run error : {}", e);
            }
        Logger.LogInformation("[DfNewsListJob Execute] run success , new insert news {}", insertNumber);
    }

    private static List<string?> GetExistsUrls(SqlSugarClient connection, List<SpiderNewsListModel> newsList)
    {
        var urls = newsList.Select(item => item.NewsUrl).ToList();
        return connection.Queryable<SpiderNewsListModel>()
            .Where(it => urls.Contains(it.NewsUrl))
            .Select(it => it.NewsUrl).ToList();
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<DfNewsListJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}
