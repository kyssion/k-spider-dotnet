using KSpider.Config;
using KSpider.Data;
using KSpider.Job.Check;
using KSpider.Job.News;
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

        var builder = Host.CreateApplicationBuilder(args);
        // K_SPIDER__ 前缀环境变量覆盖 ( Host 默认只映射 DOTNET_ 前缀 )
        builder.Configuration.AddEnvironmentVariables("K_SPIDER__");

        builder.Services.Configure<DatabaseOptions>(
            builder.Configuration.GetSection(DatabaseOptions.SectionName));
        builder.Services.AddSingleton<Pg>();
        builder.Services.AddSingleton<SpiderNewsDao>();
        builder.Services.AddSingleton<SpiderNewsBatchDao>();
        builder.Services.AddSingleton<StockDao>();
        builder.Services.AddQuartz(AddSpiderJobs);
        // 优雅停机 : 收到退出信号后等待在跑任务完成
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        var host = builder.Build();
        // 启动时幂等补齐 fail_count 列与轮询索引 ( 库不可用时仅记录日志不阻断 )
        host.Services.GetRequiredService<Pg>().EnsureSpiderNewsListDbObjects();
        // 实时快讯表幂等建表 ( 表 + 实时消费索引 + update_time 触发器 )
        host.Services.GetRequiredService<Pg>().EnsureFlashNewsDbObjects();
        await host.RunAsync();
    }

    /// <summary>
    ///     集中注册定时任务 ( 间隔 / Cron 与旧版 Starter 一致 ) ; 股票任务按需取消注释启用
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

        // 股票任务 : 按需启用 ( Cron 工作日 20:00 )
        // quartz.AddJob<StockCnJob>(j => j.WithIdentity("StockCnJob").DisallowConcurrentExecution())
        //     .AddTrigger(t => t.WithIdentity("StockCnJob.Trigger").ForJob("StockCnJob").StartNow()
        //         .WithCronSchedule("0 0 20 ? * MON-FRI"));
        // quartz.AddJob<StockHkJob>(j => j.WithIdentity("StockHkJob").DisallowConcurrentExecution())
        //     .AddTrigger(t => t.WithIdentity("StockHkJob.Trigger").ForJob("StockHkJob").StartNow()
        //         .WithCronSchedule("0 0 20 ? * MON-FRI"));
    }
}
