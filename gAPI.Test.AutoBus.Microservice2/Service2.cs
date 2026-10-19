using gAPI.Generated;
using gAPI.Test.AutoBus.Shared;
using Microsoft.Extensions.Hosting;

namespace gAPI.Test.AutoBus.Microservice2;

public class Service2(
    IBusContext busContext,
    IBusConnection busConnection)
    : IService2, IHostedService
{
    public async IAsyncEnumerable<string> Get(string name)
    {
        yield return "Hoi from 2";

    }
    public async Task StartAsync(CancellationToken ct)
    {
        await busConnection.SubscribeAsync(this, ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await busConnection.UnsubscribeAsync(this, ct);
    }

    public Task Ping(CancellationToken ct)
    {
        return busContext.Service1.ToAny().Pong(ct);
    }

    public Task Test(IAsyncEnumerable<string> test)
    {
        throw new NotImplementedException();
    }

    public void WriteLine(string message)
    {
        Console.WriteLine($"From Microservice 2: {message}");
    }

    public async Task WriteLineAsync(string message)
    {
        Console.WriteLine($"From Microservice 2: {message}");
    }
}
