namespace KSpider.Model;

/// <summary>
///     拥有自增 bigint 主键的表实体 , 数据同步按 Id 增量推进时使用
/// </summary>
public interface ILongIdEntity
{
    long Id { get; set; }
}

/// <summary>
///     拥有 update_time 列的表实体 , 数据同步按 (update_time, id) 双键水位增量更新已有行时使用
/// </summary>
public interface IUpdateTimeEntity
{
    DateTime UpdateTime { get; set; }
}
