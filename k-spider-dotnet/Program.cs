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
        start.StartStockHkJob();
        start.StartStockCnJob();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Environment.Exit(0);
        };
        OnProcessExit();
        AutoEvent.WaitOne();
    }

    private static void OnProcessExit()
    {
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Log.LogInformation("结束....");
            AutoEvent.Set();
        };
    }
}