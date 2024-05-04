using k_spider_dotnet.job;
using k_spider_dotnet.tool.log;
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