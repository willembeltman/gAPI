using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Example.Shared.Private;

[GeneratePlane]
public interface IPlane1
{
    Task StartServiceTests(SessionId sessionId, string to, CancellationToken ct);

    void SendAndForget(string message, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task SendWithList(IAsyncEnumerable<string> test, CancellationToken ct);
    Task<string> InvokeTask(string name, CancellationToken ct);
    Task<string> InvokeTaskWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    IAsyncEnumerable<string> InvokeList(string name, CancellationToken ct);
    IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, CancellationToken ct);

    IAsyncEnumerable<double> PingEnumerable(CancellationToken ct);
    Task PongTask(CancellationToken ct);
    void Ping(string v);
}
