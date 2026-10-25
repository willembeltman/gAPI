using gAPI.Generated;
using gAPI.Test.Shared.Public;
using System.Runtime.CompilerServices;

namespace gAPI.Test.Main.Server;

public class MainApi(
    IAuthenticationService authenticationService,
    IBusConnection busConnection,
    IBusContext busContext,
    IClientContext clientContext)
    : IMainApi
{
    private async IAsyncEnumerable<string> MyList()
    {
        yield return "1";
        yield return "2";
        yield return "3";
    }
    public async Task StartServerTests(CancellationToken ct)
    {
        // Terug naar client
        clientContext.MainHub.ToSession(authenticationService.SessionId).SendAndForget("hoi", ct);
        await clientContext.MainHub.ToSession(authenticationService.SessionId).Send("hoi", ct);
        var result = string.Empty;
        await foreach (var item in clientContext.MainHub.ToSession(authenticationService.SessionId).InvokeList("hoi", ct))
        {
            result = item;
        }
        await foreach (var item in clientContext.MainHub.ToSession(authenticationService.SessionId).InvokeListWithList(MyList(), ct))
        {
            result = item;
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


}
