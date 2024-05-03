using k_spider_dotnet.exception;
using k_spider_dotnet.job;
using Npgsql.Replication.TestDecoding;

namespace k_spider_dotnet;

public static class Program
{
    public static void Main()
    {
        var start = new Starter();
        start.StartDfListNewsJob();
        start.StartDfContentNewsJob();
        start.StartDfContentNewsOriginJob();
        Console.ReadLine();
    }
}
