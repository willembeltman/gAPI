using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using gAPI.Generated;
using gAPI.Example.Shared.Private;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Example.MicroService2;

public class Bus2(
    ILoggerFactory loggerFactory,
    IBusContext busContext)
    : IBus2
    , IHostedBusService
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<Bus2>();


    public async Task OnStartedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }
    public async Task OnStoppedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }
    public async Task OnSubscribedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }
    public async Task OnUnsubscribedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }

    public Task Ping(CancellationToken ct)
    {
        return busContext.Bus1.ToAny().PongTask(ct);
    }

    public async Task StartServiceTests(SessionId sessionId, CancellationToken ct)
    {
        async IAsyncEnumerable<string> MyList()
        {
            yield return "1";
            Logger.LogInformation($"MyList - 1");
            yield return "2";
            Logger.LogInformation($"MyList - 2");
            yield return "3";
            Logger.LogInformation($"MyList - 3");
        }
        var sw = Stopwatch.StartNew();

        busContext.MainBus.ToAny().SendAndForget("hoi", ct);
        Logger.LogInformation($"MainService.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await busContext.MainBus.ToAny().Send("hoi", ct);
        Logger.LogInformation("MainService.Send");

        var result = await busContext.MainBus.ToAny().InvokeTask("hoi", ct);
        Logger.LogInformation($"MainService.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.MainBus.ToAny().InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"MainService.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        result = await busContext.MainBus.ToAny().InvokeTaskWithList(MyList(), ct);
        Logger.LogInformation($"MainService.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.MainBus.ToAny().InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"MainService.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        await busContext.MainBus.ToAny().StartServiceTests(sessionId, ct);

        Logger.LogInformation($"MainBus ALL TESTS DONE - {sw.ElapsedMilliseconds}ms");
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
