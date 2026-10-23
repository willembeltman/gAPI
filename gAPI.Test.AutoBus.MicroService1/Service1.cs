using gAPI.Generated;
using gAPI.Test.AutoBus.Shared;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.AutoBus.MicroService1;

public class Service1(
    IBusContext busContext,
    IBusConnection busConnection)
    : IService1
    , IHostedService
{


    public async Task StartAsync(CancellationToken ct)
    {
        await busConnection.SubscribeAsync(this, ct);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await busConnection.UnsubscribeAsync(this, ct);
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

    Stopwatch? stopwatch;
    TaskCompletionSource<double>? complete;

    public async IAsyncEnumerable<double> Ping([EnumeratorCancellation] CancellationToken ct)
    {
        for (int i = 0; i < 20; i++)
        {
            stopwatch = Stopwatch.StartNew();
            complete = new TaskCompletionSource<double>();

            await busContext.Service2.ToAny().Ping(ct);
            yield return await complete.Task.WaitAsync(ct);
        }
    }

    public async Task Pong(CancellationToken ct)
    {
        if (stopwatch == null || complete == null)
            return;
        complete.SetResult(stopwatch.Elapsed.TotalMilliseconds);
    }
}
