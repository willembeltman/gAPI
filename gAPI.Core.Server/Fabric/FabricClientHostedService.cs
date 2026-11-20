using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace gAPI.Core.Server.Fabric;

public class FabricClientHostedService(
    FabricClient fabricClient)
    : IHostedService
{
    private CancellationTokenSource? Cts;

    public async Task StartAsync(CancellationToken ct)
    {
        if (Cts != null)
            return;

        Cts = new CancellationTokenSource();
        ct = Cts.Token;

        fabricClient.Start(ct);
    }
    public async Task StopAsync(CancellationToken ct)
    {
        if (Cts == null)
            return;

        var cts = Cts;

        if (cts != null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        Cts = null;
    }
}

