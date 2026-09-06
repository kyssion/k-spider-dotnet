using Microsoft.Extensions.Logging;

namespace KSpider.Logger;

public class LogFactory
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder => builder.AddSimpleConsole(
        options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        })
    );

    public static ILogger<T> GetLogger<T>()
    {
        return Factory.CreateLogger<T>();
    }
}