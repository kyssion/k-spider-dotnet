using KSpider.Admin.Api.Queries;
using KSpider.Admin.Auth;
using KSpider.Admin.Grpc;
using KSpider.Admin.Reconciliation;
using KSpider.Admin.Repository;
using KSpider.Admin.Repository.Abstractions;
using KSpider.Admin.Repository.Postgres;
using KSpider.Admin.Scheduler;
using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;
using KSpider.Admin.WebHosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KSpider.Admin.Hosting;

/// <summary>
///     admin 组合根：注册 Options、仓储、领域服务、gRPC、认证、健康检查与后台循环。
/// </summary>
public static class AdminServiceCollectionExtensions
{
    /// <summary>
    ///     注册 admin 全部服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置。</param>
    public static IServiceCollection AddAdminServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddOptions(services, configuration);

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IAdminDbConnectionFactory, PostgresConnectionFactory>();
        services.AddSingleton<ITaskRepository, PostgresTaskRepository>();
        services.AddSingleton<ISysNodeRepository, PostgresNodeRepository>();
        services.AddSingleton<IChannelRepository, PostgresChannelRepository>();
        services.AddSingleton<IUserRepository, PostgresUserRepository>();

        services.AddSingleton<IAdminTaskService, AdminTaskService>();
        services.AddSingleton<ChannelRoundGuard>();
        services.AddSingleton<ITaskTerminalObserver>(provider =>
            provider.GetRequiredService<ChannelRoundGuard>());
        services.AddSingleton<ChannelRuntimeCache>();
        services.AddSingleton<ChannelScheduleEvaluator>();
        services.AddSingleton<TaskPlanner>();
        services.AddSingleton<NodeSelector>();
        services.AddSingleton<ChannelCommandService>();

        services.AddSingleton<IReconciliationService, ReconciliationService>();

        services.AddSingleton<NodeConnectionRegistry>();
        services.AddSingleton<INodeDirectory>(provider =>
            provider.GetRequiredService<NodeConnectionRegistry>());
        services.AddSingleton<INodeDispatcher, GrpcNodeDispatcher>();
        services.AddSingleton<GrpcProbeService>();
        services.AddSingleton<IProbeService>(provider =>
            provider.GetRequiredService<GrpcProbeService>());
        services.AddSingleton<IProbeReplySink>(provider =>
            provider.GetRequiredService<GrpcProbeService>());
        services.AddSingleton<NodeMessageHandler>();
        services.AddSingleton<SchedulerGrpcService>();
        services.AddGrpc();

        services.AddSingleton<ChannelQueryService>();
        services.AddSingleton<TaskQueryService>();
        services.AddSingleton<NodeQueryService>();

        services.AddHostedService<SchedulerLoop>();
        services.AddHostedService<ReconciliationLoop>();
        services.AddHostedService<NodeHealthMonitor>();
        services.AddHostedService<TaskCleanupLoop>();

        services.AddAdminAuth(configuration);
        services.AddSingleton<DatabaseHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SchedulerOptions>()
            .Bind(configuration.GetSection(SchedulerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ReconciliationOptions>()
            .Bind(configuration.GetSection(ReconciliationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GrpcOptions>()
            .Bind(configuration.GetSection(GrpcOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AdminEndpointsOptions>()
            .Bind(configuration.GetSection(AdminEndpointsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
