using gAPI.Core.Attributes;

namespace gAPI.Example.Shared.WssPublic;

[GenerateHub]
public interface IWssHub
{
    void SendAndForget(string message, CancellationToken ct);
    //void SendAndForgetWithList(IAsyncEnumerable<string> test, CancellationToken ct);
    Task Send(string message, CancellationToken ct);
    Task SendWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    IAsyncEnumerable<string> InvokeList(string name, CancellationToken ct);
    IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, CancellationToken ct);
    void Ping(string v);
    void Pong(string v);
}
