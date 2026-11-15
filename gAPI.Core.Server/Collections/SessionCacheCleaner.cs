using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace gAPI.Core.Server.Collections;

public class SessionCacheCleaner(
    ILoggerFactory loggerFactory,
    SessionCache sessionCache)
    : IHostedService
{
    CancellationTokenSource? Cts;
    ILogger<SessionCacheCleaner> Logger = loggerFactory.CreateLogger<SessionCacheCleaner>();

    public async Task StartAsync(CancellationToken ct)
    {
        if (Cts != null)
        {
            await Cts.CancelAsync();
            Cts.Dispose();
        }
        Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(async () =>
        {
            try
            {
                while (ct.IsCancellationRequested == false)
                {
                    sessionCache.Cleanup();
                    await Task.Delay(TimeSpan.FromMinutes(5), ct);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while running SessinoCacheCleaner");
            }
        });
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
