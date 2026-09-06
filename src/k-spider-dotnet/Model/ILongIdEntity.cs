namespace KSpider.Model;

/// <summary>
///     拥有自增 bigint 主键的表实体 , 数据同步按 Id 增量推进时使用
/// </summary>
public interface ILongIdEntity
{
    long Id { get; set; }
}
