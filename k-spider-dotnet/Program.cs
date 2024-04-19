using System.Collections.Concurrent;
using System.Text.Json;
using k_spider_dotnet.script.df_news.spider;

namespace k_spider_dotnet;
public static class KSpiderMain
{
    
    public struct WeatherForecast
    {
        public DateTimeOffset Date;
        public int TemperatureCelsius;
        public string? Summary;
    }

    public class Program
    {
        public static void Main()
        {
            Case1(null);
        }
    }
    private static int counter;

    public static void Case1(string[] args)
    {
        var startTime = DateTime.Now;
        var idList = new ConcurrentQueue<int>();
        counter = 1000000;
        for (var a = 0; a < 1000000; a++)
        {
            var indexNow = a;
            Task.Run(async () =>
            {
                // Console.WriteLine($"start : {{id}} {Thread.CurrentThread.ManagedThreadId}");
                await Task.Delay(10000);
                idList.Enqueue(indexNow);
                // Console.WriteLine($"end : {{id}} {Thread.CurrentThread.ManagedThreadId}");
                Interlocked.Decrement(ref counter);
            });
        }

        while (counter != 0)
        {
        }
        Console.WriteLine(idList.Count);
        Console.WriteLine(DateTime.Now.Subtract(startTime).TotalMilliseconds);
    }
}
