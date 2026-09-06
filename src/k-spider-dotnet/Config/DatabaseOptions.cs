namespace KSpider.Config;

/// <summary>
///     数据库配置 , 绑定 appsettings 的 Database 节 , 环境变量 K_SPIDER__DATABASE__CONNECTIONSTRING 可覆盖
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    ///     连接串兜底默认值 ( 本地开发库 ) , 配置为空时使用
    /// </summary>
    public const string DefaultConnectionString =
        "PORT=5432;DATABASE=k_script_spider;HOST=127.0.0.1;PASSWORD=14159265jkl;USER ID=postgres ;Include Error Detail=true;Pooling=true;MaxPoolSize=10";

    /// <summary>
    ///     PostgreSQL 连接串 ( 库 k_script_spider ) ; test/prod 由部署方用环境变量
    ///     K_SPIDER__DATABASE__CONNECTIONSTRING 注入 , 不在仓库保存真实凭据
    /// </summary>
    public string ConnectionString { get; set; } = DefaultConnectionString;
}
