using k_spider_dotnet.dal.db;
using k_spider_dotnet.dao;
using k_spider_dotnet.model;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.log;
using k_spider_dotnet.tool.resource;
using k_spider_dotnet.tool.time;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.df_news_job;

public class DfNewsJob : SpiderJob
{
    private const string JobName = "DfNewsJob";
    private const string JobDescription = "东方财富网站抓取新闻列表信息任务";
    
    private static readonly ILogger Logger = LogFactory.GetLogger<DfNewsJob>();
    private static readonly DfListSpider DfListSpider = new();
    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            try
            {
                const int startNumber = 1;const int endNumber = 3; const int pageSize = 200;
                const int maxBatchSize = 600;
                var connection = Pg.Connection();
                foreach (var resourceItem in DfResource.DfListUrlResourceList)
                {
                    Logger.LogInformation("[DfNewsJob Execute] start news , model name : {} ", resourceItem.CategoryInfo.CategoryName);
                    var dfListInfos = DfListSpider.GetDfListInfoByUrl(resourceItem,startNumber, endNumber, pageSize,DfListOrderType.ByTime).Result;
                    var dbDfListInfos = dfListInfos.Select(item => new SpiderNewsListModel
                        {
                            FromMedia = (int)NewsFromType.DfMedia,
                            NewsUrl = item.NewsUrl,
                            NewsTitle = item.NewsTitle,
                            NewsSummary = item.NewsSummary,
                            NewsFrom = item.NewsFrom,
                            NewsTime = TimeTools.GetDateByTimeStr(item.NewsTime ?? "", TimeTools.DfTimeFormat),
                            NewsDownloadTime = item.NewsDownloadTime,
                            Category = item.Category
                        }).GroupBy(item =>  item.NewsUrl).Select(item=>item.First())
                        .ToList();
                    Logger.LogInformation("[DfNewsJob Execute] insert news info number : {}",SpiderNewsListDao.UpsertSpiderNewsList(connection,dbDfListInfos,maxBatchSize));
                }
            }
            catch (Exception e)
            {
                Logger.LogError("[DfNewsJob Execute]  run error : {}", e.ToString());
                throw;
            }
        });
    }

    public override ITrigger GetTrigger(string jobGroup,IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
                 .WithIdentity(DfNewsJob.JobName+".Trigger", jobGroup+".Trigger").StartNow()
                 .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever().Build())
                 .Build(); 
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<DfNewsJob>().WithIdentity(DfNewsJob.JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution()// 禁止并发执行
            .WithDescription(DfNewsJob.JobDescription).Build();
    }
}
