using gAPI.Core.Attributes;

namespace gAPI.Example.Shared.WssPublic;

[GenerateApi]
public interface IWssApi
{
    Task StartServerTests(double time, CancellationToken ct);

    void SendAndForget(string message, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task SendWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    Task<string> InvokeTask(string name, CancellationToken ct);
    Task<string> InvokeTaskWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    IAsyncEnumerable<string> InvokeList(string name, CancellationToken ct);
    IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    void Ping(string v);
    void Pong(string v);
    IAsyncEnumerable<ReadOnlyMemory<byte>> VideoFeedTest(CancellationToken ct);
}
