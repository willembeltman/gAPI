using gAPI.Generated;
using gAPI.Test.Shared.Private;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;

namespace gAPI.Test.Main.Server;

public class MainService(
    IBusConnection busConnection,
    IBusContext busContext)
    : IHostedService
    , IMainService
{
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
