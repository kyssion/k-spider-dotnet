using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using k_spider_dotnet.job;
using Microsoft.Extensions.Logging;

namespace k_spider_dotnet;

public static class Program
{
    private static readonly ILogger Log = LogFactory.GetLogger<SpiderJobListener>();
    private static readonly AutoResetEvent AutoEvent = new(false);

    public static void Main()
    {
        var start = new Starter();
        start.StartDfListNewsJob();
        start.StartDfContentNewsJob();
        start.StartDfContentNewsOriginJob();
        start.StartCheckJob();
        // start.StartStockHkJob();
        // start.StartStockCnJob();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            AutoEvent.Set();
        };
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => AutoEvent.Set();
        AutoEvent.WaitOne();
        Log.LogInformation("收到退出信号 , 等待任务完成后停止....");
        start.Shutdown();
        Log.LogInformation("结束....");
    }
}
