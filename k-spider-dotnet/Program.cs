using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using k_spider_dotnet.job;
using k_spider_dotnet.script.df_news.spider;

namespace k_spider_dotnet;
public static class KSpiderMain
{
    

    public class Program
    {
        public static void Main()
        {
           Starter.StartDfListNewsJob();
           Console.ReadLine();
        }
    }
    
}
