using System.Threading.Channels;
using k_spirder_script.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace k_spirder_script.script;

public class TransferSiderData
{
    // 同步远程数据库脚本到本子
    public static void DoTransfer()
    {
        var remoteOtions = new DbContextOptionsBuilder<KScriptSpiderContext>().UseNpgsql("PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=100").Options;
        var remoteFactory = new PooledDbContextFactory<KScriptSpiderContext>(remoteOtions,100);
        var locationOtions = new DbContextOptionsBuilder<KScriptSpiderContext>().UseNpgsql("PORT=5432;DATABASE=k_script_spider;HOST=192.168.0.102;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=100").Options;
        var locationFactory = new PooledDbContextFactory<KScriptSpiderContext>(locationOtions, 100);
        
        var taskNewsList = SyncSpiderNewsList(remoteFactory, locationFactory);
        var taskNewsContentOrigin =SyncSpiderNewsContentOrigin(remoteFactory, locationFactory);
        var taskNewsContent =SyncSpiderNewsContent(remoteFactory, locationFactory);
        var taskNewsImage =SyncSpiderNewsImage(remoteFactory,locationFactory);
        Task.WhenAll(taskNewsList,taskNewsContentOrigin,taskNewsContent,taskNewsImage).GetAwaiter().GetResult();
    }

    private static async Task SyncSpiderNewsContentOrigin(PooledDbContextFactory<KScriptSpiderContext> remoteFactory,
        PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = !locationContext.SpiderNewsContentOrigins.Any()
                ? 0
                :locationContext.SpiderNewsContentOrigins.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsContentOrigins.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                locationContext.SpiderNewsContentOrigins.Add(item);
                await locationContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            throw; // TODO 处理异常
        }
    }
    private static async Task SyncSpiderNewsContent(PooledDbContextFactory<KScriptSpiderContext> remoteFactory,
        PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = !locationContext.SpiderNewsContents.Any()
                ? 0
                :locationContext.SpiderNewsContents.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsContents.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                locationContext.SpiderNewsContents.Add(item);
                await locationContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            throw; // TODO 处理异常
        }
    }
    
    
    private static async Task SyncSpiderNewsImage(PooledDbContextFactory<KScriptSpiderContext> remoteFactory,
        PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = !locationContext.SpiderNewsImageLists.Any()
                ? 0
                : locationContext.SpiderNewsImageLists.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsImageLists.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                locationContext.SpiderNewsImageLists.Add(item);
                await locationContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            throw; // TODO 处理异常
        }
    }

    private static async Task SyncSpiderNewsList(PooledDbContextFactory<KScriptSpiderContext> remoteFactory, PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = locationContext.SpiderNewsLists.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsLists.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                locationContext.SpiderNewsLists.Add(item);
                await locationContext.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            throw; // TODO 处理异常
        }
    }
}