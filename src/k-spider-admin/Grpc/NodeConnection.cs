using System.Globalization;
using Grpc.Core;
using KSpider.Admin.Tasks.Abstractions;
using GrpcScheduler = KSpider.Grpc.Scheduler;

namespace KSpider.Admin.Grpc;

/// <summary>
///     一条已建立的节点双向流；下行写入按连接串行化。
/// </summary>
/// <remarks>
///     <para>
///         同一个 gRPC 流不支持并发 WriteAsync；所有下行消息（派发/取消/探测）都必须经过
///         <see cref="TrySendAsync" /> 的写锁。
///     </para>
///     <para>
///         心跳字段使用 Interlocked 更新，读侧拿到的 Snapshot 是一致性近似值；它只服务调度
///         负载判断，不要求强一致。
///     </para>
/// </remarks>
public sealed class NodeConnection : IDisposable
{
    private readonly IServerStreamWriter<GrpcScheduler.AdminMessage> _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CancellationTokenSource _closed = new();
    private readonly TimeProvider _timeProvider;
    private long _lastSeenUtcTicks;
    private int _runningTasks;

    /// <summary>
    ///     创建连接并初始化最后活跃时间。
    /// </summary>
    /// <param name="nodeId">节点标识。</param>
    /// <param name="nodeType">节点类型。</param>
    /// <param name="deviceJson">节点自报硬件 JSON。</param>
    /// <param name="writer">gRPC 下行写入器。</param>
    /// <param name="timeProvider">可测试时钟。</param>
    public NodeConnection(
        Guid nodeId,
        string nodeType,
        string? deviceJson,
        IServerStreamWriter<GrpcScheduler.AdminMessage> writer,
        TimeProvider timeProvider)
    {
        NodeId = nodeId;
        NodeType = nodeType;
        DeviceJson = deviceJson;
        _writer = writer;
        _timeProvider = timeProvider;
        Touch(0);
    }

    /// <summary>节点标识。</summary>
    public Guid NodeId { get; }

    /// <summary>节点类型：crawler / process。</summary>
    public string NodeType { get; }

    /// <summary>节点自报硬件 JSON。</summary>
    public string? DeviceJson { get; }

    /// <summary>连接是否已关闭。</summary>
    public bool IsClosed => _closed.IsCancellationRequested;

    /// <summary>
    ///     更新心跳时间与在跑任务数；负数被钳制为 0。
    /// </summary>
    /// <param name="runningTasks">节点自报在跑任务数。</param>
    public void Touch(int runningTasks)
    {
        Interlocked.Exchange(ref _runningTasks, Math.Max(0, runningTasks));
        Interlocked.Exchange(ref _lastSeenUtcTicks, _timeProvider.GetUtcNow().UtcTicks);
    }

    /// <summary>
    ///     生成节点快照。
    /// </summary>
    public NodeSnapshot Snapshot()
    {
        var ticks = Interlocked.Read(ref _lastSeenUtcTicks);
        return new NodeSnapshot(
            NodeId,
            NodeType,
            Volatile.Read(ref _runningTasks),
            new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc)));
    }

    /// <summary>
    ///     串行写入一条下行消息。
    /// </summary>
    /// <remarks>
    ///     连接已关闭、流写入失败或写入器失效时返回 false；取消异常原样透传，由调用方决定
    ///     是超时还是宿主关闭。
    /// </remarks>
    /// <param name="message">下行消息。</param>
    /// <param name="cancellationToken">写入取消令牌。</param>
    /// <returns>写入成功返回 true。</returns>
    public async Task<bool> TrySendAsync(
        GrpcScheduler.AdminMessage message,
        CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (IsClosed)
            {
                return false;
            }

            await _writer.WriteAsync(message, cancellationToken);
            return true;
        }
        catch (RpcException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    ///     标记连接关闭；已开始的写入由取消令牌或流异常结束。
    /// </summary>
    public void Close()
    {
        if (!_closed.IsCancellationRequested)
        {
            _closed.Cancel();
        }
    }

    /// <summary>
    ///     释放连接持有的同步与取消资源。
    /// </summary>
    public void Dispose()
    {
        Close();
        _closed.Dispose();
        _writeLock.Dispose();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{NodeId} ({NodeType})");
    }
}
