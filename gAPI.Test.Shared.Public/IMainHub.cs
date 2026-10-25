using gAPI.Core.Attributes;

namespace gAPI.Test.Shared.Public;

[GenerateHub]
public interface IMainHub
{
    void SendAndForget(string message, CancellationToken ct);
    void SendAndForgetWithList(IAsyncEnumerable<string> test, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task SendWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    IAsyncEnumerable<string> InvokeList(string name, CancellationToken ct);
    IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, CancellationToken ct);
}
