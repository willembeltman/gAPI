using gAPI.Core.Ids;
using gAPI.Core.Server.Interfaces;
using gAPI.Generated;
using gAPI.Example.Shared.Private;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Example.MicroService2;

public class Plane2(
    ILoggerFactory loggerFactory,
    IPlaneContext busContext)
    : IPlane2
    , IHostedPlaneService
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<Plane2>();


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

    public Task RealPing(CancellationToken ct)
    {
        return busContext.Plane1.ToAny().PongTask(ct);
    }

    public async Task StartServiceTests(SessionId sessionId, string to, CancellationToken ct)
    {
        try
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

            busContext.Plane1.ToAny().SendAndForget("hoi", ct);
            Logger.LogInformation($"Service2.SendAndForget - {sw.ElapsedMilliseconds}ms");

            await busContext.Plane1.ToAny().Send("hoi", ct);
            Logger.LogInformation("Service2.Send");

            var result = await busContext.Plane1.ToAny().InvokeTask("hoi", ct);
            Logger.LogInformation($"Service2.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

            await foreach (var item in busContext.Plane1.ToAny().InvokeList("hoi", ct))
            {
                result = item;
                Logger.LogInformation($"Service2.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
            }

            result = await busContext.Plane1.ToAny().InvokeTaskWithList(MyList(), ct);
            Logger.LogInformation($"Service2.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

            await foreach (var item in busContext.Plane1.ToAny().InvokeListWithList(MyList(), ct))
            {
                result = item;
                Logger.LogInformation($"Service2.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
            }


            busContext.ApiPlane.ToAny().SendAndForget("hoi", ct);
            Logger.LogInformation($"MainService.SendAndForget - {sw.ElapsedMilliseconds}ms");

            await busContext.ApiPlane.ToAny().Send("hoi", ct);
            Logger.LogInformation("MainService.Send");

            result = await busContext.ApiPlane.ToAny().InvokeTask("hoi", ct);
            Logger.LogInformation($"MainService.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

            await foreach (var item in busContext.ApiPlane.ToAny().InvokeList("hoi", ct))
            {
                result = item;
                Logger.LogInformation($"MainService.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
            }

            result = await busContext.ApiPlane.ToAny().InvokeTaskWithList(MyList(), ct);
            Logger.LogInformation($"MainService.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

            await foreach (var item in busContext.ApiPlane.ToAny().InvokeListWithList(MyList(), ct))
            {
                result = item;
                Logger.LogInformation($"MainService.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
            }

            await busContext.ApiPlane.ToAny().StartServiceTests(sessionId, ct);

            Logger.LogInformation($"MainPlane ALL TESTS DONE - {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            throw;
        }
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

    public void Ping(string v)
    {
        busContext.ApiPlane.ToAny().Pong(v);
    }

}
