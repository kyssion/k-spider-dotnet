using k_spider_dotnet_lib.logger;
using k_spider_script.job;
using k_spider_script.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace k_spider_script.script;

public static class TransferSpiderData
{
    private static readonly ILogger Logger = LogFactory.GetLogger<Starter>();
    // 同步远程数据库脚本到本子
    public static async Task DoTransfer()
    {
        var remoteOtions = new DbContextOptionsBuilder<KScriptSpiderContext>().UseNpgsql("PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=100").Options;
        var remoteFactory = new PooledDbContextFactory<KScriptSpiderContext>(remoteOtions,100);
        var locationOtions = new DbContextOptionsBuilder<KScriptSpiderContext>().UseNpgsql("PORT=5432;DATABASE=k_script_spider;HOST=192.168.0.102;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=100").Options;
        var locationFactory = new PooledDbContextFactory<KScriptSpiderContext>(locationOtions, 100);
        // var taskNewsList = SyncSpiderNewsList(remoteFactory, locationFactory);
        // Task.WhenAll(taskNewsList).GetAwaiter().GetResult();
        // var taskNewsContentOrigin =SyncSpiderNewsContentOrigin(remoteFactory, locationFactory);
        // Task.WhenAll(taskNewsContentOrigin).GetAwaiter().GetResult();
        // var taskNewsContent =SyncSpiderNewsContent(remoteFactory, locationFactory);
        // Task.WhenAll(taskNewsContent).GetAwaiter().GetResult();
        // var taskNewsImage =SyncSpiderNewsImage(remoteFactory,locationFactory);
        // Task.WhenAll(taskNewsImage).GetAwaiter().GetResult();
        // Task.WhenAll(taskNewsList,taskNewsContentOrigin,taskNewsContent,taskNewsImage);
        await SyncSpiderNewsList(remoteFactory, locationFactory);
        await SyncSpiderNewsContentOrigin(remoteFactory, locationFactory);
        await SyncSpiderNewsContent(remoteFactory, locationFactory);
        await SyncSpiderNewsImage(remoteFactory,locationFactory);
    }

    private static async Task SyncSpiderNewsContentOrigin(PooledDbContextFactory<KScriptSpiderContext> remoteFactory,
        PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            const int batchSize = 1000; // 调整批量大小
            List<SpiderNewsContentOrigin> batch = new List<SpiderNewsContentOrigin>(batchSize);
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = !locationContext.SpiderNewsContentOrigins.Any()
                ? 0
                :locationContext.SpiderNewsContentOrigins.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsContentOrigins.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                batch.Add(item);
                if(batch.Count<batchSize) continue;
                locationContext.SpiderNewsContentOrigins.AddRange(batch);
                await locationContext.SaveChangesAsync();
                Console.WriteLine($"SyncSpiderNewsContentOrigin : sync end id : {batch.Last().Id}");
                batch.Clear();
            }
            if (batch.Count != 0)
            {
                locationContext.SpiderNewsContentOrigins.AddRange(batch);
                await locationContext.SaveChangesAsync();
            }

        }
        catch (Exception e)
        {
            Logger.LogError("[SyncSpiderNewsContentOrigin] error :{}",e);
            throw; // TODO 处理异常
        }
    }
    private static async Task SyncSpiderNewsContent(PooledDbContextFactory<KScriptSpiderContext> remoteFactory,
        PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            const int batchSize = 1000; // 调整批量大小
            List<SpiderNewsContent> batch = new List<SpiderNewsContent>(batchSize);
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = !locationContext.SpiderNewsContents.Any()
                ? 0
                :locationContext.SpiderNewsContents.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsContents.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                batch.Add(item);
                if(batch.Count<batchSize) continue;
                locationContext.SpiderNewsContents.AddRange(batch);
                await locationContext.SaveChangesAsync();
                Console.WriteLine($"SyncSpiderNewsContent : sync end id : {batch.Last().Id}");
                batch.Clear();
            }
            if (batch.Count != 0)
            {
                locationContext.SpiderNewsContents.AddRange(batch);
                await locationContext.SaveChangesAsync();
            }

        }
        catch (Exception e)
        {
            Logger.LogError("[SyncSpiderNewsContentOrigin] error :{}",e);
            throw; // TODO 处理异常
        }
    }
    
    
    private static async Task SyncSpiderNewsImage(PooledDbContextFactory<KScriptSpiderContext> remoteFactory,
        PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            const int batchSize = 1000; // 调整批量大小
            List<SpiderNewsImageList> batch = new List<SpiderNewsImageList>(batchSize);
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = !locationContext.SpiderNewsImageLists.Any()
                ? 0
                : locationContext.SpiderNewsImageLists.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsImageLists.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                batch.Add(item);
                if(batch.Count<batchSize) continue;
                locationContext.SpiderNewsImageLists.AddRange(batch);
                await locationContext.SaveChangesAsync();
                Console.WriteLine($"SyncSpiderNewsImage : sync end id : {batch.Last().Id}");
                batch.Clear();
            }
            if (batch.Count != 0)
            {
                locationContext.SpiderNewsImageLists.AddRange(batch);
                await locationContext.SaveChangesAsync();
            }

        }
        catch (Exception e)
        {
            Logger.LogError("[SyncSpiderNewsImage] error :{}",e);
            throw; // TODO 处理异常
        }
    }

    private static async Task SyncSpiderNewsList(PooledDbContextFactory<KScriptSpiderContext> remoteFactory, PooledDbContextFactory<KScriptSpiderContext> locationFactory)
    {
        try
        {
            const int batchSize = 1000; // 调整批量大小
            List<SpiderNewsList> batch = new List<SpiderNewsList>(batchSize);
            await using var remoteContext = await remoteFactory.CreateDbContextAsync();
            await using var locationContext = await locationFactory.CreateDbContextAsync();
            long localMaxId = locationContext.SpiderNewsLists.Max(x => x.Id);
            await foreach (var item in remoteContext.SpiderNewsLists.Where(p => p.Id > localMaxId).OrderBy(p => p.Id).AsAsyncEnumerable()) // 异步流式加载，按ID排序
            {
                batch.Add(item);
                if (batch.Count < batchSize) continue;
                locationContext.SpiderNewsLists.AddRange(batch);
                await locationContext.SaveChangesAsync();
                Console.WriteLine($"SyncSpiderNewsList : sync end id : {batch.Last().Id}");
                batch.Clear();
            }
            if (batch.Count != 0)
            {
                locationContext.SpiderNewsLists.AddRange(batch);
                await locationContext.SaveChangesAsync();
            }

        }
        catch (Exception e)
        {
            Logger.LogError("[SyncSpiderNewsList] error :{}",e);
            throw; // TODO 处理异常
        }
    }
}