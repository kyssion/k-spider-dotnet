using k_spider_dotnet_lib.job;
using k_spider_dotnet_lib.logger;
using k_spider_script.job;
using k_spider_script.script;
using Microsoft.Extensions.Logging;

namespace k_spider_script;

internal static class Program
{
    // 定义一个静态方法Main，这是C#程序的入口点
    private static readonly ILogger Log = LogFactory.GetLogger<SpiderJobListener>();
    private static readonly AutoResetEvent AutoEvent = new(false);

    public static void Main()
    {
        var start = new Starter();
        start.TransferSpiderDataJob();
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