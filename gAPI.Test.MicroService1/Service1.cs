using gAPI.Generated;
using gAPI.Test.Shared.Private;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.MicroService1;

public class Service1(
    IBusContext busContext,
    IBusConnection busConnection)
    : IHostedService
    , IService1
{
    private CancellationTokenSource? Cts;
    async Task IHostedService.StartAsync(CancellationToken ct)
    {
        if (Cts != null)
            return;
        Cts = new CancellationTokenSource();
        ct = Cts.Token;
        await busConnection.SubscribeAsync(this, ct);
    }
    async Task IHostedService.StopAsync(CancellationToken ct)
    {
        if (Cts == null)
            return;
        await busConnection.UnsubscribeAsync(this, ct);
        Cts.Cancel();
        Cts.Dispose();
        Cts = null;
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

    public async void SendAndForget(string message, CancellationToken ct)
    {
    }
    public async void SendAndForgetWithList(IAsyncEnumerable<string> list, CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
        }
    }
    public async Task Send(string message, CancellationToken ct)
    {
    }
    public async Task SendWithList(IAsyncEnumerable<string> list, CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
        }
    }
    public async Task<string> InvokeTask(string name, CancellationToken ct)
    {
        return "Hoi terug";
    }
    public async Task<string> InvokeTaskWithList(IAsyncEnumerable<string> list, CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
        }
        return "Hoi terug";
    }
    public async IAsyncEnumerable<string> InvokeList(string name, [EnumeratorCancellation] CancellationToken ct)
    {
        yield return "1";
        yield return "2";
        yield return "3";
    }
    public async IAsyncEnumerable<string> InvokeListWithList(IAsyncEnumerable<string> list, [EnumeratorCancellation] CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
        }

        yield return "1";
        yield return "2";
        yield return "3";
    }
}
