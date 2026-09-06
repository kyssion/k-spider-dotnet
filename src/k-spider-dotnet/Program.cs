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
        await host.RunAsync();
    }

    /// <summary>
    ///     集中注册定时任务 ( 间隔 / Cron 与旧版 Starter 一致 ) ; 股票任务按需取消注释启用
    /// </summary>
    private static void AddSpiderJobs(IServiceCollectionQuartzConfigurator quartz)
    {
        quartz.AddJob<DfNewsListJob>(j => j.WithIdentity("DfNewsListJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("DfNewsListJob.Trigger").ForJob("DfNewsListJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever()));

        quartz.AddJob<DfNewsContentOriginJob>(j => j.WithIdentity("DfNewsContentOriginJob")
                .DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("DfNewsContentOriginJob.Trigger").ForJob("DfNewsContentOriginJob")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(3).RepeatForever()));

        quartz.AddJob<DfNewsContentJob>(j => j.WithIdentity("DfNewsContentJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("DfNewsContentJob.Trigger").ForJob("DfNewsContentJob").StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever()));

        quartz.AddJob<DfCheckJob>(j => j.WithIdentity("DfCheckJob").DisallowConcurrentExecution())
            .AddTrigger(t => t.WithIdentity("DfCheckJob.Trigger").ForJob("DfCheckJob").StartNow()
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
