using KSpider.Config;
using KSpider.Data;
using KSpider.Web.Api;
using KSpider.Web.Query;
using KSpider.Web.Replay;
using Microsoft.Extensions.Configuration;

namespace KSpider.Web;

/// <summary>
///     Web 控制台服务端 : 只读查询新闻 / 快讯 / 系统状态 + 聚合分析 , 任务控制走指令表 ( 异步受理 )。
///     与爬虫进程完全独立部署 , 只共享同一个 PostgreSQL。
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        // Host 默认环境为 Production , 本地不设置时默认 Development ( 与主程序同款 )
        System.Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT",
            System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            // 配置根固定为程序目录 : 与主程序同坑 , 从仓库根 dotnet run 会静默找不到配置
            ContentRootPath = AppContext.BaseDirectory
        });
        // K_SPIDER__ 前缀环境变量覆盖 ( 与主程序同款 )
        builder.Configuration.AddEnvironmentVariables("K_SPIDER__");

        builder.Services.Configure<DatabaseOptions>(
            builder.Configuration.GetSection(DatabaseOptions.SectionName));
        builder.Services.AddSingleton<Pg>();
        builder.Services.AddSingleton<SpiderNewsDao>();
        builder.Services.AddSingleton<SystemStatusDao>();
        builder.Services.AddSingleton<StatusQueryService>();
        builder.Services.AddSingleton<NewsQueryService>();
        builder.Services.AddSingleton<FlashQueryService>();
        builder.Services.AddSingleton<AnalysisQueryService>();
        builder.Services.AddSingleton<JobCommandService>();
        builder.Services.AddSingleton<OriginReplayService>();

        // 开发期 OpenAPI 文档 ( /openapi/v1.json ) , 生产不暴露
        builder.Services.AddOpenApi();

        var app = builder.Build();
        // 幂等补齐系统表 ( 含重放任务表 ) : Web 可能先于主程序部署在同一个库上跑
        app.Services.GetRequiredService<Pg>().EnsureSystemDbObjects();
        if (app.Environment.IsDevelopment()) app.MapOpenApi();

        // 前端为构建产物静态文件 ( scripts/build-web.sh 拷入 wwwroot ) ; 未构建时只提供 API
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapSpiderApis();

        // SPA 路由回退 : 刷新 /jobs 这类前端路由能回到 index.html
        // 探测用 WebRootFileProvider 而不是物理路径 —— 开发模式静态资源经 staticwebassets 清单直接伺服自项目目录 ,
        // bin 下没有 wwwroot 物理目录 ; 发布产物则两者一致
        var indexExists = app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists;
        if (indexExists)
            app.MapFallbackToFile("index.html");
        else
            app.Logger.LogWarning("前端产物缺失 ( wwwroot/index.html ) , 当前仅提供 API ; 构建前端请执行 scripts/build-web.sh");

        await app.RunAsync();
    }
}
