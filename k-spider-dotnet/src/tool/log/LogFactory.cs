using Microsoft.Extensions.Logging;

namespace k_spider_dotnet.tool.log;

public class LogFactory
{
    private static readonly ILoggerFactory Factory =  LoggerFactory.Create(builder => builder.AddConsole());
    public static ILogger GetLogger(string name)
    {
        return Factory.CreateLogger(name);
    }
}