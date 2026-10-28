using gAPI.Core.Attributes;
using gAPI.Core.Ids;

namespace gAPI.Test.Shared.Private;

[GenerateBus]
public interface IBus1
{
    Task StartServiceTests(SessionId sessionId, CancellationToken ct);

    void SendAndForget(string message, CancellationToken ct);
    void SendAndForgetWithList(IAsyncEnumerable<string> test, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task SendWithList(IAsyncEnumerable<string> test, CancellationToken ct);
    Task<string> InvokeTask(string name, CancellationToken ct);
    Task<string> InvokeTaskWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    IAsyncEnumerable<string> InvokeList(string name, CancellationToken ct);
    IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, CancellationToken ct);

    IAsyncEnumerable<double> Ping(CancellationToken ct);
    Task Pong(CancellationToken ct);
}
