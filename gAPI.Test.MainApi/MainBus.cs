using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using gAPI.Generated;
using gAPI.Test.Shared.Private;
using gAPI.Test.Shared.Public;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.Main.Server;

public class MainBus(
    ILoggerFactory loggerFactory,
    IClientContext clientContext,
    IBusContext busContext)
    : IMainBus
    , IHostedBusService
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<MainBus>();
    private TaskCompletionSource<string>? PontResult;

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


    internal async Task DoPing()
    {
        for (var y = 0; y < 50; y++)
        {
            var sw2 = Stopwatch.StartNew();

            PontResult = new TaskCompletionSource<string>();

            busContext.Bus1.ToAny().Ping("hoi, dit is een lange zin. of zo.");

            var str = await PontResult.Task.WaitAsync(CancellationToken.None);

            Logger.LogInformation($"mainBus => Bus1 - PINGPONG: {sw2.Elapsed.TotalMilliseconds:F3}ms");
        }
    }
    public void Pong(string v)
    {
        PontResult?.SetResult(v);
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

        clientContext.MainHub.ToSession(sessionId).SendAndForget("hoi", ct);
        Logger.LogInformation($"MainHub.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await clientContext.MainHub.ToSession(sessionId).Send("hoi", ct);
        Logger.LogInformation($"MainHub.Send - {sw.ElapsedMilliseconds}ms");

        var result = string.Empty;
        await foreach (var item in clientContext.MainHub.ToSession(sessionId).InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"MainHub.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        await foreach (var item in clientContext.MainHub.ToSession(sessionId).InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"MainHub.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }

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
