using KSpider.Config;
using KSpider.Data;
using KSpider.Job;
using KSpider.Job.Check;
using KSpider.Job.News.Flash;
using KSpider.Job.News.Report;
using KSpider.Job.News.Web;
using KSpider.Job.Node;
using KSpider.Job.Ranking;
using KSpider.Spider.News.Web.Cls;
using KSpider.Spider.Verify;
using KSpider.Spider.Verify.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace KSpider;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // Host 默认环境为 Production , 本地不设置时默认 Development ( DOTNET_ENVIRONMENT 可显式覆盖 )
        System.Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT",
            System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development");

        // 配置根固定为程序所在目录 : Host 默认用当前工作目录找 appsettings ,
        // 从仓库根执行 dotnet run --project 时工作目录是仓库根 , 会静默找不到配置并回退代码默认连接串 ( 曾踩坑 )
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });
        // K_SPIDER__ 前缀环境变量覆盖 ( Host 默认只映射 DOTNET_ 前缀 )
        builder.Configuration.AddEnvironmentVariables("K_SPIDER__");

        builder.Services.Configure<DatabaseOptions>(
            builder.Configuration.GetSection(DatabaseOptions.SectionName));
        AddSpiderData(builder.Services);
        builder.Services.AddQuartz(AddSpiderJobs);
        // 优雅停机 : 收到退出信号后等待在跑任务完成
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        var host = builder.Build();
        // 财联社详情页 ( /detail/* SSR ) 被阿里云 WAF 人机验证拦截 , 纯 HTTP 一律拿到滑块页 :
        // 该源显式放开滑块的浏览器自动处理 ( 滑块仍不在全局默认放行集 , 其它源不受影响 ,
        // 见 VerificationPolicy.DefaultAllowedKinds ; 实测挑战页对干净指纹的浏览器会脚本自动放行 , 无需人工 )
        VerificationRegistry.SetPolicy(ClsArticleResource.ResourceHost,
            VerificationPolicy.WithKinds(VerificationKind.SliderCaptcha));
        // 启动时幂等补齐 fail_count 列与轮询索引 ( 库不可用时仅记录日志不阻断 )
        host.Services.GetRequiredService<Pg>().EnsureSpiderNewsListDbObjects();
        // 实时快讯表幂等建表 ( 表 + 实时消费索引 + update_time 触发器 )
        host.Services.GetRequiredService<Pg>().EnsureFlashNewsDbObjects();
        // 研报表幂等建表 ( 第三管线 : 表 + 唯一键 + 摘要回填部分索引 + update_time 触发器 )
        host.Services.GetRequiredService<Pg>().EnsureResearchReportDbObjects();
        // 盘面榜单表幂等建表 ( Ranking 管线 : 表 + 类型行键唯一索引 + 查询索引 + 触发器 )
        host.Services.GetRequiredService<Pg>().EnsureRankingDbObjects();
        // 系统状态表幂等建表 ( Web 控制台通道 : 任务调度态 / 任务指令 / 节点快照 )
        host.Services.GetRequiredService<Pg>().EnsureSystemDbObjects();
        await host.RunAsync();
    }

    /// <summary>
    ///     数据层 DI 注册 ( 各 Job 的构造依赖 ) : 单独成方法 , 供测试校验
    ///     "全部 SpiderJob 的构造参数都可从容器解析" —— DI 缺注册构建期不报错 ,
    ///     只有运行时任务实例化才炸 ( 2026-09-30 研报 DAO 漏注册实测踩过 )。
    /// </summary>
    public static void AddSpiderData(IServiceCollection services)
    {
        services.AddSingleton<Pg>();
        services.AddSingleton<SpiderNewsDao>();
        services.AddSingleton<SpiderNewsBatchDao>();
        services.AddSingleton<SystemStatusDao>();
        services.AddSingleton<SpiderResearchReportDao>();
        services.AddSingleton<SpiderRankingDao>();
    }

    /// <summary>
    ///     集中注册定时任务 ( 间隔 / Cron 与旧版 Starter 一致 )
    /// </summary>
    private static void AddSpiderJobs(IServiceCollectionQuartzConfigurator quartz)
    {
        // 任务执行结果上报 ( 每次执行写 spider_job_state , 供 Web 控制台展示 ) , 作用于全部任务
        quartz.AddJobListener<JobRuntimeListener>();

        quartz.AddJob<NewsListJob>(j => j.WithIdentity("NewsListJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("NewsListJob.Trigger").ForJob("NewsListJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever()));

        // 实时快讯 : 15 秒一轮 ( 发布到入库最坏延迟约 16 秒 ) ; 四源合计约 16 请求/分钟
        quartz.AddJob<FlashNewsJob>(j => j.WithIdentity("FlashNewsJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("FlashNewsJob.Trigger").ForJob("FlashNewsJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(15).RepeatForever()));

        quartz.AddJob<NewsContentOriginJob>(j => j.WithIdentity("NewsContentOriginJob")
                .DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("NewsContentOriginJob.Trigger").ForJob("NewsContentOriginJob")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(3).RepeatForever()));

        quartz.AddJob<NewsContentJob>(j => j.WithIdentity("NewsContentJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("NewsContentJob.Trigger").ForJob("NewsContentJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));

        quartz.AddJob<NewsCheckJob>(j => j.WithIdentity("NewsCheckJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("NewsCheckJob.Trigger").ForJob("NewsCheckJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));

        // 研报 : 5 分钟一轮 ( 发布集中在盘后/早间 , 列表即元数据直写 , 摘要由详情页回填 )
        quartz.AddJob<ResearchReportJob>(j => j.WithIdentity("ResearchReportJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("ResearchReportJob.Trigger").ForJob("ResearchReportJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));

        // 盘面榜单 : 30 分钟一轮 ( 龙虎榜/大宗/两融 , 行即数值发布即终态 , DO NOTHING )
        quartz.AddJob<RankingJob>(j => j.WithIdentity("RankingJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("RankingJob.Trigger").ForJob("RankingJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(30).RepeatForever()));

        // 节点状态任务 : 3 秒刷新调度态上报 + 消费 Web 控制台指令 ( 状态通道 , 不允许暂停自己 )
        quartz.AddJob<NodeStateJob>(j => j.WithIdentity(NodeStateJob.JobName).DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("NodeStateJob.Trigger").ForJob(NodeStateJob.JobName).StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(3).RepeatForever()));
    }
}
