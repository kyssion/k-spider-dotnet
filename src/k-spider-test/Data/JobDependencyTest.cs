using KSpider.Config;
using KSpider.Job;
using KSpider.Spider.News.Report.Eastmoney;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using Quartz.Impl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Test.Data;

/// <summary>
///     Job 依赖注入防回归 : DI 缺注册构建期不报错 , 只有 Quartz 运行时实例化任务才炸
///     ( 2026-09-30 ResearchReportJob 漏注册 SpiderResearchReportDao , 启动后每个触发周期报
///     "Unable to resolve service" , 其它任务照常跑 , 从日志外很难第一眼定位 )。
///     本用例把 Program.AddSpiderData 的注册原样装配 , 逐个 SpiderJob 子类试构造 ,
///     任何一个构造参数解析不出来立即失败 —— 新增 Job 或新增构造依赖时此处先行兜底。
/// </summary>
[TestClass]
public class JobDependencyTest
{
    [TestMethod]
    public void AllSpiderJobsConstructorDependenciesAreResolvable()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<DatabaseOptions>(_ => { });
        services.AddLogging();
        // NodeStateJob 的调度器依赖 , 生产由 AddQuartz 注册 , 这里用标准实现等价替代
        services.TryAddSingleton<ISchedulerFactory, StdSchedulerFactory>();
        KSpider.Program.AddSpiderData(services);
        using var provider = services.BuildServiceProvider();

        var jobTypes = typeof(SpiderJob).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(SpiderJob)) && !type.IsAbstract)
            .ToList();
        Assert.IsTrue(jobTypes.Count >= 7, $"SpiderJob 子类应有 7 个以上 ( 当前扫描到 {jobTypes.Count} )");

        foreach (var jobType in jobTypes)
        {
            // 任一构造参数缺 DI 注册 , 这里抛 InvalidOperationException 使用例失败
            var instance = ActivatorUtilities.CreateInstance(provider, jobType);
            Assert.IsInstanceOfType(instance, jobType, $"{jobType.Name} 构造产物类型不符");
        }
    }

    [TestMethod]
    public void ResearchReportDaoIsRegistered()
    {
        // 直锁本次踩坑的具体缺失 , 语义直白便于排障时对照
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<DatabaseOptions>(_ => { });
        services.AddLogging();
        KSpider.Program.AddSpiderData(services);
        using var provider = services.BuildServiceProvider();
        Assert.IsNotNull(provider.GetService<KSpider.Data.SpiderResearchReportDao>());
        Assert.IsNotNull(provider.GetService<KSpider.Data.SpiderNewsBatchDao>());
        Assert.IsNotNull(provider.GetService<KSpider.Data.SpiderNewsDao>());
        Assert.IsNotNull(provider.GetService<KSpider.Data.SystemStatusDao>());
        Assert.IsNotNull(provider.GetService<KSpider.Data.Pg>());
    }
}
