using gAPI.Core.Ids;
using gAPI.Generated;
using gAPI.Test.Shared.Private;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.MicroService2;

public class Service2(
    ILoggerFactory loggerFactory,
    IBusContext busContext,
    IBusConnection busConnection)
    : IHostedService
    , IService2
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<Service2>();
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

    public Task Ping(CancellationToken ct)
    {
        return busContext.Service1.ToAny().Pong(ct);
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

        busContext.MainService.ToAny().SendAndForget("hoi", ct);
        Logger.LogInformation($"MainService.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await busContext.MainService.ToAny().Send("hoi", ct);
        Logger.LogInformation("MainService.Send");

        var result = await busContext.MainService.ToAny().InvokeTask("hoi", ct);
        Logger.LogInformation($"MainService.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.MainService.ToAny().InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"MainService.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        result = await busContext.MainService.ToAny().InvokeTaskWithList(MyList(), ct);
        Logger.LogInformation($"MainService.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.MainService.ToAny().InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"MainService.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        await busContext.MainService.ToAny().StartServiceTests(sessionId, ct);

        Logger.LogInformation($"MainService ALL TESTS DONE - {sw.ElapsedMilliseconds}ms");
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
