using Microsoft.Extensions.Hosting;

namespace gAPI.Core.Server.Collections;

public class SessionCacheCleaner(
    SessionCache sessionCache)
    : IHostedService
{
    CancellationTokenSource? Cts;

    public async Task StartAsync(CancellationToken ct)
    {
        if (Cts != null)
        {
            await Cts.CancelAsync();
            Cts.Dispose();
        }
        Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        while (ct.IsCancellationRequested == false)
        {
            sessionCache.Cleanup();
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Cts == null)
            return;
        await Cts.CancelAsync();
        Cts.Dispose();
        Cts = null;
    }
}
