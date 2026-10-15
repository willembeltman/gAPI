using gAPI.Core.Attributes;

namespace gAPI.Test.AutoBus.Shared;

[GenerateBus]
public interface IService2
{
    void WriteLine(string message);
    Task WriteLineAsync(string message);
    IAsyncEnumerable<string> Get(string name);
    Task Test(IAsyncEnumerable<string> test);
}
