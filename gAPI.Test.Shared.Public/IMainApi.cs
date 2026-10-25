using gAPI.Core.Attributes;

namespace gAPI.Test.Shared.Public;

[GenerateApi]
public interface IMainApi
{
    Task StartServerTests(double time, CancellationToken ct);

    void SendAndForget(string message, CancellationToken ct);
    void SendAndForgetWithList(IAsyncEnumerable<string> test, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task SendWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    Task<string> InvokeTask(string name, CancellationToken ct);
    Task<string> InvokeTaskWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    IAsyncEnumerable<string> InvokeList(string name, CancellationToken ct);
    IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, CancellationToken ct);
}
