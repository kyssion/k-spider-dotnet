using k_spider_dotnet.job;

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