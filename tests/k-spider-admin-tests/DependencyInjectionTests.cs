using KSpider.Admin.Api.Queries;
using KSpider.Admin.Grpc;
using KSpider.Admin.Hosting;
using KSpider.Admin.Reconciliation;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Postgres;
using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KSpider.Admin.Tests;

[TestClass]
public sealed class DependencyInjectionTests
{
    [TestMethod]
    public async Task AdminServiceGraphResolves()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Host=localhost;Database=k_spider;Username=test;Password=test",
        });
        builder.Services.AddAdminServices(builder.Configuration);

        await using var app = builder.Build();

        Assert.IsNotNull(app.Services.GetRequiredService<IAdminTaskService>());
        Assert.IsNotNull(app.Services.GetRequiredService<IReconciliationService>());
        Assert.IsNotNull(app.Services.GetRequiredService<INodeDirectory>());
        Assert.IsNotNull(app.Services.GetRequiredService<INodeDispatcher>());
        Assert.IsNotNull(app.Services.GetRequiredService<IProbeService>());
        Assert.IsNotNull(app.Services.GetRequiredService<IUserRepository>());
        Assert.IsNotNull(app.Services.GetRequiredService<ChannelQueryService>());
        Assert.IsTrue(app.Services.GetRequiredService<IEnumerable<IHostedService>>()
            .Any(service => service is SchedulerLoop));
        Assert.IsNotNull(app.Services.GetRequiredService<NodeConnectionRegistry>());
        Assert.IsNotNull(app.Services.GetRequiredService<SchedulerGrpcService>());
        Assert.IsNotNull(app.Services.GetRequiredService<DatabaseHealthCheck>());
    }
}
