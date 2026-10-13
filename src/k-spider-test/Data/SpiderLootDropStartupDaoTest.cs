using KSpider.Data;
using KSpider.Model;
using KSpider.Spider;
using SqlSugar;

namespace KSpider.Test.Data;

/// <summary>
///     数据集 DAO 手拼 SQL 形态锁 ( 离线 , 不连库 ) :
///     spider_lootdrop_startup 的 upsert 是本仓库第一个 ON CONFLICT DO UPDATE ,
///     与新闻系的 DO NOTHING 形态差异必须锁住 —— 三个不变量 :
///     ① INSERT 必须带源侧 id 列 ( 非自增主键 , 漏写整批主键为 0 ) ;
///     ② INSERT 不含 create_time/update_time ( 库默认值 + 触发器管理 ) ;
///     ③ 冲突子句以 content_hash 变更为条件 ( 变更检测语义的核心 )。
///     SqlSugar 的 ToSqlString 是纯内存操作 , 不需要真实连接。
/// </summary>
[TestClass]
public class SpiderLootDropStartupDaoTest
{
    [TestMethod]
    public void UpsertSqlKeepsSourceIdAndHashGuard()
    {
        var startup = new SpiderLootDropStartupModel
        {
            Id = 2563,
            FromMedia = (int)FromTypeOfNews.LootDropMedia,
            Name = "Plenty Unlimited",
            Sector = "Materials",
            TotalFunding = 944000000,
            ContentHash = "abc123",
            RawContent = "{\"id\":2563}"
        };
        var client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            DbType = SqlSugar.DbType.PostgreSQL,
            IsAutoCloseConnection = true
        });
        var item = client.Insertable(new List<SpiderLootDropStartupModel> { startup })
            .IgnoreColumns("create_time", "update_time");
        item.InsertBuilder.IsNoPage = true;
        var insertSql = SpiderNewsBatchDao.TrimInsertSqlTail(item.ToSqlString());

        // 形态锁 ① : 源侧 id 必须在列清单与值里 ( 与研报表 IgnoreColumns("id") 的自增形态相反 )
        Assert.IsTrue(insertSql.Contains("\"id\"", StringComparison.OrdinalIgnoreCase),
            $"INSERT 应包含 id 列 : {insertSql[..Math.Min(300, insertSql.Length)]}");

        // 形态锁 ② : 库管理列不出现
        Assert.IsFalse(insertSql.Contains("create_time"), "INSERT 不应包含 create_time");
        Assert.IsFalse(insertSql.Contains("update_time"), "INSERT 不应包含 update_time");

        // 形态锁 ③ : 冲突子句带哈希变更守卫 , 且更新列不含 from_media
        var sql = SpiderLootDropStartupDao.BuildUpsertSql(insertSql);
        var marker = "ON CONFLICT (id) DO UPDATE SET";
        var conflictIndex = sql.IndexOf(marker, StringComparison.Ordinal);
        Assert.IsTrue(conflictIndex >= 0, "缺少 ON CONFLICT DO UPDATE 子句");
        StringAssert.Contains(sql, "WHERE spider_lootdrop_startup.content_hash IS DISTINCT FROM EXCLUDED.content_hash");
        var updateSection = sql[(conflictIndex + marker.Length)..];
        Assert.IsFalse(updateSection.Contains("from_media"), "冲突更新不应改写 from_media");
        StringAssert.Contains(updateSection, "content_hash = EXCLUDED.content_hash");
        StringAssert.Contains(updateSection, "raw_content = EXCLUDED.raw_content");
    }
}
