using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Guards;

[TestClass]
public class DdlContractTests
{
    private static readonly string Root = RepoRoot.Find();

    private static readonly (string File, string[] Tables)[] ExpectedTables =
    [
        ("001_business_tables.sql", ["crawler_adapter", "channel_category", "channel", "data_source", "data_detail"]),
        ("002_sys_tables.sql", ["sys_node", "sys_task"]),
        ("003_auth_tables.sql", ["sys_user"]),
    ];

    [TestMethod]
    public void DdlFilesExistAndAreIdempotent()
    {
        foreach (var (file, _) in ExpectedTables)
        {
            var sql = ReadDdl(file);
            Assert.IsFalse(
                Regex.IsMatch(sql, @"CREATE\s+TABLE\s+(?!IF\s+NOT\s+EXISTS)", RegexOptions.IgnoreCase),
                $"{file} 存在非幂等的 CREATE TABLE");
            Assert.IsFalse(
                Regex.IsMatch(sql, @"CREATE\s+(?:UNIQUE\s+)?INDEX\s+(?!IF\s+NOT\s+EXISTS)", RegexOptions.IgnoreCase),
                $"{file} 存在非幂等的 CREATE INDEX");
        }
    }

    [TestMethod]
    public void DdlContainsExpectedTablesAndComments()
    {
        foreach (var (file, tables) in ExpectedTables)
        {
            var sql = ReadDdl(file);
            foreach (var table in tables)
            {
                Assert.IsTrue(
                    Regex.IsMatch(
                        sql,
                        $@"CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+{Regex.Escape(table)}\b",
                        RegexOptions.IgnoreCase),
                    $"{file} 缺少表 {table}");
                Assert.IsTrue(
                    Regex.IsMatch(
                        sql,
                        $@"COMMENT\s+ON\s+TABLE\s+{Regex.Escape(table)}\s+IS\s+'",
                        RegexOptions.IgnoreCase),
                    $"{file} 缺少表 {table} 的 COMMENT ON TABLE");
            }
        }
    }

    private static string ReadDdl(string file)
    {
        return File.ReadAllText(Path.Combine(Root, "db", file));
    }
}
