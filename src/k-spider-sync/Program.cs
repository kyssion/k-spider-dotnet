using k_spider_dotnet.logger;
using k_spider_sync.job;
using Microsoft.Extensions.Logging;

namespace k_spider_sync;

internal static class Program
{
    // 定义一个静态方法Main，这是C#程序的入口点
    private static readonly ILogger Log = LogFactory.GetLogger<Starter>();
    private static readonly AutoResetEvent AutoEvent = new(false);

    public static void Main()
    {
        var start = new Starter();
        start.TransferSpiderDataJob();
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
