using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.lark;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet.dal.db;
using k_spider_dotnet.job.dfNewsJob;
using k_spider_dotnet.model;
using k_spider_dotnet.script;
using Microsoft.Extensions.Logging;
using Quartz;
using SqlSugar;

namespace k_spider_dotnet.job.larkJob;

public class SendLarkNewsMessageJob: SpiderJob
{
    private const string JobName = "SendLarkNewsMessageJob";
    private const string JobDescription = "新闻信息推送lark任务";

    private static long _beforeIndex = long.MaxValue;

    private static readonly ILogger Logger = LogFactory.GetLogger<SendLarkNewsMessageJob>();

    public void SendNewsMessage()
    {
        using var connection = Pg.Connection();
        var lastOne = connection.Queryable<SpiderNewsListModel>().OrderBy(it => it.Id, OrderByType.Desc).Take(1).ToList()[0];
        if (_beforeIndex == long.MaxValue)
        {
            if (lastOne == null)
            {
                Logger.LogError("[SendLarkNewsMessageJob] last one date not find");
                return;
            }
            _beforeIndex = lastOne.Id - 10;
        }

        var beforeIndex = _beforeIndex;
        var newsListInfos = connection.Queryable<SpiderNewsListModel>()
            .Where(it => it.Id>beforeIndex && it.FromMedia == (int)FromTypeOfNews.DfMedia)
            .OrderBy(item => item.NewsTime, OrderByType.Desc).ToList();
        var maxIndex = _beforeIndex;
        foreach (var spiderItem in newsListInfos)
        {
            maxIndex = Math.Max(maxIndex, spiderItem.Id);
            LarkMessage.SendTemplateMessage("cli_a6c6ce8d66fa500e", "gX9w2dWfiX9cvHm4oCvR7eHG7lDDJqD2",
                "chat_id", "oc_43cfa41c91def10da22305278eb4de3e", new LarkMessage.TemplateInfo
                {
                    Data = new LarkMessage.TemplateData
                    {
                        TemplateId = "ctp_AAk5g6Ps48sP",
                        TemplateVariable =new Dictionary<string, object>()
                        {
                            { "news_title" , spiderItem.NewsTitle??""},
                            { "news_summary", spiderItem.NewsSummary??""},
                            { "from_media", "东方财富"},
                            { "news_from", spiderItem.NewsFrom??""},
                            { "news_time", spiderItem.NewsTime.ToString()??""},
                            { "news_url", spiderItem.NewsUrl??""}
                        }
                    }
                }).Wait();
        }
        _beforeIndex = maxIndex;
    }
    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(SendNewsMessage);
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<SendLarkNewsMessageJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}