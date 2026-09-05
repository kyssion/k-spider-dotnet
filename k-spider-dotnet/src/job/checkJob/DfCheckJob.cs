using System.Text.Json.Nodes;
using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet.script.df_news.spider;
using k_spider_dotnet.tool.http;
using Microsoft.Extensions.Logging;
using Quartz;

namespace k_spider_dotnet.job.checkJob;

public class DfCheckJob: SpiderJob
{
    public static void Run()
    {
        var item = new DfCheckJob();
        item.Execute(null).GetAwaiter().GetResult();
    }
    
    private const string JobName = "DfCheckJob";
    private const string JobDescription = "东方财富网站抓取新闻列表check任务";

    private static readonly ILogger Logger = LogFactory.GetLogger<DfCheckJob>();
    public override Task Execute(IJobExecutionContext context)
    {
        return Task.Run(async () =>
        {
            const DfListOrderType orderType = DfListOrderType.ByTime;
            foreach (var resourceItem in DfNewsResource.DfListUrlResourceList)
                try
                {
                    var urlNow = string.Format(DfNewsResource.RequestDfListUrl, resourceItem.ListResourceNumber, (int)orderType, 1, 10, DateTime.Now.Millisecond);
                    var responseString = await HttpClientTools.CreateByHost(DfNewsResource.ListResourceHost).GetStringAsync(urlNow);
                    var forecastNode = JsonNode.Parse(responseString)!;
                    var jsonData = forecastNode["data"];
                    if (jsonData?["list"] == null)
                    {
                        throw new Exception("not find date");
                    }
                    var jsonDataList = (JsonArray)jsonData["list"]!;
                    if (jsonDataList.Count == 0)
                    {
                        throw new Exception("not find date");
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("[DfNewsListJob Execute]  run error : {}, resourceItem id : {}", e,resourceItem.ListResourceNumber);
                }
        });
    }

    public override ITrigger GetTrigger(string jobGroup, IJobDetail jobDetail)
    {
        return TriggerBuilder.Create().ForJob(jobDetail)
            .WithIdentity(JobName + ".Trigger", jobGroup + ".Trigger").StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever().Build())
            .Build();
    }

    public override IJobDetail GetJobDetail(string jobGroup)
    {
        return JobBuilder.Create<DfCheckJob>().WithIdentity(JobName + ".Job", jobGroup + ".Job")
            .DisallowConcurrentExecution() // 禁止并发执行
            .WithDescription(JobDescription).Build();
    }
}