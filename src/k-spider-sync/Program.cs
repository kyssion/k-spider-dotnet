using KSpider.Sync.Job;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace KSpider.Sync;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddQuartz(quartz =>
        {
            quartz.AddJob<TransferSpiderDataJob>(j => j.WithIdentity("TransferSpiderDataJob")
                    .DisallowConcurrentExecution())
                .AddTrigger(t => t.WithIdentity("TransferSpiderDataJob.Trigger").ForJob("TransferSpiderDataJob")
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(2).RepeatForever()));
        });
        // 优雅停机 : 收到退出信号后等待在跑任务完成
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        await builder.Build().RunAsync();
    }
}
