using gAPI.Generated;
using gAPI.Test.Shared.Public;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace gAPI.Test.Main.Server;

public class MainApi(
    ILoggerFactory loggerFactory, 
    IAuthenticationService authenticationService,
    IBusConnection busConnection,
    IBusContext busContext,
    IClientContext clientContext)
    : IMainApi
{
    private readonly ILogger Logger = loggerFactory.CreateLogger<MainApi>();
    private async IAsyncEnumerable<string> MyList()
    {
        yield return "1";
        Logger.LogInformation($"MyList - 1");
        yield return "2";
        Logger.LogInformation($"MyList - 2");
        yield return "3";
        Logger.LogInformation($"MyList - 3");
    }
    public async Task StartServerTests(double time, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        // Terug naar client
        clientContext.MainHub.ToSession(authenticationService.SessionId).SendAndForget("hoi", ct);
        Logger.LogInformation($"clientContext.MainHub.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await clientContext.MainHub.ToSession(authenticationService.SessionId).Send("hoi", ct);
        Logger.LogInformation($"clientContext.MainHub.Send - {sw.ElapsedMilliseconds}ms");

        var result = string.Empty;
        await foreach (var item in clientContext.MainHub.ToSession(authenticationService.SessionId).InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"mainApi.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }
        await foreach (var item in clientContext.MainHub.ToSession(authenticationService.SessionId).InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"mainApi.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }


        busContext.Service1.ToAny().SendAndForget("hoi", ct);
        Logger.LogInformation($"mainApi.SendAndForget - {sw.ElapsedMilliseconds}ms");

        await busContext.Service1.ToAny().Send("hoi", ct);
        Logger.LogInformation("mainApi.Send");

        result = await busContext.Service1.ToAny().InvokeTask("hoi", ct);
        Logger.LogInformation($"mainApi.InvokeTask - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.Service1.ToAny().InvokeList("hoi", ct))
        {
            result = item;
            Logger.LogInformation($"mainApi.InvokeList - {item} - {sw.ElapsedMilliseconds}ms");
        }

        result = await busContext.Service1.ToAny().InvokeTaskWithList(MyList(), ct);
        Logger.LogInformation($"mainApi.InvokeTaskWithList - {result} - {sw.ElapsedMilliseconds}ms");

        await foreach (var item in busContext.Service1.ToAny().InvokeListWithList(MyList(), ct))
        {
            result = item;
            Logger.LogInformation($"mainApi.InvokeListWithList - {item} - {sw.ElapsedMilliseconds}ms");
        }


        Logger.LogInformation($"mainApi ALL TESTS DONE - {sw.ElapsedMilliseconds}ms");
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
