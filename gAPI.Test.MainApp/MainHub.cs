using gAPI.Generated;
using gAPI.Test.Shared.Public;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.Main.Client;

public class MainHub(
    ILoggerFactory loggerFactory,
    IMainApi mainApi,
    IClientConnection clientConnection)
    : IHostedService
    , IMainHub
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<MainHub>();
    private CancellationTokenSource? Cts;
    async Task IHostedService.StartAsync(CancellationToken ct)
    {
        if (Cts != null)
            return;
        Cts = new CancellationTokenSource();
        ct = Cts.Token;
        await clientConnection.SubscribeAsync(this, ct);

        _ = StartAllTests(ct);
    }
    async Task IHostedService.StopAsync(CancellationToken ct)
    {
        if (Cts == null)
            return;
        await clientConnection.UnsubscribeAsync(this, ct);
        Cts.Cancel();
        Cts.Dispose();
        Cts = null;
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

    public async Task StartAllTests(CancellationToken ct)
    {
        await Task.Delay(2000);

        for (int i = 0; i < 10; i++)
            await mainApi.InvokeTask("hoi", ct);
        Logger.LogInformation("mainApi - Warmup done");

        for (var y = 0; y < 50; y++)
        {
            var sw = Stopwatch.StartNew();

            mainApi.SendAndForget("hoi", ct);
            Logger.LogInformation($"mainApi.SendAndForget - {sw.ElapsedMilliseconds}ms");

            await mainApi.Send("hoi", ct);
            Logger.LogInformation("> mainApi.Send");

            var result = await mainApi.InvokeTask("hoi", ct);
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

}
