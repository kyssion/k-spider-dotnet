using System.ComponentModel.DataAnnotations;

namespace KSpider.Admin.Repository;

/// <summary>
///     PostgreSQL 连接配置。
/// </summary>
/// <remarks>
///     连接串由部署方通过 <c>K_SPIDER__DATABASE__CONNECTIONSTRING</c> 或 appsettings 注入；
///     仓库不保存真实凭据。空值会在 Options 校验阶段直接阻止 admin 启动。
/// </remarks>
public sealed class DatabaseOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "Database";

    /// <summary>Npgsql 连接串。</summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
