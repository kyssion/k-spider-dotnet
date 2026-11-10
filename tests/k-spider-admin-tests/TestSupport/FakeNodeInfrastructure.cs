using KSpider.Admin.Tasks;
using KSpider.Admin.Tasks.Abstractions;

namespace KSpider.Admin.Tests.TestSupport;

internal sealed class FakeNodeDispatcher : INodeDispatcher
{
    public List<DispatchRequest> Dispatches { get; } = [];

    public List<CancelRequest> Cancels { get; } = [];

    public bool DispatchResult { get; set; } = true;

    public bool CancelResult { get; set; } = true;

    public Task<bool> DispatchAsync(
        Guid nodeId,
        DispatchRequest request,
        CancellationToken cancellationToken)
    {
        Dispatches.Add(request);
        return Task.FromResult(DispatchResult);
    }

    public Task<bool> CancelAsync(
        Guid nodeId,
        CancelRequest request,
        CancellationToken cancellationToken)
    {
        Cancels.Add(request);
        return Task.FromResult(CancelResult);
    }

    public Task<bool> ProbeAsync(
        Guid nodeId,
        string requestId,
        long taskId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}

internal sealed class FakeProbeService : IProbeService
{
    public bool Alive { get; set; } = true;

    public Task<ProbeResult> ProbeAsync(
        Guid nodeId,
        long taskId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new ProbeResult(Alive));
    }
}

internal sealed class FakeNodeDirectory : INodeDirectory
{
    private readonly Dictionary<Guid, NodeSnapshot> _nodes = new();

    public void Add(NodeSnapshot node)
    {
        _nodes[node.NodeId] = node;
    }

    public IReadOnlyList<NodeSnapshot> GetOnlineNodes(string nodeType)
    {
        return _nodes.Values
            .Where(node => node.NodeType == nodeType)
            .ToList();
    }

    public bool TryGet(Guid nodeId, out NodeSnapshot snapshot)
    {
        return _nodes.TryGetValue(nodeId, out snapshot!);
    }

    public void MarkOffline(Guid nodeId)
    {
        _nodes.Remove(nodeId);
    }
}

internal sealed class RecordingTerminalObserver : ITaskTerminalObserver
{
    public List<(long TaskId, long ChannelId)> Notifications { get; } = [];

    public ValueTask OnTaskTerminalAsync(long taskId, long channelId, CancellationToken cancellationToken)
    {
        Notifications.Add((taskId, channelId));
        return ValueTask.CompletedTask;
    }
}
