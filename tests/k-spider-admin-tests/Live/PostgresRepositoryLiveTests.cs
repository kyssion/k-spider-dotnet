using System.Text;
using KSpider.Admin.Hosting;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace KSpider.Admin.Tests.Live;

/// <summary>
///     admin PostgreSQL 仓储真实读写测试。
/// </summary>
/// <remarks>
///     默认门禁排除 <c>TestCategory=Live</c>；运行前注入 <c>K_SPIDER_LIVE_DB</c>（Npgsql 连接串），
///     并准备 DDL 与 <c>test-site</c> 联调夹具。测试会短暂暂停/恢复测试渠道，并清理自己插入的任务。
/// </remarks>
[TestClass]
[TestCategory("Live")]
public sealed class PostgresRepositoryLiveTests
{
    private const string FixtureRecordKey = "test-site:sha256:web-fixture-1";

    /// <summary>
    ///     校验总览与原始数据仓储能读取真实 PostgreSQL，并区分列表不读 raw、详情读取 raw。
    /// </summary>
    [TestMethod]
    public async Task OverviewAndRawDataRepositoriesReadPostgres()
    {
        var connectionString = RequireConnectionString();
        await using var provider = BuildProvider(connectionString);

        var overview = await provider.GetRequiredService<IOverviewRepository>()
            .GetAsync(CancellationToken.None);
        Assert.IsTrue(overview.Channels.Total >= 1, "Live 库需要至少一个渠道夹具");

        var rawData = provider.GetRequiredService<IRawDataRepository>();
        var sources = await rawData.QuerySourcesAsync(
            new RawDataSourceQuery(1, 100),
            CancellationToken.None);
        var source = sources.Items.SingleOrDefault(item => item.RecordKey == FixtureRecordKey);
        Assert.IsNotNull(source, $"Live 库缺少 {FixtureRecordKey} 夹具");
        Assert.IsNull(source.Raw, "source 列表查询不应读取 raw");

        var sourceDetail = await rawData.GetSourceAsync(source.Id, CancellationToken.None);
        Assert.IsNotNull(sourceDetail);
        Assert.IsNotNull(sourceDetail.Raw);
        Assert.AreEqual("hello from k-spider real postgres", Encoding.UTF8.GetString(sourceDetail.Raw));

        var details = await rawData.QueryDetailsAsync(source.Id, 1, 100, CancellationToken.None);
        var detail = details.Items.Single();
        Assert.IsNull(detail.Raw, "detail 列表查询不应读取 raw");
        var detailDetail = await rawData.GetDetailAsync(detail.Id, CancellationToken.None);
        Assert.IsNotNull(detailDetail);
        Assert.IsNotNull(detailDetail.Raw);
        Assert.AreEqual("hello detail from real postgres", Encoding.UTF8.GetString(detailDetail.Raw));
    }

    /// <summary>
    ///     校验渠道状态条件更新的真实写入；结束时恢复原状态，避免污染后续 Live 用例。
    /// </summary>
    [TestMethod]
    public async Task ChannelRepositoryPausesAndResumes()
    {
        var connectionString = RequireConnectionString();
        await using var provider = BuildProvider(connectionString);

        var channels = provider.GetRequiredService<IChannelRepository>();
        var page = await channels.QueryAsync(new ChannelQuery(1, 100), CancellationToken.None);
        var channel = page.Items.SingleOrDefault(item => item.ChannelName == "测试渠道");
        Assert.IsNotNull(channel, "Live 库缺少“测试渠道”夹具");

        var original = channel.Status;
        try
        {
            Assert.IsTrue(await channels.UpdateStatusAsync(channel.Id, "paused", CancellationToken.None));
            Assert.AreEqual("paused", (await channels.GetAsync(channel.Id, CancellationToken.None))?.Status);

            Assert.IsTrue(await channels.UpdateStatusAsync(channel.Id, "active", CancellationToken.None));
            Assert.AreEqual("active", (await channels.GetAsync(channel.Id, CancellationToken.None))?.Status);
        }
        finally
        {
            await channels.UpdateStatusAsync(channel.Id, original, CancellationToken.None);
        }
    }

    /// <summary>
    ///     校验任务条件更新：pushing → pushed → cancelled，并验证已取消任务不能再次取消。
    /// </summary>
    [TestMethod]
    public async Task TaskRepositoryCancelsOnlyFromNonTerminalState()
    {
        var connectionString = RequireConnectionString();
        await using var provider = BuildProvider(connectionString);

        var channels = provider.GetRequiredService<IChannelRepository>();
        var channelPage = await channels.QueryAsync(new ChannelQuery(1, 100), CancellationToken.None);
        Assert.IsTrue(channelPage.Items.Count > 0, "Live 库需要至少一个渠道夹具");
        var channel = channelPage.Items[0];

        var tasks = provider.GetRequiredService<ITaskRepository>();
        var token = $"live-{Guid.NewGuid():N}";
        var inserted = await tasks.InsertAsync(
            new SysTaskInsert(
                Guid.NewGuid(),
                "channel_round",
                $$"""{"channel_id":{{channel.Id}},"round_type":"list","batch_limit":1}""",
                "pushing",
                $$"""{"dispatch_token":"{{token}}","fail_count":0}"""),
            CancellationToken.None);

        try
        {
            Assert.AreEqual("pushing", (await tasks.GetAsync(inserted.Id, CancellationToken.None))?.Status);
            Assert.IsTrue(await tasks.TryMarkPushedAsync(inserted.Id, token, CancellationToken.None));
            Assert.AreEqual("pushed", (await tasks.GetAsync(inserted.Id, CancellationToken.None))?.Status);

            Assert.IsTrue(await tasks.TryCancelAsync(inserted.Id, "live cancel", CancellationToken.None));
            Assert.AreEqual("cancelled", (await tasks.GetAsync(inserted.Id, CancellationToken.None))?.Status);
            Assert.IsFalse(await tasks.TryCancelAsync(inserted.Id, "again", CancellationToken.None));
        }
        finally
        {
            await DeleteTaskAsync(connectionString, inserted.Id);
        }
    }

    /// <summary>读取 Live 连接串；缺失时标记 Inconclusive，避免误用默认库。</summary>
    private static string RequireConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("K_SPIDER_LIVE_DB");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("未设置 K_SPIDER_LIVE_DB，跳过 admin PostgreSQL Live 测试");
        }

        return connectionString;
    }

    /// <summary>只构建 DI 容器，不启动 hosted service，避免调度循环访问真实库。</summary>
    private static ServiceProvider BuildProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = connectionString,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAdminServices(configuration);
        return services.BuildServiceProvider();
    }

    /// <summary>清理 Live 测试插入的任务，避免 sys_task 持续增长。</summary>
    private static async Task DeleteTaskAsync(string connectionString, long taskId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DELETE FROM sys_task WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", taskId);
        await command.ExecuteNonQueryAsync();
    }
}
