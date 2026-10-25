using gAPI.Core.Ids;
using gAPI.Generated;
using gAPI.Test.Shared.Private;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.MicroService2;

public class Service1(
    ILoggerFactory loggerFactory,
    IBusContext busContext,
    IBusConnection busConnection)
    : IHostedService
    , IService1
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<Service1>();
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

    private async IAsyncEnumerable<string> MyList()
    {
        yield return "1";
        Logger.LogInformation($"MyList - 1");
        yield return "2";
        Logger.LogInformation($"MyList - 2");
        yield return "3";
        Logger.LogInformation($"MyList - 3");
    }
    public async Task StartServiceTests(SessionId sessionId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        busContext.Service2.ToAny().SendAndForget("hoi", ct);
        Logger.LogInformation($"Service2.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await busContext.Service2.ToAny().Send("hoi", ct);
        Logger.LogInformation("Service2.Send");

        var result = await busContext.Service2.ToAny().InvokeTask("hoi", ct);
        Logger.LogInformation($"Service2.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.Service2.ToAny().InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"Service2.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        result = await busContext.Service2.ToAny().InvokeTaskWithList(MyList(), ct);
        Logger.LogInformation($"Service2.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.Service2.ToAny().InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"Service2.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        await busContext.Service2.ToAny().StartServiceTests(sessionId, ct);

        Logger.LogInformation($"Service2 ALL TESTS DONE - {sw.ElapsedMilliseconds}ms");
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
