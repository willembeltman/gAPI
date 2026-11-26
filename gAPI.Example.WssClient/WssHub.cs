using gAPI.Core.Client.Interfaces;
using gAPI.Core.Ids;
using gAPI.Example.Shared.WssPublic;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Example.ApiServer.Client;

public class WssHub(
    ILoggerFactory loggerFactory,
    IWssApi mainApi)
    : IWssHub
    , IHostedHubService
{

    public async Task OnStartedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }

    public async Task OnStoppedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }

    public async Task OnSubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
    {
        _ = StartAllTests(ct);
    }
    public async Task OnUnsubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
    {
    }

    private readonly ILogger Logger = loggerFactory.CreateLogger<WssHub>();

    public TaskCompletionSource<string>? PontResult { get; private set; }

    private async IAsyncEnumerable<string> MyList()
    {
        yield return "1";
        Logger.LogInformation($"MyList - 1");
        yield return "2";
        Logger.LogInformation($"MyList - 2");
        yield return "3";
        Logger.LogInformation($"MyList - 3");
    }

    public async Task StartAllTests(CancellationToken ct)
    {
        //await Task.Delay(2000);

        //int i2 = 0;
        //await foreach (var data in mainApi.VideoFeedTest(ct))
        //{
        //    if (i2 == 1)
        //    {
        //        break;
        //    }
        //    i2++;
        //}
        //await Task.Delay(TimeSpan.FromSeconds(10), ct);

        for (int i = 0; i < 10; i++)
            await mainApi.InvokeTask("hoi", ct);
        Logger.LogInformation("mainApi - Warmup done");


        for (var y = 0; y < 50; y++)
        {
            var sw2 = Stopwatch.StartNew();

            PontResult = new TaskCompletionSource<string>();

            mainApi.Ping("hoi, dit is een lange zin. of zo.");

            var str = await PontResult.Task.WaitAsync(ct);

            Logger.LogInformation($"mainClient => mainApi - PINGPONG: {sw2.Elapsed.TotalMilliseconds:F3}ms");
        }


        var sw = Stopwatch.StartNew();
        var result = string.Empty;

        mainApi.SendAndForget("hoi", ct);
        Logger.LogInformation($"mainApi.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await mainApi.Send("hoi", ct);
        Logger.LogInformation("> mainApi.Send");

        result = await mainApi.InvokeTask("hoi", ct);
        Logger.LogInformation($"> mainApi.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in mainApi.InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"> mainApi.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        result = await mainApi.InvokeTaskWithList(MyList(), ct);
        Logger.LogInformation($"> mainApi.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in mainApi.InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"> mainApi.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        await mainApi.StartServerTests(sw.Elapsed.TotalMilliseconds, ct);

        Logger.LogInformation($"> mainApi - ALL TESTS SUCCESFULL - {sw.ElapsedMilliseconds}ms");
    }

    public async void SendAndForget(string message, CancellationToken ct)
    {
        Logger.LogInformation($"MainHub.SendAndForget {message}");
    }
    public async void SendAndForgetWithList(IAsyncEnumerable<string> list, CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
            Logger.LogInformation($"MainHub.SendAndForgetWithList {item}");
        }
    }
    public async Task Send(string message, CancellationToken ct)
    {
        Logger.LogInformation($"MainHub.Send {message}");
    }
    public async Task SendWithList(IAsyncEnumerable<string> list, CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
            Logger.LogInformation($"MainHub.SendWithList {item}");
        }
    }
    public async Task<string> InvokeTask(string message, CancellationToken ct)
    {
        Logger.LogInformation($"MainHub.Send {message}");
        return "Hoi terug";
    }
    public async Task<string> InvokeTaskWithList(IAsyncEnumerable<string> list, CancellationToken ct)
    {
        var result = string.Empty;
        await foreach (var item in list)
        {
            result = item;
            Logger.LogInformation($"MainHub.InvokeTaskWithList {item}");
        }
        return "Hoi terug";
    }
    public async IAsyncEnumerable<string> InvokeList(string message, [EnumeratorCancellation] CancellationToken ct)
    {
        Logger.LogInformation($"MainHub.InvokeList {message}");
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
            Logger.LogInformation($"MainHub.InvokeListWithList {item}");
        }

        yield return "1";
        yield return "2";
        yield return "3";
    }


    public void Ping(string v)
    {
        mainApi.Pong(v);
    }
    public void Pong(string v)
    {
        PontResult?.SetResult(v);
    }
}
