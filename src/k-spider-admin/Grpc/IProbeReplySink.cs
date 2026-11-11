namespace KSpider.Admin.Grpc;

/// <summary>
///     接收节点探测应答的入口。
/// </summary>
public interface IProbeReplySink
{
    /// <summary>
    ///     用 request_id 唤醒等待中的探测。
    /// </summary>
    /// <param name="requestId">探测关联 ID。</param>
    /// <param name="alive">节点是否存活。</param>
    void Complete(string requestId, bool alive);
}
