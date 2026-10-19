using gAPI.Generated;
using gAPI.Test.AutoBus.Shared;
using Microsoft.Extensions.Hosting;

namespace gAPI.Test.AutoBus.MicroService1;

public class Service1(
    IBusConnection busConnection)
    : IService1
    , IHostedService
{

    public Task StartAsync(CancellationToken ct)
    {
        return busConnection.SubscribeAsync(this, ct);
    }

    public  Task StopAsync(CancellationToken ct)
    {
        return busConnection.UnsubscribeAsync(this, ct);
    }

    public void WriteLine(string message, CancellationToken ct)
    {
        Console.WriteLine($"From Microservice 1: {message}");
    }

    public async Task WriteLineAsync(string message, CancellationToken ct)
    {
        Console.WriteLine($"From Microservice 1: {message}");
    }
    public async IAsyncEnumerable<string> Get(string name, CancellationToken ct)
    {
        yield return "hoi van 1";
    }

    public async Task Test(IAsyncEnumerable<string> test, CancellationToken ct)
    {
        await foreach (var item in test)
        {

        }
    }
}
