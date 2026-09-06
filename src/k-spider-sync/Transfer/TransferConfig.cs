namespace KSpider.Sync.Transfer;

/// <summary>
///     同步任务的远端 / 本地库配置 , 支持环境变量覆盖
/// </summary>
public static class TransferConfig
{
    private const string DefaultRemoteConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=192.168.5.78;PASSWORD=14159265jkl;USER ID=postgres ;Include Error Detail=true;Pooling=true;MaxPoolSize=100";

    private const string DefaultLocalConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=127.0.0.1;PASSWORD=14159265jkl;USER ID=postgres ;Include Error Detail=true;Pooling=true;MaxPoolSize=100";

    public static string RemoteConnectionString =>
        Environment.GetEnvironmentVariable("K_SPIDER_REMOTE__CONNECTIONSTRING") ?? DefaultRemoteConnectionString;

    public static string LocalConnectionString =>
        Environment.GetEnvironmentVariable("K_SPIDER_LOCAL__CONNECTIONSTRING") ?? DefaultLocalConnectionString;
}
