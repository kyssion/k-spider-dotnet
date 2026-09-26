namespace KSpider.Sync.Transfer;

/// <summary>
///     同步任务的远端 / 本地库配置 , 支持环境变量覆盖
/// </summary>
public static class TransferConfig
{
    /// <summary>
    ///     远端库兜底默认值 ( 开发环境 ) , 环境变量未设置时使用
    /// </summary>
    private const string DefaultRemoteConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=192.168.5.78;PASSWORD=14159265jkl;USER ID=postgres ;Include Error Detail=true;Pooling=true;MaxPoolSize=100";

    /// <summary>
    ///     本地库兜底默认值 , 环境变量未设置时使用
    /// </summary>
    private const string DefaultLocalConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=127.0.0.1;PASSWORD=14159265jkl;USER ID=postgres ;Include Error Detail=true;Pooling=true;MaxPoolSize=100";

    /// <summary>
    ///     远端库 ( 被搬运方 ) 连接串 , 环境变量 K_SPIDER_REMOTE__CONNECTIONSTRING 可覆盖
    /// </summary>
    public static string RemoteConnectionString =>
        Environment.GetEnvironmentVariable("K_SPIDER_REMOTE__CONNECTIONSTRING") ?? DefaultRemoteConnectionString;

    /// <summary>
    ///     本地库 ( 搬入方 ) 连接串 , 环境变量 K_SPIDER_LOCAL__CONNECTIONSTRING 可覆盖
    /// </summary>
    public static string LocalConnectionString =>
        Environment.GetEnvironmentVariable("K_SPIDER_LOCAL__CONNECTIONSTRING") ?? DefaultLocalConnectionString;
}
