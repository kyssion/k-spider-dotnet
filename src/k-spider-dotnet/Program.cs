using KSpider.Config;
using KSpider.Data;
using KSpider.Job.Check;
using KSpider.Job.News.Flash;
using KSpider.Job.News.Web;
using KSpider.Spider.News.Web.Cls;
using KSpider.Spider.Verify;
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
        builder.Services.AddSingleton<Pg>();
        builder.Services.AddSingleton<SpiderNewsDao>();
        builder.Services.AddSingleton<SpiderNewsBatchDao>();
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
        await host.RunAsync();
    }

    /// <summary>
    ///     集中注册定时任务 ( 间隔 / Cron 与旧版 Starter 一致 )
    /// </summary>
    private static void AddSpiderJobs(IServiceCollectionQuartzConfigurator quartz)
    {
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
    }
}
