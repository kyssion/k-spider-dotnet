using Microsoft.Extensions.Logging;

namespace KSpider.Admin.Common.Logging;

/// <summary>
///     admin 集中日志事件；使用源生成器，避免热路径上不必要的参数装箱与字符串分配。
/// </summary>
public static partial class AdminLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "创建任务包 task_id={TaskId} channel_id={ChannelId} node_id={NodeId} task_type={TaskType} token={Token}")]
    public static partial void TaskCreated(
        ILogger logger,
        long taskId,
        long channelId,
        Guid nodeId,
        string taskType,
        string token);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "任务状态更新 task_id={TaskId} channel_id={ChannelId} status={Status} reason={Reason}")]
    public static partial void TaskTransitionApplied(
        ILogger logger,
        long taskId,
        long channelId,
        string status,
        string? reason);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "忽略过期派发的终态回报 task_id={TaskId} token={Token}")]
    public static partial void TaskStaleResult(ILogger logger, long taskId, string token);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning,
        Message = "节点回报终态与数据库状态不一致 task_id={TaskId} node_status={NodeStatus} db_status={DbStatus}")]
    public static partial void TaskResultMismatch(
        ILogger logger,
        long taskId,
        string nodeStatus,
        string dbStatus);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning,
        Message = "终态任务缺少可解析的 channel_id task_id={TaskId}")]
    public static partial void TaskDetailMissing(ILogger logger, long taskId);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Error,
        Message = "终态观察者执行失败 task_id={TaskId} channel_id={ChannelId} observer={Observer}")]
    public static partial void TaskObserverFailed(
        ILogger logger,
        Exception exception,
        long taskId,
        long channelId,
        string observer);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Information,
        Message = "任务重派 task_id={TaskId} channel_id={ChannelId} node_id={NodeId} fail_count={FailCount} token={Token}")]
    public static partial void ReconciliationRedispatch(
        ILogger logger,
        long taskId,
        long channelId,
        Guid nodeId,
        int failCount,
        string token);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Warning,
        Message = "任务失败放弃 task_id={TaskId} channel_id={ChannelId} reason={Reason}")]
    public static partial void ReconciliationAbandoned(
        ILogger logger,
        long taskId,
        long channelId,
        string reason);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Error,
        Message = "周期循环单轮执行失败，将在下一轮重试：{Service}")]
    public static partial void LoopFailed(ILogger logger, Exception exception, string service);


    [LoggerMessage(EventId = 1009, Level = LogLevel.Warning,
        Message = "取消通知发送失败 task_id={TaskId} node_id={NodeId}")]
    public static partial void TaskCancelSendFailed(ILogger logger, long taskId, Guid nodeId);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information,
        Message = "节点已连接 node_id={NodeId} node_type={NodeType}")]
    public static partial void NodeConnected(ILogger logger, Guid nodeId, string nodeType);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Information,
        Message = "节点已断开 node_id={NodeId}")]
    public static partial void NodeDisconnected(ILogger logger, Guid nodeId);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning,
        Message = "节点心跳超时，标记离线 node_id={NodeId} last_seen={LastSeen}")]
    public static partial void NodeOffline(ILogger logger, Guid nodeId, DateTimeOffset lastSeen);


    [LoggerMessage(EventId = 1104, Level = LogLevel.Warning,
        Message = "拒绝节点注册 reason={Reason}")]
    public static partial void NodeRegisterRejected(ILogger logger, string reason);

    [LoggerMessage(EventId = 1105, Level = LogLevel.Warning,
        Message = "节点终态消息无效 task_id={TaskId} status={Status}")]
    public static partial void NodeResultInvalid(ILogger logger, string taskId, string status);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Warning,
        Message = "下行写入失败 node_id={NodeId} message_type={MessageType}")]
    public static partial void GrpcSendFailed(ILogger logger, Guid nodeId, string messageType);


    [LoggerMessage(EventId = 1106, Level = LogLevel.Error,
        Message = "节点断开后写 offline 失败 node_id={NodeId}")]
    public static partial void NodeOfflineWriteFailed(ILogger logger, Exception exception, Guid nodeId);


    [LoggerMessage(EventId = 1107, Level = LogLevel.Error,
        Message = "处理节点消息失败 node_id={NodeId}")]
    public static partial void NodeMessageFailed(ILogger logger, Exception exception, Guid nodeId);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Warning,
        Message = "任务派发失败 task_id={TaskId} node_id={NodeId} reason={Reason}")]
    public static partial void DispatchFailed(ILogger logger, long taskId, Guid nodeId, string reason);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Warning,
        Message = "任务探测失败 task_id={TaskId} node_id={NodeId}")]
    public static partial void ProbeFailed(ILogger logger, long taskId, Guid nodeId);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Debug,
        Message = "调度跳过 channel_id={ChannelId} reason={Reason}")]
    public static partial void SchedulerSkipped(ILogger logger, long channelId, string reason);


    [LoggerMessage(EventId = 1302, Level = LogLevel.Warning,
        Message = "渠道 schedule 无法解析 channel_id={ChannelId} reason={Reason}")]
    public static partial void ScheduleInvalid(ILogger logger, long channelId, string reason);

    [LoggerMessage(EventId = 1303, Level = LogLevel.Warning,
        Message = "同渠道互斥恢复冲突 channel_id={ChannelId} existing_task={ExistingTaskId} ignored_task={IgnoredTaskId}")]
    public static partial void GuardRestoreConflict(
        ILogger logger,
        long channelId,
        long existingTaskId,
        long ignoredTaskId);

    [LoggerMessage(EventId = 1304, Level = LogLevel.Error,
        Message = "任务缺少 dispatch_token task_id={TaskId}")]
    public static partial void TaskTokenMissing(ILogger logger, long taskId);


    [LoggerMessage(EventId = 1305, Level = LogLevel.Information,
        Message = "清理完成 terminal_tasks={Tasks} offline_nodes={Nodes}")]
    public static partial void CleanupCompleted(ILogger logger, int tasks, int nodes);

    [LoggerMessage(EventId = 1306, Level = LogLevel.Warning,
        Message = "对账无可用节点，保留任务等待下一轮 task_id={TaskId} channel_id={ChannelId} reason={Reason}")]
    public static partial void ReconciliationNoNode(
        ILogger logger,
        long taskId,
        long channelId,
        string reason);


    [LoggerMessage(EventId = 1307, Level = LogLevel.Information,
        Message = "渠道状态变更 channel_id={ChannelId} status={Status} operator_id={OperatorId}")]
    public static partial void ChannelStatusChanged(
        ILogger logger,
        long channelId,
        string status,
        long operatorId);

    [LoggerMessage(EventId = 9001, Level = LogLevel.Error,
        Message = "API 未处理异常 method={Method} path={Path}")]
    public static partial void ApiUnhandledException(
        ILogger logger,
        Exception exception,
        string method,
        string path);

    [LoggerMessage(EventId = 9002, Level = LogLevel.Error,
        Message = "API 数据访问异常 method={Method} path={Path}")]
    public static partial void ApiDatabaseException(
        ILogger logger,
        Exception exception,
        string method,
        string path);

    [LoggerMessage(EventId = 1401, Level = LogLevel.Information,
        Message = "登录成功 user_id={UserId} username={Username}")]
    public static partial void AuthLoginSucceeded(ILogger logger, long userId, string username);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Warning,
        Message = "登录失败 username={Username} reason={Reason}")]
    public static partial void AuthLoginFailed(ILogger logger, string username, string reason);

    [LoggerMessage(EventId = 1403, Level = LogLevel.Information,
        Message = "账号变更 operator_id={OperatorId} target_id={TargetId} action={Action}")]
    public static partial void AuthAccountChanged(ILogger logger, long operatorId, long targetId, string action);
}
