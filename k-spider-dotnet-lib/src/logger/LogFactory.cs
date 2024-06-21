using Microsoft.Extensions.Logging;

namespace k_spider_dotnet_lib.logger;

public class LogFactory
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder.AddSimpleConsole(
        options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "HH:mm:ss ";
        })
    );

    public static ILogger<T> GetLogger<T>()
    {
        return Factory.CreateLogger<T>();
    }
}