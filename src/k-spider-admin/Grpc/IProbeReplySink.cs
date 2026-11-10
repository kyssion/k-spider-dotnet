namespace KSpider.Admin.Grpc;

public interface IProbeReplySink
{
    void Complete(string requestId, bool alive);
}
