using Microsoft.Extensions.Configuration;

namespace k_spider_dotnet.config;

/// <summary>
///     全局配置中心。
///     读取优先级 : appsettings.json -> appsettings.{环境}.json -> K_SPIDER_ 前缀环境变量 ( 层级用双下划线 ) -> 代码内默认值。
///     环境由 DOTNET_ENVIRONMENT 指定 ( dev / test / prod ) , 未设置时默认 dev。
/// </summary>
public static class AppConfig
{
    /// <summary>
    ///     当前环境 : dev ( 本地默认 ) / test / prod , 打包发布时与 -p:SpiderEnvironment 对齐
    /// </summary>
    public static string Environment { get; } =
        System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "dev";

    private static readonly IConfiguration Root = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", true)
        .AddJsonFile($"appsettings.{Environment}.json", true)
        // 前缀须含双下划线 : K_SPIDER__DATABASE__CONNECTIONSTRING -> Database:ConnectionString ( 层级用 __ )
        .AddEnvironmentVariables("K_SPIDER__")
        .Build();

    /// <summary>
    ///     读取字符串配置 , 未配置或为空白时返回默认值
    /// </summary>
    public static string Get(string key, string defaultValue = "")
    {
        var value = Root[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    public static class Database
    {
        private const string DefaultConnectionString =
            "PORT=5432;DATABASE=k_script_spider;HOST=127.0.0.1;PASSWORD=14159265jkl;USER ID=postgres ;Include Error Detail=true;Pooling=true;MaxPoolSize=10";

        /// <summary>
        ///     PostgreSQL 连接串 ( 库 k_script_spider )
        /// </summary>
        public static string ConnectionString => Get("Database:ConnectionString", DefaultConnectionString);
    }
}
