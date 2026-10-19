using gAPI.Core.Attributes;

namespace gAPI.Test.AutoBus.Shared;

[GenerateBus]
public interface IService1
{
    void WriteLine(string message, CancellationToken ct);
    Task WriteLineAsync(string message, CancellationToken ct);
    IAsyncEnumerable<string> Get(string name, CancellationToken ct);
    Task Test(IAsyncEnumerable<string> test, CancellationToken ct);
    IAsyncEnumerable<double> Ping(CancellationToken ct);
    Task Pong(CancellationToken ct);
}
