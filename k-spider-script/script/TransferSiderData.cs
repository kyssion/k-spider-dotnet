using System.Threading.Channels;
// using k_spirder_script.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace k_spirder_script.script;

public class TransferSiderData
{
    // 同步远程数据库脚本到本子
    // public static void DoTransfer()
    // {
    //     var options = new DbContextOptionsBuilder<KScriptSpiderContext>().UseNpgsql("PORT=5432;DATABASE=k_script_spider;HOST=39.100.86.193;PASSWORD=Javarustc++11.;USER ID=spider ;Include Error Detail=true;Pooling=true;MaxPoolSize=100").Options;
    //     var factory = new PooledDbContextFactory<KScriptSpiderContext>(options,100);
    //     using (var context = factory.CreateDbContext())
    //     {
    //         var ans = context.SpiderNewsLists.Where(x => x.Id == 1).ToList();
    //         Console.WriteLine(ans);
    //     }
    // }
}