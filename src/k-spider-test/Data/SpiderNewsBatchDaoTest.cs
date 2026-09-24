using KSpider.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Data;

/// <summary>
///     批量 upsert 的 SQL 尾部处理回归。
///     实测 SqlSugar 5.1.4.216 ( IsNoPage = true ) 的 ToSqlString 结尾形态 :
///     多条 ( ≥2 ) 以 ";" 结尾 ; 单条以 VALUES 段结尾 , 不带分号也无 returning。
///     旧实现用 [..LastIndexOf(';')] 截断 , 单条时 LastIndexOf 返回 -1 直接抛参数越界 ,
///     这里用真实形态的夹具锁住 TrimInsertSqlTail 的边界行为。
/// </summary>
[TestClass]
public class SpiderNewsBatchDaoTest
{
    /// <summary>多条形态 : 取自真实 ToSqlString 输出尾部 ( 字段值无关紧要 , 只看结尾 )</summary>
    private const string MultiRowInsertSql =
        "INSERT INTO spider_flash_news (from_media,category,news_url,title) " +
        "VALUES (2,101,'https://x/1','t1'),(2,101,'https://x/2','t2')\n;";

    /// <summary>单条形态 : 取自真实 ToSqlString 输出尾部 , 以 VALUES 段结尾无分号</summary>
    private const string SingleRowInsertSql =
        "INSERT INTO spider_flash_news (from_media,category,news_url,title) " +
        "VALUES\n           (2,101,N'https://x/1',N't1')";

    [TestMethod]
    public void MultiRowSqlTrailingSemicolonIsRemoved()
    {
        var trimmed = SpiderNewsBatchDao.TrimInsertSqlTail(MultiRowInsertSql);

        Assert.IsFalse(trimmed.EndsWith(';'), "结尾分号应被去掉 , 否则无法拼接 ON CONFLICT 子句");
        Assert.IsTrue(MultiRowInsertSql.StartsWith(trimmed), "除结尾分号外不应改动其它内容");
    }

    [TestMethod]
    public void SingleRowSqlWithoutSemicolonIsKeptIntact()
    {
        // 夹具自检 : 单条形态确实没有分号 ( 旧写法在这一步 LastIndexOf 返回 -1 抛越界 )
        Assert.AreEqual(-1, SingleRowInsertSql.LastIndexOf(';'));

        Assert.AreEqual(SingleRowInsertSql, SpiderNewsBatchDao.TrimInsertSqlTail(SingleRowInsertSql));
    }

    [TestMethod]
    public void BlankSqlStaysEmpty()
    {
        Assert.AreEqual("", SpiderNewsBatchDao.TrimInsertSqlTail("  \n"));
    }
}
