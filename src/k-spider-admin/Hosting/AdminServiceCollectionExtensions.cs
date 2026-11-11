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
        // 绑定并校验强类型配置；配置缺失或越界时在启动阶段失败，不等首个请求才暴露。
        AddOptions(services, configuration);

        // 统一时间源；调度、对账和查询测试可替换为可控时钟。
        services.AddSingleton(TimeProvider.System);

        // 数据访问基础设施：每次仓储操作独立创建连接，避免连接/事务跨请求共享。
        services.AddSingleton<IAdminDbConnectionFactory, PostgresConnectionFactory>();
        // 任务表：状态机条件更新、派发 token、失败计数与终态查询。
        services.AddSingleton<ITaskRepository, PostgresTaskRepository>();
        // 节点表：节点上下线、心跳镜像与按节点查询任务。
        services.AddSingleton<ISysNodeRepository, PostgresNodeRepository>();
        // 渠道与分类：调度读取 active 渠道，命令接口写启停状态。
        services.AddSingleton<IChannelRepository, PostgresChannelRepository>();
        // 认证账号：登录校验、密码哈希、账号启停与展示名维护。
        services.AddSingleton<IUserRepository, PostgresUserRepository>();
        // 总览聚合：渠道/任务/节点状态计数与最近失败任务。
        services.AddSingleton<IOverviewRepository, PostgresOverviewRepository>();
        // 原始数据只读：列表不读 raw，详情才按需读取大字段。
        services.AddSingleton<IRawDataRepository, PostgresRawDataRepository>();

        // 任务生命周期入口：创建、推送、取消、重派、放弃与终态通知。
        services.AddSingleton<IAdminTaskService, AdminTaskService>();
        // 同渠道互斥守卫：阻止同一渠道同时存在两个非终态任务。
        services.AddSingleton<ChannelRoundGuard>();
        // 把 ChannelRoundGuard 暴露为终态观察者，任务终态时立即释放渠道互斥。
        services.AddSingleton<ITaskTerminalObserver>(provider =>
            provider.GetRequiredService<ChannelRoundGuard>());
        // 渠道运行时间缓存：保存每个渠道最近一次任务创建时间，避免每个 tick 重复查 JSONB。
        services.AddSingleton<ChannelRuntimeCache>();
        // 调度到期判定：解析 channel.schedule 并计算下一轮是否该触发。
        services.AddSingleton<ChannelScheduleEvaluator>();
        // 任务包生成器：把渠道配置转换为轮次 TaskPlan（当前调度使用列表轮）。
        services.AddSingleton<TaskPlanner>();
        // 节点选择器：从调用方传入的在线 crawler 候选中挑选运行任务数最少的节点。
        services.AddSingleton<NodeSelector>();
        // 渠道命令服务：暂停/恢复渠道，仅改 channel.status，不隐式取消在跑任务。
        services.AddSingleton<ChannelCommandService>();

        // 对账服务：扫描卡住/失联任务，按策略重派、放弃或等待节点恢复。
        services.AddSingleton<IReconciliationService, ReconciliationService>();

        // 节点连接注册表：维护 gRPC 双向流的在线节点、心跳和连接代次。
        services.AddSingleton<NodeConnectionRegistry>();
        // 把注册表暴露为只读节点目录，供调度/查询按在线状态选节点。
        services.AddSingleton<INodeDirectory>(provider =>
            provider.GetRequiredService<NodeConnectionRegistry>());
        // 下行派发器：向节点发送任务、取消与探测请求（admin → crawler）。
        services.AddSingleton<INodeDispatcher, GrpcNodeDispatcher>();
        // 探测服务：对 pushed/running 任务发起节点侧探活，结果用于对账判定。
        services.AddSingleton<GrpcProbeService>();
        // 把探测服务暴露为客户端接口，避免调用方依赖具体 gRPC 实现。
        services.AddSingleton<IProbeService>(provider =>
            provider.GetRequiredService<GrpcProbeService>());
        // 接收节点探测回包；与探测请求共用同一单例维护等待中的请求。
        services.AddSingleton<IProbeReplySink>(provider =>
            provider.GetRequiredService<GrpcProbeService>());
        // 节点上行消息处理器：处理 task_result、进度与探测回包。
        services.AddSingleton<NodeMessageHandler>();
        // 调度 gRPC 服务：暴露 Connect 双向流，接收节点注册/心跳/任务结果。
        services.AddSingleton<SchedulerGrpcService>();
        // gRPC 运行时基础设施（消息序列化、服务注册、HTTP/2 端点支持）。
        services.AddGrpc();

        // REST 查询服务：控制器只做 HTTP 映射，查询与分页规则留在服务层。
        services.AddSingleton<ChannelQueryService>();
        services.AddSingleton<TaskQueryService>();
        services.AddSingleton<NodeQueryService>();
        services.AddSingleton<OverviewQueryService>();
        services.AddSingleton<RawDataQueryService>();

        // 后台循环：调度新轮次、对账卡住任务、判死节点、清理过期终态任务。
        services.AddHostedService<SchedulerLoop>();
        services.AddHostedService<ReconciliationLoop>();
        services.AddHostedService<NodeHealthMonitor>();
        services.AddHostedService<TaskCleanupLoop>();

        // 认证与账号：Cookie 登录、授权策略、CSRF、Data Protection 与账号服务。
        services.AddAdminAuth(configuration);
        // 健康检查：验证数据库连接是否可用，供 /api/health 使用。
        services.AddSingleton<DatabaseHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    /// <summary>
    ///     绑定 admin 的强类型配置节，并在启动阶段执行数据注解校验。
    /// </summary>
    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        // 数据库连接串；空值会阻止 admin 启动。
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 调度循环 tick 与默认任务包批量上限。
        services.AddOptions<SchedulerOptions>()
            .Bind(configuration.GetSection(SchedulerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 对账扫描、探活、心跳判死与终态清理的运行参数。
        services.AddOptions<ReconciliationOptions>()
            .Bind(configuration.GetSection(ReconciliationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 节点 gRPC 派发/取消/探测超时等下行参数。
        services.AddOptions<GrpcOptions>()
            .Bind(configuration.GetSection(GrpcOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // REST 与 gRPC 的监听地址、端口；Kestrel 配置阶段会校验两端口不能相同。
        services.AddOptions<AdminEndpointsOptions>()
            .Bind(configuration.GetSection(AdminEndpointsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
