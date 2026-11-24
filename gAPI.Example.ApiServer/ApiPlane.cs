using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using gAPI.Generated;
using gAPI.Example.Shared.Private;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Example.ApiServer.Server;

public class ApiPlane(
    ILoggerFactory loggerFactory,
    IClientContext clientContext,
    IPlaneContext busContext)
    : IApiPlane
    , IHostedPlaneService
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<ApiPlane>();
    private TaskCompletionSource<string>? PongResult;

    public async Task OnStartedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }

    public async Task OnStoppedAsync(ServiceId serviceId, CancellationToken ct)
    {
    }

    public async Task OnSubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
    {
    }

    public async Task OnUnsubscribedAsync(ServiceId serviceId, ServiceSubscriptionId serviceSubscriptionId, CancellationToken ct)
    {
    }


    public async Task DoPing()
    {
        for (var y = 0; y < 50; y++)
        {
            var sw2 = Stopwatch.StartNew();

            PongResult = new TaskCompletionSource<string>();

            busContext.Plane2.ToAny().Ping("hoi, dit is een lange zin. of zo.");

            var str = await PongResult.Task.WaitAsync(CancellationToken.None);

            Logger.LogInformation($"apiPlane => Plane2 - PINGPONG: {sw2.Elapsed.TotalMilliseconds:F3}ms");
        }
    }
    public void Pong(string v)
    {
        PongResult?.SetResult(v);
    }

    public async Task StartServiceTests(SessionId sessionId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        clientContext.ApiHub.ToSession(sessionId).SendAndForget("hoi", ct);
        Logger.LogInformation($"MainHub.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await clientContext.ApiHub.ToSession(sessionId).Send("hoi", ct);
        Logger.LogInformation($"MainHub.Send - {sw.ElapsedMilliseconds}ms");

        var result = string.Empty;
        await foreach (var item in clientContext.ApiHub.ToSession(sessionId).InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"MainHub.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
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
